using System;
using System.Collections.Generic;
using System.Linq;
using CodePulse.Client.Agent;
using CodePulse.Client.Config;
using log4net;
using OpenCover.Framework.Communication;
using OpenCover.Framework.Model;

namespace OpenCover.Framework.Persistance
{
    /// <summary>
    /// Persists data to Code Pulse application.
    /// </summary>
    public class CodePulsePersistence : BasePersistance
    {
        /// <summary>
        /// Logger used for this instance of BasePersistance.
        /// </summary>
        private readonly ILog _logger;

        /// <summary>
        /// Agent that connects to Code Pulse application.
        /// </summary>
        private ITraceAgent _agent;

        /// <inheritdoc />
        public CodePulsePersistence(ICommandLine commandLine, ILog logger)
            : base(commandLine, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes Code Pulse agent using the specified configuration.
        /// </summary>
        /// <param name="configuration">Configuration details for agent initialization.</param>
        /// <returns>True if agent started and ready to send trace data to Code Pulse. False
        /// if the agent could not connect or prepare for communication with Code Pulse.
        /// </returns>
        public bool Initialize(StaticAgentConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (_agent != null)
            {
                throw new InvalidOperationException("Agent is already initialized");
            }

            _agent = new DefaultTraceAgent(configuration);
            if (!_agent.Connect())
            {
                _logger.Error($"Cannot connect agent to Code Pulse at {configuration.HqHost} and {configuration.HqPort}.");
                return false;
            }

            if (!_agent.Prepare())
            {
                _logger.Error("Could not prepare to send data to Code Pulse");
                return false;
            }

            _agent.WaitForStart();
            return true;
        }

        /// <inheritdoc />
        protected override void OnContextEnd(Guid contextId, HashSet<uint> relatedSpids)
        {
            foreach (var relatedSpid in relatedSpids)
            {
                if (relatedSpid == (uint)MSG_IdType.IT_VisitPointContextEnd)
                {
                    // ignore visit-point-context-end association with contextId
                    continue;
                }

                var startAndEndLineNumber = InstrumentationPoint.GetLineNumbers(relatedSpid);
                if (startAndEndLineNumber?.Item1 == null || startAndEndLineNumber.Item2 == null)
                {
                    continue;
                }

                var declaringMethod = InstrumentationPoint.GetDeclaringMethod(relatedSpid);
                var methodContainingSpid = GetMethod(declaringMethod.DeclaringClass.DeclaringModule.ModulePath,
                    declaringMethod.MetadataToken, out var @class);
                if (methodContainingSpid == null)
                {
                    continue;
                }

                var filePath = "?";
                var firstFile = @class.Files.FirstOrDefault();
                if (firstFile != null)
                {
                    filePath = firstFile.FullPath;
                }

                var endLineNumber = startAndEndLineNumber.Item2.Value;

                _agent.TraceDataCollector.MethodEntry(@class.FullName, filePath,
                    declaringMethod.CallName,
                    declaringMethod.MethodSignature,
                    startAndEndLineNumber.Item1.Value,
                    endLineNumber);
            }
        }
    }
}
