using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Dynamo.Graph.Nodes;

using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;

using Internal;
using Dynamo.Utilities;

namespace Log
{
    /// <summary>
    /// Interactive log filter node to be attached to node progressing implementations.
    /// </summary>
    [NodeName("Log Filter")]
    [NodeDescription("Log filtering node")]
    [InPortTypes(nameof(ProgressingTask))]
    [OutPortTypes(typeof(LogMessage))]
    [NodeCategory("TRexIfc.Log")]
    [IsDesignScriptCompatible]
    public class LogFilterNodeModel : BaseNodeModel
    {
        #region Internals

        private int _logCount = 10;
        private LogSeverity _logMinSeverity;
        private IDictionary<ObservableCollection<LogMessage>, long> _runtimeStamp = new Dictionary<ObservableCollection<LogMessage>, long>();
        private long _timeStamp = long.MinValue;
        //private string _sCallQualifier;
        
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
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("logSource", "Log source (i.e. IfcModel)")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("logReason", "Reason filter flags")));
            
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("logMessages", "Log events")));

            RegisterAllPorts();
            Init();
        }

#pragma warning disable CS1591

        public int LogCount
        {
            get {
                return _logCount;
            }
            set {
                _logCount = value;
                RaisePropertyChanged(nameof(LogCount));
                OnNodeModified(true);
            }
        }        

        public LogSeverity LogMinSeverity
        {
            get {
                return _logMinSeverity;
            }
            set {
                _logMinSeverity = value;
                RaisePropertyChanged(nameof(LogMinSeverity));
                OnNodeModified(true);
            }
        }

        public ProgressingTask RegisterOnActionLogChanged(ProgressingTask nodeProgressing)
        {
            if (!_runtimeStamp.ContainsKey(nodeProgressing.ActionLog))
                nodeProgressing.ActionLog.CollectionChanged += ActionLog_CollectionChanged;

            _runtimeStamp[nodeProgressing.ActionLog] = _timeStamp;
            return nodeProgressing;
        }

        private void ActionLog_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnNodeModified(true);
        }

        private void ClearOnActionLogChanged(long clearTime)
        {
            foreach (var logSource in _runtimeStamp.Where(g => g.Value < clearTime).Select(g => g.Key).ToArray())
            {
                logSource.CollectionChanged -= ActionLog_CollectionChanged;
                _runtimeStamp.Remove(logSource);
            }
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            ClearErrorsAndWarnings();
            ClearOnActionLogChanged(_timeStamp);
            _timeStamp = DateTime.Now.ToBinary();
            
            var callFilter = AstFactory.BuildFunctionCall(
                new Func<LogSeverity, object, ProgressingTask, int, LogMessage[]>(LogMessage.FilterBySeverity),
                new List<AssociativeNode>()
                {
                    MapEnum(LogMinSeverity),
                    inputAstNodes[1],
                    inputAstNodes[0].ToDynamicTaskProgressingFunc(RegisterOnActionLogChangedName),
                    AstFactory.BuildIntNode(LogCount)
                });
            return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), callFilter) };
        }

#pragma warning restore CS1591
    }
}
