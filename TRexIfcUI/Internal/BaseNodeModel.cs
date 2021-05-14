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

        protected void WarnForMissingInputs(bool withDefaults = false)
        {
            Warning(string.Format("Missing connected ports ({0})", 
                string.Join(",", InPorts.Where(p => !p.IsConnected && (withDefaults || p.DefaultValue == null)).Select(p => p.Name))));
        }

        protected void ErrorForMissingInputs(bool withDefaults = false)
        {
            Error(string.Format("Missing connected ports ({0})",
                string.Join(",", InPorts.Where(p => !p.IsConnected && (withDefaults || p.DefaultValue == null)).Select(p => p.Name))));
        }

        protected AssociativeNode MapEnum(Enum value)
        {
            return AstFactory.BuildFunctionCall(
                new Func<string, string, object>(DynamicArgumentDelegation.TryParseEnum),
                new List<AssociativeNode>() 
                { 
                    AstFactory.BuildStringNode(value.GetType().FullName), 
                    AstFactory.BuildStringNode(value.ToString()) 
                });
        }

        protected AssociativeNode CacheObjects(params object[] args)
        {
            return AstFactory.BuildFunctionCall(
                        new Func<string, object[]>(DynamicArgumentDelegation.GetArgs),
                        new List<AssociativeNode>() 
                        { 
                            AstFactory.BuildStringNode(DynamicArgumentDelegation.PutArguments(args)) 
                        });
        }

        protected AssociativeNode CacheObject(object arg)
        {
            return AstFactory.BuildFunctionCall(
                        new Func<string, object>(DynamicArgumentDelegation.GetArg),
                        new List<AssociativeNode>() 
                        { 
                            AstFactory.BuildStringNode(DynamicArgumentDelegation.PutArguments(arg)) 
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

            var data = ids.Select(id => engineController.GetMirror(id)?.GetData());
            return data.SelectMany(Unwrap<T>).Distinct().ToArray();
        }

        private static T[] Unwrap<T>(MirrorData data)
        {
            if (data?.IsCollection ?? false)
                return data.GetElements().SelectMany(e =>
                {
                    if (e.IsCollection)
                        return Unwrap<T>(e);
                    else if (e.Data is T obj)
                        return new T[] { obj };
                    else
                        return new T[] { };
                }).ToArray();
            else if (data?.Data is T obj)
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
            var idNodes = nodes.Select(n => n.Owner.GetAstIdentifierForOutputIndex(n.Index));
            return idNodes.SelectMany(idn => UnwrapAstValue<T>(engineController.GetMirror(idn.Name)?.GetData(), idn)).ToArray();
        }

        private static AstValue<T>[] UnwrapAstValue<T>(MirrorData data, IdentifierNode idn, params int[] indexes)
        {
            if (data?.IsCollection ?? false)
            {
                return data.GetElements().SelectMany((e, index) =>
                {
                    if (e.IsCollection)
                        return UnwrapAstValue<T>(e, idn, indexes.Concat(new int[] { index }).ToArray());
                    else if (e.Data is T obj)
                        return new AstValue<T>[] { new AstValue<T>(idn.Name, obj, indexes.Concat(new int[] { index }).ToArray()) };
                    else
                        return new AstValue<T>[] { };
                }).ToArray();
            }
            else if (data?.Data is T obj)
                return new AstValue<T>[] { new AstValue<T>(idn.Name, obj) };
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
                return Unwrap<T>(data).Distinct().ToArray();
        }

        protected AssociativeNode NodeToExprList<N>(AssociativeNode n) where N : AssociativeNode
        {
            if (n is N valueNode)
                return AstFactory.BuildExprList(new List<AssociativeNode>() { valueNode });
            else
                return n;
        }

        protected AssociativeNode BuildEnumNameNode<T>(T n) where T : Enum
        {
            return AstFactory.BuildStringNode(Enum.GetName(typeof(T), n));
        }
    }

#pragma warning restore CS1591
}
