using System;
using System.Collections.Generic;
using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;


namespace Internal
{
    // Disable comment warning
#pragma warning disable CS1591

    public class BaseNodeModel : NodeModel
    {
        protected BaseNodeModel() : base()
        {
        }

        protected BaseNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        public Func<T1, R> MapInputs<T1, R>(Func<T1, R> f, params string[] inputNames)
        {
            return f;
        }

        protected AssociativeNode WrapName<T>(T n) where T : Enum
        {
            return AstFactory.BuildStringNode(Enum.GetName(typeof(T), n));
        }
    }

#pragma warning restore CS1591
}
