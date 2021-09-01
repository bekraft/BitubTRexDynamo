using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Dynamo.Graph.Nodes;

using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;

using TRex.Internal;

using Dynamo.Utilities;

namespace TRex.Log
{
    /// <summary>
    /// Interactive log filter node to be attached to node progressing implementations.
    /// </summary>
    [NodeName("Log Filter")]
    [NodeDescription("Log filtering by reason mask and threshold of maximum messages")]
    [InPortTypes(nameof(ProgressingTask), nameof(LogReason))]
    [InPortNames("model", "mask")]
    [InPortDescriptions("Log source (i.e. a model)", "Log filter mask")]
    [OutPortTypes(nameof(LogMessage))]
    [OutPortNames("messages")]
    [OutPortDescriptions("Extracted log messages")]
    [NodeCategory("TRex.Log")]
    [IsDesignScriptCompatible]
    public class LogFilterNodeModel : BaseNodeModel
    {
        #region Internals

        private int logCount = 10;
        private LogSeverity logMinSeverity;
        private IDictionary<ProgressingTask, long> logSourceRegistry = new Dictionary<ProgressingTask, long>();
        private long recentTimeStamp = long.MinValue;
        
        [JsonConstructor]
        LogFilterNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            Init();
        }

        private void Init()
        {
            DynamicDelegation.Put<ProgressingTask, ProgressingTask>(RegisterOnActionLogChangedName, RegisterOnActionLogChanged);
        }

        private string[] RegisterOnActionLogChangedName
        {
            get => typeof(LogFilterNodeModel).ToQualifiedMethodName(nameof(RegisterOnActionLogChanged));
        }

        #endregion

        /// <summary>
        /// New log filter model.
        /// </summary>
        public LogFilterNodeModel()
        {
            RegisterAllPorts();
            Init();
        }

#pragma warning disable CS1591

        public int LogCount
        {
            get {
                return logCount;
            }
            set {
                logCount = value;
                RaisePropertyChanged(nameof(LogCount));
                OnNodeModified(true);
            }
        }        

        public LogSeverity LogMinSeverity
        {
            get {
                return logMinSeverity;
            }
            set {
                logMinSeverity = value;
                RaisePropertyChanged(nameof(LogMinSeverity));
                OnNodeModified(true);
            }
        }

        public ProgressingTask RegisterOnActionLogChanged(ProgressingTask nodeProgressing)
        {
            if (null != nodeProgressing)
            {
                if (!logSourceRegistry.ContainsKey(nodeProgressing))
                    nodeProgressing.OnLogAction += NodeProgressing_OnLogAction;

                logSourceRegistry[nodeProgressing] = recentTimeStamp;
            }
            return nodeProgressing;
        }

        private void NodeProgressing_OnLogAction(object sender, IEnumerable<LogMessage> actions)
        {
            OnNodeModified(true);
        }

        private void ClearOnActionLogChanged(long clearTime)
        {
            foreach (var logSource in logSourceRegistry.Where(g => g.Value < clearTime).Select(g => g.Key).ToArray())
            {
                logSource.OnLogAction -= NodeProgressing_OnLogAction;
                logSourceRegistry.Remove(logSource);
            }
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {            
            ClearErrorsAndWarnings();
            if (!IsAcceptable(inputAstNodes, 1))
            {
                WarnForMissingInputs();
                return BuildNullResult();
            }

            ClearOnActionLogChanged(recentTimeStamp);
            recentTimeStamp = DateTime.Now.ToBinary();

            var astFilterBySeverity = AstFactory.BuildFunctionCall(
                new Func<LogSeverity, object, ProgressingTask, long, LogMessage[]>(LogMessage.FilterBySeverity),
                new List<AssociativeNode>()
                {
                    MapEnum(LogMinSeverity),
                    inputAstNodes[1],
                    inputAstNodes[0].ToDynamicTaskProgressingFunc(RegisterOnActionLogChangedName),
                    AstFactory.BuildIntNode((long)LogCount)
                });
            return BuildResult(astFilterBySeverity);
        }

#pragma warning restore CS1591
    }
}
