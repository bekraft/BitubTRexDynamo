using System;
using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Log;
using TRex.Store;
using TRex.Internal;

using TRex.UI.Customization;

using Bitub.Xbim.Ifc.Transform;

namespace TRex.Task
{
    /// <summary>
    /// IFC representation refactoring. Will unwrap all representations with multiple items by
    /// cloning the hosting product. Will help if ITO toolchains have problems with geometrical information derivation.
    /// </summary>
    [NodeName("Ifc Representation Refactor")]
    [NodeDescription("Decomposes representations with multiple items per product within the same context.")]
    [NodeCategory("TRex.Task")]
    [InPortTypes(nameof(String), nameof(IfcAuthorMetadata), nameof(String), nameof(LogReason), nameof(IfcModel))]
    [OutPortTypes(nameof(IfcModel))]
    [IsDesignScriptCompatible]
    public class IfcRepresentationRefactorTransformNodeModel : CancelableProgressingOptionNodeModel<IfcProductRefactorOption>
    {
#pragma warning disable CS1591

        public IfcRepresentationRefactorTransformNodeModel()
        {
            InPorts.Add(
                new PortModel(PortType.Input, this, new PortData("context", "Representation context", AstFactory.BuildStringNode("Body"))));
            InPorts.Add(
                new PortModel(PortType.Input, this, new PortData("author", "Author meta data", AstFactory.BuildNullNode())));
            InPorts.Add(
                new PortModel(PortType.Input, this, new PortData("nameAddon", "Fragment name of canonical full name", AstFactory.BuildNullNode())));
            InPorts.Add(
                new PortModel(PortType.Input, this, new PortData("logFilter", "Log reason type filtering", MapEnum(LogReason.Any))));
            InPorts.Add(
                new PortModel(PortType.Input, this, new PortData("ifcModel", "IFC input model")));

            OutPorts.Add(
                new PortModel(PortType.Output, this, new PortData("ifcModel", "IFC output model")));

            RegisterAllPorts();

            IsCancelable = true;
            LogReasonMask = LogReason.Changed;
            Selected = GetInitialOptions().First();
        }

        #region Internals

        [JsonConstructor]
        IfcRepresentationRefactorTransformNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            IsCancelable = true;
            LogReasonMask = LogReason.Changed;
        }

        protected override IEnumerable<IfcProductRefactorOption> GetInitialOptions() => new[]
        {
            new IfcProductRefactorOption(
                "OptionDecompose",
                "Decompose representations only", 
                ProductRefactorStrategy.DecomposeMultiItemRepresentations),
            new IfcProductRefactorOption(
                "OptionDecomposeWithAssembly",
                "Decompose representations with IfcElementAssembly",
                ProductRefactorStrategy.DecomposeMultiItemRepresentations
                | ProductRefactorStrategy.DecomposeWithEntityElementAssembly),
            new IfcProductRefactorOption(
                "OptionDecomposeAllWithAssembly",
                "Decompose all representations with IfcElementAssembly",
                ProductRefactorStrategy.DecomposeMultiItemRepresentations 
                | ProductRefactorStrategy.DecomposeMappedRepresentations
                | ProductRefactorStrategy.DecomposeWithEntityElementAssembly)
        };

        #endregion

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAst)
        {
            BeforeBuildOutputAst();
            if (!IsAcceptable(inputAst))
            {
                ResetState();
                return BuildNullResult();
            }

            // AST for logger retrieval
            var astGetLogger = AstFactory.BuildFunctionCall(
                new Func<IfcModel, Logger>(IfcModel.GetLogger),
                new List<AssociativeNode>() 
                { 
                    inputAst[4] 
                }
            );

            // AST for transform creation
            var astCreateTransform = AstFactory.BuildFunctionCall(
                new Func<Logger, IfcAuthorMetadata, string[], object, IfcTransform>(IfcTransform.NewRepresentationRefactorTransform),
                new List<AssociativeNode>() 
                { 
                    astGetLogger, 
                    inputAst[1], 
                    inputAst[0], 
                    BuildEnumNameNode(Selected?.Data ?? default(ProductRefactorStrategy)) 
                }
            );

            // Create transformation delegate
            var astCreateTransformDelegate = AstFactory.BuildFunctionCall(
                new Func<IfcModel, IfcTransform, string, object, IfcModel>(IfcTransform.BySourceAndTransform),
                new List<AssociativeNode>() 
                { 
                    inputAst[4], 
                    astCreateTransform, 
                    inputAst[2], 
                    inputAst[3] 
                }
            );

            return BuildResult(astCreateTransformDelegate.ToDynamicTaskProgressingFunc(ProgressingTaskMethodName));
        }

#pragma warning restore CS1591
    }
}
