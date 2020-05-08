using System;
using System.Collections.Generic;
using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;
using Dynamo.Engine;
using System.Linq;
using System.Windows.Input;

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

        /// <summary>
        /// Gets the provided scene contexts from input node(s).
        /// </summary>
        /// <param name="engineController">Dynamo engine controller</param>
        /// <param name="portName">InPort name</param>
        /// <returns></returns>
        public T[] GetCachedInput<T>(string portName, EngineController engineController)
        {
            return GetCachedInput<T>(InPorts.First(p => p.Name.Equals(portName)).Index, engineController);
        }

        /// <summary>
        /// Gets the provided scene contexts from input node(s).
        /// </summary>
        /// <param name="engineController">Dynamo engine controller</param>
        /// <param name="inPortNo">InPort number</param>
        /// <returns></returns>
        public T[] GetCachedInput<T>(int inPortNo, EngineController engineController)
        {
            var nodes = InPorts[inPortNo].Connectors.Select(c => (c.Start.Index, c.Start.Owner));
            var ids = nodes.Select(n => n.Owner.GetAstIdentifierForOutputIndex(n.Index).Name);

            var data = ids.Select(id => engineController.GetMirror(id).GetData());
            return data.SelectMany(d =>
            {
                if (d.IsCollection)
                    return d.GetElements().Select(e => e.Data).OfType<T>().ToArray();
                else if (d.Data is T obj)
                    return new T[] { obj };
                else
                    return new T[] { };
            }).ToArray();
        }

        /// <summary>
        /// Gets the provided scene contexts from output node(s).
        /// </summary>
        /// <param name="engineController">Dynamo engine controller</param>
        /// <param name="portName">OutPort name</param>
        /// <returns></returns>
        public T[] GetCachedOutput<T>(string portName, EngineController engineController)
        {
            return GetCachedOutput<T>(InPorts.First(p => p.Name.Equals(portName)).Index, engineController);
        }

        /// <summary>
        /// Gets the provided scene contexts from output node(s).
        /// </summary>
        /// <param name="engineController">Dynamo engine controller</param>
        /// <param name="outPortNo">OutPort number</param>
        /// <returns></returns>
        public T[] GetCachedOutput<T>(int outPortNo, EngineController engineController)
        {
            var data = engineController.GetMirror(GetAstIdentifierForOutputIndex(outPortNo).Name)?.GetData();
            if (null == data)
            {
                return new T[] { };
            }
            else
            {
                if (data.IsCollection)
                    return data.GetElements().Select(e => e.Data).OfType<T>().ToArray();
                else if (data.Data is T obj)
                    return new T[] { obj };
                else
                    return new T[] { };
            }
        }

        protected AssociativeNode BuildEnumNameNode<T>(T n) where T : Enum
        {
            return AstFactory.BuildStringNode(Enum.GetName(typeof(T), n));
        }
    }

#pragma warning restore CS1591
}
