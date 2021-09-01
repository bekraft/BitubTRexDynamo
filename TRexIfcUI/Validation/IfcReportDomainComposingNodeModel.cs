using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Dynamo.Graph.Nodes;

using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;

using TRex.Internal;
using Dynamo.Utilities;

namespace TRex.Validation
{
    /// <summary>
    /// List model with selection of partial <see cref="IfcReportDomain"/> flags.
    /// </summary>
    [NodeName("IfcReport Domain")]
    [NodeDescription("Composing reporting validation failures for filtering reports")]
    [OutPortTypes(typeof(IfcReportDomain))]
    [NodeCategory("TRex.Validation")]
    [IsDesignScriptCompatible]
    public class IfcReportDomainComposingNodeModel : BaseNodeModel
    {
        #region Internals

        private IfcReportDomain[] _flags = new IfcReportDomain[] { };

        [JsonConstructor]
        IfcReportDomainComposingNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            Items.AddRange(Enum.GetValues(typeof(IfcReportDomain)).Cast<IfcReportDomain>());
        }

        #endregion

        /// <summary>
        /// New <see cref="IfcReportDomain"/> custom node model.
        /// </summary>
        public IfcReportDomainComposingNodeModel()
        {
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("reportDomainType", "Reporting domain filter")));
            RegisterAllPorts();
            Items.AddRange(Enum.GetValues(typeof(IfcReportDomain)).Cast<IfcReportDomain>());
        }

#pragma warning disable CS1591

        [JsonIgnore]
        public ObservableCollection<IfcReportDomain> Items { get; } = new ObservableCollection<IfcReportDomain>();

        public IfcReportDomain[] Selected
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
            // TODO Exception in EnumExtensions in Dynamo
            //var actionComposing = MapEnum(Selected.Aggregate(IfcReportDomain.Pass, (a, b) => a | b));
            var composedFlag = AstFactory.BuildStringNode(Selected.Aggregate(IfcReportDomain.None, (a, b) => a | b).ToString());
            return new AssociativeNode[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), composedFlag) };
        }

#pragma warning restore CS1591
    }
}
