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
    /// Node model for composing a <see cref="LogReason"/>.
    /// </summary>
    [NodeName("Log Reason")]
    [NodeDescription("Composing log reasons for filtering logs")]
    [OutPortTypes(typeof(LogReason))]
    [NodeCategory("TRexIfc.Log")]
    [IsDesignScriptCompatible]
    public class LogReasonComposingNodeModel : BaseNodeModel
    {
        #region Internals

        private LogReason[] _flags = new LogReason[] { };

        [JsonConstructor]
        LogReasonComposingNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            Items.AddRange(Enum.GetValues(typeof(LogReason)).Cast<LogReason>());
        }

        #endregion

        /// <summary>
        /// New action composing node model.
        /// </summary>
        public LogReasonComposingNodeModel()
        {
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("logReasonFlag", "Log reason flag")));
            RegisterAllPorts();

            Items.AddRange(Enum.GetValues(typeof(LogReason)).Cast<LogReason>());
        }

#pragma warning disable CS1591

        [JsonIgnore]
        public ObservableCollection<LogReason> Items { get; } = new ObservableCollection<LogReason>();

        public LogReason[] Selected
        {
            get {
                return _flags;
            }
            set {
                _flags = value;
                RaisePropertyChanged(nameof(Selected));
                OnNodeModified(true);
            }
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            ClearErrorsAndWarnings();
            // TODO Exception in EnumExtensions in Dynamo
            //var composedFlag = MapEnum(Selected.Aggregate(LogReason.None, (a, b) => a | b));
            var composedFlag = AstFactory.BuildStringNode(Selected.Aggregate(LogReason.None, (a, b) => a | b).ToString());
            return new AssociativeNode[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), composedFlag) };
        }

#pragma warning restore CS1591
    }
}
