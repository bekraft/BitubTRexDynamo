using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Internal;

namespace TRex.Export
{
    /// <summary>
    /// Unit scale node.
    /// </summary>
    [NodeName("Unit scale")]
    [NodeDescription("Unit per meter creation node used by scene build and export.")]
    [NodeCategory("TRex.Export")]
    [InPortTypes(new string[] { nameof(UnitScale) })]
    [OutPortTypes(typeof(UnitScale))]
    [IsDesignScriptCompatible]
    public class UnitScaleNodeModel : BaseNodeModel
    {
#pragma warning disable CS1591

        public UnitScaleNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("nested", "Nest an existing model scale", AstFactory.BuildNullNode())));

            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("unitScale", "Model units per meter")));

            RegisterAllPorts();

            unitScale = UnitScale.defined.Values.FirstOrDefault();
        }

        #region Internals

        private UnitScale unitScale;
        
        [JsonConstructor]
        UnitScaleNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        internal static AssociativeNode BuildUnitScaleNode(UnitScale us)
        {
            if (null != us)
            {
                return AstFactory.BuildFunctionCall(
                    new Func<string, string, float, UnitScale>(UnitScale.ByData),
                    new List<AssociativeNode>()
                    {
                    AstFactory.BuildStringNode(us.Reference),
                    AstFactory.BuildStringNode(us.Name),
                    AstFactory.BuildDoubleNode(us.UnitsPerMeter)
                    });
            }
            else
            {
                return AstFactory.BuildNullNode();
            }
        }

        #endregion

        public UnitScale SelectedUnitScale
        {
            get {
                return unitScale;
            }
            set {
                unitScale = value;
                RaisePropertyChanged(nameof(UnitScale));
                OnNodeModified(true);
            }
        }

        [JsonIgnore]
        public ObservableCollection<UnitScale> ItemsUnitScale { get; } = new ObservableCollection<UnitScale>(UnitScale.defined.Values);

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            ClearErrorsAndWarnings();
            return BuildResult(new[]
            {
                AstFactory.BuildFunctionCall(
                    new Func<UnitScale, UnitScale, UnitScale>(UnitScale.ByModelUnitScale),
                    new List<AssociativeNode>()
                    {
                        inputAstNodes[0],
                        BuildUnitScaleNode(unitScale)
                    }
                )
            });
        }

#pragma warning restore CS1591
    }
}
