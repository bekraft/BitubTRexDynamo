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
using TRex.Geom;
using Bitub.Ifc.Transform.Requests;

namespace TRex.Task
{
    /// <summary>
    /// IFC Property set removal transformation. Will remove given property sets
    /// by their names complete from input model and returns a modifed output model.
    /// Modifications (addings and changes are tagged by editor authoring credentials). 
    /// </summary>
    [NodeName("Ifc ")]
    [NodeDescription("Refactors representations with multiple items per product within the same context.")]
    [NodeCategory("TRex.Task")]
    [InPortTypes(new string[] { nameof(String), nameof(IfcAuthorMetadata), nameof(String), nameof(LogReason), nameof(IfcModel) })]
    [OutPortTypes(typeof(IfcModel))]
    [IsDesignScriptCompatible]
    public class IfcRepresentationRefactorTransformNodeModel : CancelableProgressingOptionNodeModel<string>
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
            Selected = RefactorStrategyOptions.Keys.First();
        }

        #region Internals

        [JsonConstructor]
        IfcRepresentationRefactorTransformNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            IsCancelable = true;
            LogReasonMask = LogReason.Changed;
        }

        private IDictionary<string, ProductRepresentationRefactorStrategy> RefactorStrategyOptions = new Dictionary<string, ProductRepresentationRefactorStrategy>()
        {
            { "Refactor representation items", ProductRepresentationRefactorStrategy.ReplaceMultipleRepresentations },
            { "Refactor & insert new IfcElementAssembly", ProductRepresentationRefactorStrategy.RefactorWithEntityElementAssembly }
        };

        protected override IEnumerable<string> GetInitialOptions() => RefactorStrategyOptions.Keys;

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
                    inputAst[3] 
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
                    BuildEnumNameNode(RefactorStrategyOptions[Selected]) 
                }
            );

            // Create transformation delegate
            var astCreateTransformDelegate = AstFactory.BuildFunctionCall(
                new Func<IfcModel, IfcTransform, string, object, IfcModel>(IfcTransform.BySourceAndTransform),
                new List<AssociativeNode>() 
                { 
                    inputAst[3], 
                    astCreateTransform, 
                    inputAst[2], 
                    inputAst[4] 
                }
            );

            return BuildResult(astCreateTransformDelegate.ToDynamicTaskProgressingFunc(ProgressingTaskMethodName));
        }

#pragma warning restore CS1591
    }
}
