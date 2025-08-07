using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Bitub.Dto;

namespace TRex.Internal;

[IsVisibleInDynamoLibrary(false)]
public static class AstFactoryExtensions
{
    [IsVisibleInDynamoLibrary(false)]
    public static string[] ToQualifiedMethodName(this Type t, string methodName)
    {
        return new [] { t.FullName ?? "", methodName };
    }

    [IsVisibleInDynamoLibrary(false)]
    public static AssociativeNode ToDynamicTaskProgressingFunc(this AssociativeNode taskProgressingNode, params string[] funcQualifier)
    {
        return AstFactory.BuildFunctionCall(
            new Func<Qualifier, ProgressingTask, ProgressingTask>(DynamicDelegation.CallDynamicTaskConsumer),
            new List<AssociativeNode>()
            {
                    AstFactory.BuildFunctionCall(
                        new Func<string[], Qualifier>(DynamicDelegation.BuildQualifier),
                        new List<AssociativeNode>()
                        {
                            AstFactory.BuildExprList(
                                funcQualifier.Select(AstFactory.BuildStringNode).Cast<AssociativeNode>().ToList())
                        }),
                    taskProgressingNode
            });
    }
    
    [IsVisibleInDynamoLibrary(false)]
    public static AssociativeNode ToEnumNameNode<T>(this T n) where T : Enum
    {
        var serialized = Enum.GetName(typeof(T), n);            
        return AstFactory.BuildStringNode(serialized ?? n.ToString());
    }

    [IsVisibleInDynamoLibrary(false)]
    public static AssociativeNode ToStringNode(this string? str)
    {
        return null != str ? AstFactory.BuildStringNode(str) : AstFactory.BuildNullNode();
    }
    
    [IsVisibleInDynamoLibrary(false)]
    public static IEnumerable<AssociativeNode> BuildNullAssignment<TModel>(this TModel model, params int[] outportIndexes) where TModel : BaseNodeModel
    {
        var appliedOutportIndexes = outportIndexes;
        if (appliedOutportIndexes.Length == 0)
            appliedOutportIndexes = new[] { 0 };

        return appliedOutportIndexes
            .Select(p =>
                AstFactory.BuildAssignment(model.GetAstIdentifierForOutputIndex(p), AstFactory.BuildNullNode()))
            .ToArray();
    }
}