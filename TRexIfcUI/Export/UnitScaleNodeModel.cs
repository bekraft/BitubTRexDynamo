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

            unitScale = UnitScale.defined.FirstOrDefault();
        }

        #region Internals

        private UnitScale unitScale;
        
        [JsonConstructor]
        UnitScaleNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        AssociativeNode BuildUnitScaleNode(UnitScale us)
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

        #endregion

        public UnitScale UnitScale
        {
            get {
                return unitScale;
            }
            set {
                unitScale = value;
                RaisePropertyChanged(nameof(UnitScale));
                OnNodeModified();
            }
        }

        public ObservableCollection<UnitScale> ItemsUnitScale { get; } = new ObservableCollection<UnitScale>(UnitScale.defined);

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            ClearErrorsAndWarnings();
            var inputs = inputAstNodes.ToArray();

            return BuildResult(new[]
            {
                AstFactory.BuildFunctionCall(
                    new Func<UnitScale, UnitScale, UnitScale>(UnitScale.ByModelUnitScale),
                    new List<AssociativeNode>()
                    {
                        inputs[0],
                        BuildUnitScaleNode(unitScale)
                    }
                )
            });
        }

#pragma warning restore CS1591
    }
}
