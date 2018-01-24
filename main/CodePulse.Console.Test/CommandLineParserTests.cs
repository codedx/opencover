using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodePulse.Console.Test
{
    [TestClass]
    public class CommandLineParserTests
    {
        [TestMethod]
        public void WhenIisModeAndMissingParametersExceptionOccurs()
        {
            // arrange
            var parameters = new[]
            {
                "-IIS"
            };
            ValidateCommandLineArguments(parameters, "The TargetDir argument is required.");
        }

        [TestMethod]
        public void WhenIisModeWithTargetDirAndMissingParametersExceptionOccurs()
        {
            // arrange
            var parameters = new[]
            {
                "-IIS",
                "-TargetDir:folder"
            };
            ValidateCommandLineArguments(parameters, "The IISAppPoolIdentity argument is required.");
        }

        [TestMethod]
        public void WhenIisModeServiceControlTimeoutTooLowExceptionOccurs()
        {
            // arrange
            var parameters = new[]
            {
                "-IIS",
                "-IISAppPoolIdentity:Account",
                "-TargetDir:folder",
                "-ServiceControlTimeout:4"
            };
            ValidateCommandLineArguments(parameters, "The service control timeout must be a non-negative integer. The argument servicecontroltimeout must be between 5 and 60.");
        }

        [TestMethod]
        public void WhenIisModeServiceControlTimeoutTooHighExceptionOccurs()
        {
            // arrange
            var parameters = new[]
            {
                "-IIS",
                "-IISAppPoolIdentity:Account",
                "-TargetDir:folder",
                "-ServiceControlTimeout:61"
            };
            ValidateCommandLineArguments(parameters, "The service control timeout must be a non-negative integer. The argument servicecontroltimeout must be between 5 and 60.");
        }

        [TestMethod]
        public void WhenIisModeAndSendVisitPointsTimerSpecifiedExceptionOccurs()
        {
            // arrange
            var parameters = new[]
            {
                "-IIS",
                "-IISAppPoolIdentity:Account",
                "-TargetDir:folder",
                "-SendVisitPointsTimerInterval:1"
            };
            ValidateCommandLineArguments(parameters, "SendVisitPointsTimerInterval argument is incompatible with -IIS switch.");
        }

        [TestMethod]
        public void WhenIisModeSpecifiedCorrectlySuccess()
        {
            // arrange
            var parameters = new[]
            {
                "-IIS",
                "-IISAppPoolIdentity:Account",
                "-TargetDir:folder",
                "-ServiceControlTimeout:60"
            };
            ValidateCommandLineArguments(parameters);
        }


        [TestMethod]
        public void WhenAppModeSendVisitPointsTimerIntervalTooLowExceptionOccurs()
        {
            // arrange
            var parameters = new[]
            {
                "-Target:file",
                "-SendVisitPointsTimerInterval:-1"
            };
            ValidateCommandLineArguments(parameters, "The send visit points timer interval must be a non-negative integer. -1 is not a valid value for UInt32.");
        }

        [TestMethod]
        public void WhenAppModeSendVisitPointsTimerIntervalTooHighExceptionOccurs()
        {
            // arrange
            var parameters = new[]
            {
                "-Target:file",
                $"-SendVisitPointsTimerInterval:{(60u * 60u * 1000u) + 1}"
            };
            ValidateCommandLineArguments(parameters, "The send visit points timer interval must be a non-negative integer. The argument sendvisitpointstimerinterval must be between 0 and 3600000");
        }

        [TestMethod]
        public void WhenAppModeSpecifiedWithNoSendVisitPointsTimerIntervalExceptionOccurs()
        {
            // arrange
            var parameters = new[]
            {
                "-Target:file"
            };
            ValidateCommandLineArguments(parameters, "SendVisitPointsTimerInterval command line argument must be specified as a number > 0.");
        }

        [TestMethod]
        public void WhenAppModeWithLogIncorrectExceptionOccurs()
        {
            // arrange
            var parameters = new[]
            {
                "-Target:file",
                "-SendVisitPointsTimerInterval:45",
                "-Log:any"
            };
            ValidateCommandLineArguments(parameters, "'any' is an invalid value for log parameter.");
        }

        [TestMethod]
        public void WhenAppModeSpecifiedCorrectlySuccess()
        {
            // arrange
            var parameters = new[]
            {
                "-Target:file",
                "-SendVisitPointsTimerInterval:45"
            };
            ValidateCommandLineArguments(parameters);
        }

        [TestMethod]
        public void WhenAppModeWithLogOffSpecifiedSuccess()
        {
            // arrange
            var parameters = new[]
            {
                "-Target:file",
                "-SendVisitPointsTimerInterval:45",
                "-Log:off"
            };
            ValidateCommandLineArguments(parameters);
        }

        [TestMethod]
        public void WhenAppModeWithLogFatalSpecifiedSuccess()
        {
            // arrange
            var parameters = new[]
            {
                "-Target:file",
                "-SendVisitPointsTimerInterval:45",
                "-Log:fatal"
            };
            ValidateCommandLineArguments(parameters);
        }

        [TestMethod]
        public void WhenAppModeWithLogErrorSpecifiedSuccess()
        {
            // arrange
            var parameters = new[]
            {
                "-Target:file",
                "-SendVisitPointsTimerInterval:45",
                "-Log:error"
            };
            ValidateCommandLineArguments(parameters);
        }

        [TestMethod]
        public void WhenAppModeWithLogWarnSpecifiedSuccess()
        {
            // arrange
            var parameters = new[]
            {
                "-Target:file",
                "-SendVisitPointsTimerInterval:45",
                "-Log:warn"
            };
            ValidateCommandLineArguments(parameters);
        }

        [TestMethod]
        public void WhenAppModeWithLogInfoSpecifiedSuccess()
        {
            // arrange
            var parameters = new[]
            {
                "-Target:file",
                "-SendVisitPointsTimerInterval:45",
                "-Log:info"
            };
            ValidateCommandLineArguments(parameters);
        }

        [TestMethod]
        public void WhenAppModeWithLogDebugSpecifiedSuccess()
        {
            // arrange
            var parameters = new[]
            {
                "-Target:file",
                "-SendVisitPointsTimerInterval:45",
                "-Log:debug"
            };
            ValidateCommandLineArguments(parameters);
        }

        [TestMethod]
        public void WhenAppModeWithLogVerboseSpecifiedSuccess()
        {
            // arrange
            var parameters = new[]
            {
                "-Target:file",
                "-SendVisitPointsTimerInterval:45",
                "-Log:verbose"
            };
            ValidateCommandLineArguments(parameters);
        }

        [TestMethod]
        public void WhenAppModeWithLogAllSpecifiedSuccess()
        {
            // arrange
            var parameters = new[]
            {
                "-Target:file",
                "-SendVisitPointsTimerInterval:45",
                "-Log:all"
            };
            ValidateCommandLineArguments(parameters);
        }

        private static void ValidateCommandLineArguments(string[] parameters, string expectedError = "")
        {
            // arrange
            var parser = new CommandLineParser(parameters);

            // act
            var error = string.Empty;
            try
            {
                parser.ExtractAndValidateArguments();
            }
            catch (InvalidOperationException e)
            {
                error = e.Message;
            }

            // assert
            Assert.AreEqual(expectedError, error);
        }
    }
}
