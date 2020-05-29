using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dynamo.Graph.Nodes;

using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;

using Internal;
using Dynamo.Utilities;

namespace Log
{
    /// <summary>
    /// Node model for composing and action type flag.
    /// </summary>
    [NodeName("Action type")]
    [NodeDescription("Action type composer for transformation logging")]
    [OutPortTypes(typeof(ActionType))]
    [NodeCategory("TRexIfc.Log")]
    [IsDesignScriptCompatible]
    public class ActionTypeComposingNodeModel : BaseNodeModel
    {
        #region Internals

        private ActionType[] _selectedFlags = new ActionType[] { };

        [JsonConstructor]
        ActionTypeComposingNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            Items.AddRange(Enum.GetValues(typeof(ActionType)).Cast<ActionType>());
        }

        #endregion

        /// <summary>
        /// New action composing node model.
        /// </summary>
        public ActionTypeComposingNodeModel()
        {
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("actionFlag", "Action type flag")));
            RegisterAllPorts();

            Items.AddRange(Enum.GetValues(typeof(ActionType)).Cast<ActionType>());
        }

#pragma warning disable CS1591

        [JsonIgnore]
        public ObservableCollection<ActionType> Items { get; } = new ObservableCollection<ActionType>();

        public ActionType[] Selected
        {
            get {
                return _selectedFlags;
            }
            set {
                _selectedFlags = value;
                RaisePropertyChanged(nameof(Selected));
                OnNodeModified(true);
            }
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            var actionComposing = MapEnum(Selected.Aggregate(ActionType.None, (a, b) => a | b));
            return new AssociativeNode[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), actionComposing) };
        }

#pragma warning restore CS1591
    }
}
