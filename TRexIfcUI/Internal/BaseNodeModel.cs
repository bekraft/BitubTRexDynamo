using System;
using System.Collections.Generic;
using System.Linq;

using Dynamo.Graph.Nodes;
using Dynamo.Engine;

using ProtoCore.AST.AssociativeAST;
using ProtoCore.Mirror;

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

        protected AssociativeNode MapEnum(Enum value)
        {
            return AstFactory.BuildFunctionCall(
                new Func<string, string, object>(GlobalArgumentService.DeserializeEnum),
                new List<AssociativeNode>() 
                { 
                    AstFactory.BuildStringNode(value.GetType().FullName), 
                    AstFactory.BuildStringNode(value.ToString()) 
                });
        }

        protected AssociativeNode CacheObjects(params object[] args)
        {
            return AstFactory.BuildFunctionCall(
                        new Func<string, object[]>(GlobalArgumentService.GetArgs),
                        new List<AssociativeNode>() 
                        { 
                            AstFactory.BuildStringNode(GlobalArgumentService.PutArguments(args)) 
                        });
        }

        protected AssociativeNode CacheObject(object arg)
        {
            return AstFactory.BuildFunctionCall(
                        new Func<string, object>(GlobalArgumentService.GetArg),
                        new List<AssociativeNode>() 
                        { 
                            AstFactory.BuildStringNode(GlobalArgumentService.PutArguments(arg)) 
                        });
        }

        /// <summary>
        /// Gets the provided scene contexts from input node(s).
        /// </summary>
        /// <param name="engineController">Dynamo engine controller</param>
        /// <param name="portName">InPort name</param>
        /// <returns>An array of inputs</returns>
        public T[] GetCachedInput<T>(string portName, EngineController engineController)
        {
            return GetCachedInput<T>(InPorts.First(p => p.Name.Equals(portName)).Index, engineController);
        }

        /// <summary>
        /// Gets the provided scene contexts from input node(s).
        /// </summary>
        /// <param name="engineController">Dynamo engine controller</param>
        /// <param name="inPortNo">InPort number</param>
        /// <returns>An array of inputs</returns>
        public T[] GetCachedInput<T>(int inPortNo, EngineController engineController)
        {
            var nodes = InPorts[inPortNo].Connectors.Select(c => (c.Start.Index, c.Start.Owner));
            var ids = nodes.Select(n => n.Owner.GetAstIdentifierForOutputIndex(n.Index).Name);

            var data = ids.Select(id => engineController.GetMirror(id).GetData());
            return data.SelectMany(Unwrap<T>).ToArray();
        }

        private static T[] Unwrap<T>(MirrorData data)
        {
            if (data.IsCollection)
                return data.GetElements().Select(e => e.Data).OfType<T>().ToArray();
            else if (data.Data is T obj)
                return new T[] { obj };
            else
                return new T[] { };
        }

        /// <summary>
        /// Gets the provided scene contexts from input node(s).
        /// </summary>
        /// <param name="engineController">Dynamo engine controller</param>
        /// <param name="inPortNo">InPort number</param>
        /// <returns>An array of inputs with their AST identifier</returns>
        public AstValue<T>[] GetCachedAstInput<T>(int inPortNo, EngineController engineController)
        {
            var nodes = InPorts[inPortNo].Connectors.Select(c => (c.Start.Index, c.Start.Owner));
            var ids = nodes.Select(n => n.Owner.GetAstIdentifierForOutputIndex(n.Index).Name);
            return ids.SelectMany(id => UnwrapAstValue<T>(engineController.GetMirror(id).GetData(), id)).ToArray();
        }

        private static AstValue<T>[] UnwrapAstValue<T>(MirrorData data, string astId)
        {
            if (data.IsCollection)
                return data.GetElements().Select(e => e.Data).OfType<T>().Select((d, index) => new AstValue<T>(astId, d, index)).ToArray();
            else if (data.Data is T obj)
                return new AstValue<T>[] { new AstValue<T>(astId, obj) };
            else
                return new AstValue<T>[] { };
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
                return new T[] { };
            else
                return Unwrap<T>(data);
        }

        protected AssociativeNode BuildEnumNameNode<T>(T n) where T : Enum
        {
            return AstFactory.BuildStringNode(Enum.GetName(typeof(T), n));
        }
    }

#pragma warning restore CS1591
}
