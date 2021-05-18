using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Bitub.Dto;

// Disable comment warning
#pragma warning disable CS1591

namespace TRex.Internal
{
    [IsVisibleInDynamoLibrary(false)]
    public static class AstFactoryExtensions
    {
        [IsVisibleInDynamoLibrary(false)]
        public static string[] ToQualifiedMethodName(this Type t, string methodName)
        {
            return new string[] { t.FullName, methodName };
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
                                    funcQualifier.Select(n => AstFactory.BuildStringNode(n)).Cast<AssociativeNode>().ToList())
                            }),
                        taskProgressingNode
                });

        }
    }
}

#pragma warning restore CS1591