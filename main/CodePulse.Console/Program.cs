﻿// Copyright 2018 Secure Decisions, a division of Applied Visions, Inc. 
// Permission is hereby granted, free of charge, to any person obtaining a copy of 
// this software and associated documentation files (the "Software"), to deal in the 
// Software without restriction, including without limitation the rights to use, copy, 
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the 
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies 
// or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

//
// OpenCover - S Wilde
//
// This source code is released under the MIT License; see the accompanying license file.
//
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using OpenCover.Framework;
using OpenCover.Framework.Manager;
using OpenCover.Framework.Persistance;
using OpenCover.Framework.Utility;
using log4net;
using CodePulse.Client.Config;
using OpenCover.Framework.Model;
using File = System.IO.File;

namespace CodePulse.Console
{
    internal class ProgramExitCodes
    {
        public const int Success = 0;
        public const int CannotParseCommandLine = 1;
        public const int CannotInitializeCodePulseConnection = 2;
        public const int ApplicationExitDueToError = 3;
        public const int ApplicationExitDueToUnexpectedException = 4;
        public const int ApplicationExitDueToUnhandledException = 5;
        public const int RunProcessFailed = 6;
        public const int CannotStopServiceBeforeTrace = 7;
        public const int ServiceFailedToStart = 8;
        public const int CannotRestoreServiceStatus = 9;
        public const int IisWebApplicationProfilingAlreadyRunning = 10;
        public const int CannotStopServiceAfterTrace = 11;
    }

    internal class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger("OpenCover");

        private static int _returnCodeOffset;

        private const string WorldWideWebPublishingServiceName = "w3svc";
        private const string WorldWideWebPublishingServiceDisplayName = "World Wide Web Publishing Service";
        private const string WindowsProcessActivationServiceName = "was";

        private const string ServiceApplicationProfilingKey = "1e830ab5-10e3-4201-9d39-fca143b77d8f";

        private const string GitHubIssuesListUrl = "https://github.com/codedx/codepulse/issues";
        private const string GitHubIssuesStatement = "If you are unable to resolve the issue, please contact the Code Pulse development team";

        private static CodePulsePersistence _persistence;

        /// <summary>
        /// This is the Code Pulse .NET Console application.
        /// </summary>
        /// <param name="args">Application arguments - see usage statement</param>
        /// <returns>Return code adjusted by optional offset.</returns>
        private static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            try
            {
                if (!ParseCommandLine(args, out var parser))
                {
                    return ProgramExitCodes.CannotParseCommandLine;
                }
                _returnCodeOffset = parser.ReturnCodeOffset;

                LogManager.GetRepository().Threshold = parser.LogLevel;

                Logger.Info("Starting...");

                var filter = BuildFilter(parser);
                var perfCounter = CreatePerformanceCounter(parser);

                using (var container = new Bootstrapper(Logger))
                {
                    Logger.Info("Connecting to Code Pulse...");

                    // Write to stdout so that user sees message regardless of log level.
                    System.Console.WriteLine("Open Code Pulse, select a project, wait for the connection, and start a trace.");

                    _persistence = new CodePulsePersistence(parser, Logger);
                    container.Initialise(filter, parser, _persistence, perfCounter);
                    if (!_persistence.Initialize(new StaticAgentConfiguration(parser.CodePulsePort, parser.CodePulseHost, parser.CodePulseConnectTimeout, Logger)))
                    {
                        Logger.Fatal("Failed to initialize Code Pulse connection. Is it running?");
                        return MakeExitCode(ProgramExitCodes.CannotInitializeCodePulseConnection);
                    }

                    var returnCode = RunWithContainer(parser, container, _persistence);

                    perfCounter.ResetCounters();

                    return returnCode;
                }
            }
            catch (ExitApplicationWithoutReportingException)
            {
                Logger.Fatal(GitHubIssuesStatement);
                Logger.Fatal(GitHubIssuesListUrl);
                return MakeExitCode(ProgramExitCodes.ApplicationExitDueToError);
            }
            catch (Exception ex)
            {
                Logger.Fatal("At: Program.Main");
                Logger.Fatal($"An {ex.GetType()} occured: {ex.Message}.");
                Logger.Fatal($"stack: {ex.StackTrace}");
                Logger.Fatal(GitHubIssuesStatement);
                Logger.Fatal(GitHubIssuesListUrl);
                return MakeExitCode(ProgramExitCodes.ApplicationExitDueToUnexpectedException);
            }
        }

        private static int RunWithContainer(CommandLineParser parser, Bootstrapper container, IPersistance persistance)
        {
            var returnCode = 0;
            var registered = false;

            try
            {
                if (parser.Register)
                {
                    Logger.Debug("Registering profiler...");
                    ProfilerRegistration.Register(parser.Registration);

                    registered = true;
                }

                var harness = container.Resolve<IProfilerManager>();

                var servicePrincipalList = new List<string>();
                if (parser.Iis)
                {
                    Logger.Debug($"Profiler configuration will use App Pool identity '{parser.IisAppPoolIdentity}'.");
                    servicePrincipalList.Add(parser.IisAppPoolIdentity);
                }

                harness.RunProcess(environment =>
                {
                    returnCode = parser.Iis ? RunIisWebApplication(parser, environment) : RunProcess(parser, environment);
                }, servicePrincipalList.ToArray());

                CalculateAndDisplayResults(persistance.CoverageSession, parser);
            }
            finally
            {
                if (registered)
                {
                    Logger.Debug("Unregistering profiler...");
                    ProfilerRegistration.Unregister(parser.Registration);
                }
            }
            return returnCode;
        }

        private static int RunIisWebApplication(CommandLineParser parser, Action<StringDictionary> environment)
        {
            var iisServiceApplicationProfilingKey = $"{ServiceApplicationProfilingKey}-{WorldWideWebPublishingServiceName}";

            var mutex = new System.Threading.Mutex(true, iisServiceApplicationProfilingKey, out var result);
            if (!result)
            {
                Logger.Fatal("Another instance of this application is already profiling an IIS web application.");
                return MakeExitCode(ProgramExitCodes.IisWebApplicationProfilingAlreadyRunning);
            }
            GC.KeepAlive(mutex);

            using (var w3SvcService = new ServiceControl(WorldWideWebPublishingServiceName))
            using (var wasService = new ServiceControl(WindowsProcessActivationServiceName))
            {
                var services = new[] {w3SvcService, wasService};

                // svchost.exe will have two services registered in its process and will
                // continue to run, hosting the was service, when the w3svc service stops

                foreach (var serviceToStopBeforeTrace in services)
                {
                    Logger.Info($"Stopping the service named '{serviceToStopBeforeTrace.ServiceDisplayName}', if necessary");
                    if (serviceToStopBeforeTrace.StopService(parser.ServiceControlTimeout))
                    {
                        continue;
                    }
                    Logger.Fatal($"Service '{serviceToStopBeforeTrace.ServiceDisplayName}' failed to stop.");
                    return MakeExitCode(ProgramExitCodes.CannotStopServiceBeforeTrace);
                }

                // now to set the environment variables
                var profilerEnvironment = new StringDictionary();
                environment(profilerEnvironment);

                try
                {
                    Logger.Info($"Starting service '{w3SvcService.ServiceDisplayName}'.");

                    w3SvcService.StartServiceWithPrincipalBasedEnvironment(parser.ServiceControlTimeout, profilerEnvironment);
                    Logger.Info($"Service started '{w3SvcService.ServiceDisplayName}'.");
                }
                catch (InvalidOperationException fault)
                {
                    Logger.Fatal($"Service launch failed with '{fault}'");
                    return MakeExitCode(ProgramExitCodes.ServiceFailedToStart);
                }

                Logger.Info("Trace started successfully");

                System.Console.WriteLine($"Trace will stop when either '{WorldWideWebPublishingServiceDisplayName}' stops or Code Pulse ends the trace.");

                var service = w3SvcService;
                Task.WaitAny(
                    Task.Run(() => service.WaitForStatus(ServiceControllerStatus.Stopped)),
                    Task.Run(() => _persistence.WaitForShutdown()));

                foreach (var serviceToStopAfterTrace in services)
                {
                    Logger.Info($"Stopping '{serviceToStopAfterTrace.ServiceDisplayName}'.");
                    if (serviceToStopAfterTrace.StopService(parser.ServiceControlTimeout))
                    {
                        continue;
                    }
                    Logger.Fatal($"Service '{serviceToStopAfterTrace.ServiceDisplayName}' failed to stop after trace ended.");
                    return MakeExitCode(ProgramExitCodes.CannotStopServiceAfterTrace);
                }

                if (w3SvcService.InitiallyStarted && !w3SvcService.StartService(parser.ServiceControlTimeout))
                {
                    Logger.Fatal($"Unable to restart service named  '{WorldWideWebPublishingServiceDisplayName}'.");
                    return MakeExitCode(ProgramExitCodes.CannotRestoreServiceStatus);
                }

                Logger.Info("IIS web application trace completed.");

                return MakeExitCode(ProgramExitCodes.Success);
            }
        }

        private static int RunProcess(CommandLineParser parser, Action<StringDictionary> environment)
        {
            var targetPathname = ResolveTargetPathname(parser);

            Logger.Info($"Executing: {Path.GetFullPath(targetPathname)}...");

            var startInfo = new ProcessStartInfo(targetPathname);
            environment(startInfo.EnvironmentVariables);

            if (parser.DiagMode)
            {
                startInfo.EnvironmentVariables[@"OpenCover_Profiler_Diagnostics"] = "true";
            }

            startInfo.Arguments = parser.TargetArgs;
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = parser.TargetDir;

            try
            {
                var process = Process.Start(startInfo);
                if (process == null)
                {
                    Logger.Fatal("Process unexpectedly did not start");
                    return ProgramExitCodes.RunProcessFailed;
                }

                Logger.Info("Trace started successfully - end program to finish tracing.");
                process.WaitForExit();

                var exitCode = parser.ReturnTargetCode ? process.ExitCode : 0;
                Logger.Info($"Application trace completed. Reported exited code is {exitCode}.");

                return exitCode;
            }
            catch (Exception)
            {
                Logger.Fatal($"Failed to execute the following command '{startInfo.FileName} {startInfo.Arguments}'.");
                return MakeExitCode(ProgramExitCodes.RunProcessFailed);
            }
        }

        private static IEnumerable<string> GetSearchPaths(string targetDir)
        {
            return (new[] { Environment.CurrentDirectory, targetDir }).Concat((Environment.GetEnvironmentVariable("PATH") ?? Environment.CurrentDirectory).Split(Path.PathSeparator));
        }

        private static string ResolveTargetPathname(CommandLineParser parser)
        {
            var expandedTargetName = Environment.ExpandEnvironmentVariables(parser.Target);
            var expandedTargetDir = Environment.ExpandEnvironmentVariables(parser.TargetDir ?? string.Empty);
            return Path.IsPathRooted(expandedTargetName) ? Path.Combine(Environment.CurrentDirectory, expandedTargetName) :
                GetSearchPaths(expandedTargetDir).Select(dir => Path.Combine(dir.Trim('"'), expandedTargetName)).FirstOrDefault(File.Exists) ?? expandedTargetName;
        }

        private static IFilter BuildFilter(CommandLineParser parser)
        {
            var filter = Filter.BuildFilter(parser);
            if (!string.IsNullOrWhiteSpace(parser.FilterFile))
            {
                if (!File.Exists(parser.FilterFile.Trim()))
                    System.Console.WriteLine("FilterFile '{0}' cannot be found - have you specified your arguments correctly?", parser.FilterFile);
                else
                {
                    var filters = File.ReadAllLines(parser.FilterFile);
                    filters.ToList().ForEach(filter.AddFilter);
                }
            }
            else
            {
                if (parser.Filters.Count == 0)
                    filter.AddFilter("+[*]*");
            }

            return filter;
        }

        private static IPerfCounters CreatePerformanceCounter(CommandLineParser parser)
        {
            return parser.EnablePerformanceCounters ? (IPerfCounters) new PerfCounters() : new NullPerfCounter();
        }

        private static bool ParseCommandLine(string[] args, out CommandLineParser parser)
        {
            parser = new CommandLineParser(args);

            try
            {
                parser.ExtractAndValidateArguments();

                if (parser.PrintUsage)
                {
                    System.Console.WriteLine(parser.Usage());
                    return false;
                }

                if (parser.PrintVersion)
                {
                    var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
                    if (entryAssembly == null)
                    {
                        var entryAssemblyIsUnavailableMessage = "Entry assembly is unavailable.";

                        Logger.Fatal(entryAssemblyIsUnavailableMessage);
                        System.Console.Error.WriteLine(entryAssemblyIsUnavailableMessage);

                        return false;
                    }

                    var version = entryAssembly.GetName().Version;
                    System.Console.WriteLine("Code Pulse .NET Console version {0}", version);
                    if (args.Length == 1)
                    { 
                        return false;
                    }
                }

                if (!string.IsNullOrWhiteSpace(parser.TargetDir) && !Directory.Exists(parser.TargetDir))
                {
                    var invalidTargetDirectoryMessage = $"TargetDir '{parser.TargetDir}' cannot be found - have you specified your arguments correctly?";

                    Logger.Fatal(invalidTargetDirectoryMessage);
                    System.Console.Error.WriteLine(invalidTargetDirectoryMessage);
                    return false;
                }

                if (!parser.Iis && !File.Exists(ResolveTargetPathname(parser)))
                {
                    var invalidTargetMessage = $"Target '{parser.Target}' cannot be found - have you specified your arguments correctly?";

                    Logger.Fatal(invalidTargetMessage);
                    System.Console.Error.WriteLine(invalidTargetMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                var invalidArgumentsMessage = $"Incorrect Arguments: {ex.Message}";

                Logger.Fatal(invalidArgumentsMessage);

                System.Console.Error.WriteLine();
                System.Console.Error.WriteLine(invalidArgumentsMessage);
                System.Console.Error.WriteLine();

                var executingAssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                System.Console.Error.WriteLine($"Review usage statement by running: {executingAssemblyName} -?");

                return false;
            }

            return true;
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            var ex = (Exception)unhandledExceptionEventArgs.ExceptionObject;
            var unhandledExceptionMessage = $"An {ex.GetType()} occured: {ex.Message}";

            Logger.Fatal("At: CurrentDomainOnUnhandledException");
            Logger.Fatal(unhandledExceptionMessage);
            Logger.Fatal($"stack: {ex.StackTrace}");

            System.Console.Error.WriteLine(unhandledExceptionMessage);

            Environment.Exit(MakeExitCode(ProgramExitCodes.ApplicationExitDueToUnhandledException));
        }

        private static int MakeExitCode(int exitCode)
        {
            return _returnCodeOffset + exitCode;
        }

        #region PrintResults
        private class Results
        {
            public int AltTotalClasses;
            public int AltVisitedClasses;
            public int AltTotalMethods;
            public int AltVisitedMethods;
            public readonly List<string> UnvisitedClasses = new List<string>();
            public readonly List<string> UnvisitedMethods = new List<string>();
        }

        private static void CalculateAndDisplayResults(CoverageSession coverageSession, ICommandLine parser)
        {
            if (!Logger.IsInfoEnabled)
                return;

            var results = new Results();

            if (coverageSession.Modules != null)
            {
                CalculateResults(coverageSession, results);
            }

            DisplayResults(coverageSession, parser, results);
        }

        private static void CalculateResults(CoverageSession coverageSession, Results results)
        {
            foreach (var @class in
                                from module in coverageSession.Modules.Where(x => x.Classes != null)
                                from @class in module.Classes.Where(c => !c.ShouldSerializeSkippedDueTo())
                                select @class)
            {
                if (@class.Methods == null)
                    continue;

                if (!@class.Methods.Any(x => !x.ShouldSerializeSkippedDueTo() && x.SequencePoints.Any(y => y.VisitCount > 0))
                    && @class.Methods.Any(x => x.FileRef != null))
                {
                    results.UnvisitedClasses.Add(@class.FullName);
                }

                if (@class.Methods.Any(x => x.Visited))
                {
                    results.AltVisitedClasses += 1;
                    results.AltTotalClasses += 1;
                }
                else if (@class.Methods.Any())
                {
                    results.AltTotalClasses += 1;
                }

                foreach (var method in @class.Methods.Where(x => !x.ShouldSerializeSkippedDueTo()))
                {
                    if (method.FileRef != null && !method.SequencePoints.Any(x => x.VisitCount > 0))
                        results.UnvisitedMethods.Add(method.FullName);

                    results.AltTotalMethods += 1;
                    if (method.Visited)
                    {
                        results.AltVisitedMethods += 1;
                    }
                }
            }
        }

        private static void DisplayResults(CoverageSession coverageSession, ICommandLine parser, Results results)
        {
            if (coverageSession.Summary.NumClasses > 0)
            {
                Logger.InfoFormat("Visited Classes {0} of {1} ({2})", coverageSession.Summary.VisitedClasses,
                                  coverageSession.Summary.NumClasses, Math.Round(coverageSession.Summary.VisitedClasses * 100.0 / coverageSession.Summary.NumClasses, 2));
                Logger.InfoFormat("Visited Methods {0} of {1} ({2})", coverageSession.Summary.VisitedMethods,
                                  coverageSession.Summary.NumMethods, Math.Round(coverageSession.Summary.VisitedMethods * 100.0 / coverageSession.Summary.NumMethods, 2));
                Logger.InfoFormat("Visited Points {0} of {1} ({2})", coverageSession.Summary.VisitedSequencePoints,
                                  coverageSession.Summary.NumSequencePoints, coverageSession.Summary.SequenceCoverage);
                Logger.InfoFormat("Visited Branches {0} of {1} ({2})", coverageSession.Summary.VisitedBranchPoints,
                                  coverageSession.Summary.NumBranchPoints, coverageSession.Summary.BranchCoverage);

                Logger.Info("");
                Logger.Info(
                    "==== Alternative Results (includes all methods including those without corresponding source) ====");
                Logger.InfoFormat("Alternative Visited Classes {0} of {1} ({2})", results.AltVisitedClasses,
                                  results.AltTotalClasses, results.AltTotalClasses == 0 ? 0 : Math.Round(results.AltVisitedClasses * 100.0 / results.AltTotalClasses, 2));
                Logger.InfoFormat("Alternative Visited Methods {0} of {1} ({2})", results.AltVisitedMethods,
                                  results.AltTotalMethods, results.AltTotalMethods == 0 ? 0 : Math.Round(results.AltVisitedMethods * 100.0 / results.AltTotalMethods, 2));

                if (parser.ShowUnvisited)
                {
                    Logger.Info("");
                    Logger.Info("====Unvisited Classes====");
                    foreach (var unvisitedClass in results.UnvisitedClasses)
                    {
                        Logger.Info(unvisitedClass);
                    }

                    Logger.Info("");
                    Logger.Info("====Unvisited Methods====");
                    foreach (var unvisitedMethod in results.UnvisitedMethods)
                    {
                        Logger.Info(unvisitedMethod);
                    }
                }
            }
            else
            {
                Logger.Info("No results, this could be for a number of reasons. The most common reasons are:");
                Logger.Info("    1) missing PDBs for the assemblies that match the filter please review the");
                Logger.Info("    output file and refer to the Usage guide (Usage.rtf) about filters.");
                Logger.Info("    2) the profiler may not be registered correctly, please refer to the Usage");
                Logger.Info("    guide and the -register switch.");
                Logger.Info("    3) the user account for the process under test (e.g., app pool account) may");
                Logger.Info("    not have access to the registered profiler DLL.");
            }
        }
        #endregion
    }
}

