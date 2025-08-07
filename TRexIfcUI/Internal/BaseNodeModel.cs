using System;
using System.Collections.Generic;
using System.Linq;

using Dynamo.Graph.Nodes;
using Dynamo.Engine;

using ProtoCore.AST.AssociativeAST;
using ProtoCore.Mirror;
using TRex.Log;
using TRex.Store;

namespace TRex.Internal;

public class BaseNodeModel : NodeModel
{
    protected BaseNodeModel() : base()
    { }

    protected BaseNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    { }

    protected bool IsAcceptableWithRuntimeDefaults(List<AssociativeNode> inputAstNodes, params AssociativeNode?[] runtimeDefaultParams)
    {
        if (IsPartiallyApplied)
        {
            foreach (PortModel port in InPorts.Where(p => !p.IsConnected && !p.UsingDefaultValue))
            {
                if (port.Index < runtimeDefaultParams.Length)
                {
                    if (null == runtimeDefaultParams[port.Index])
                        return false;
                    else
                        inputAstNodes[port.Index] = runtimeDefaultParams[port.Index]!;
                }
            }
        }
        return true;
    }

    protected IEnumerable<AssociativeNode> BuildErrorOutput()
    {
        return (IEnumerable<AssociativeNode>) new BinaryExpressionNode[1]
        {
            AstFactory.BuildAssignment((AssociativeNode) this.AstIdentifierForPreview, AstFactory.BuildFunctionCall("%nodeAstFailed", new List<AssociativeNode>()
            {
                (AssociativeNode) AstFactory.BuildStringNode(this.GetType().ToString())
            }))
        };
    }

    protected bool IsAcceptable(List<AssociativeNode> inputAstNodes, params int[] nullAcceptablePorts)
    {
        if (IsPartiallyApplied)
        {
            Array.Sort(nullAcceptablePorts);
            foreach (PortModel port in InPorts.Where(p => !p.IsConnected && !p.UsingDefaultValue))
            {
                if (0 > Array.BinarySearch(nullAcceptablePorts, port.Index))
                    // If not held by acceptablePorts => not acceptable
                    return false;
            }
        }
        return true;
    }

    protected string[] GetOpenPortNames(bool withDefaults = false)
    {
        return InPorts
            .Where(p => !p.IsConnected && (withDefaults || !p.UsingDefaultValue))
            .Select(p => p.Name)
            .ToArray();
    }

    protected void WarnForMissingInputs(bool withDefaults = false)
    {
        Warning($"Missing connected ports ({string.Join(", ", GetOpenPortNames(withDefaults))})", true);
    }
    
    protected void ErrorForMissingInputs(bool withDefaults = false)
    {
        Error($"Missing connected ports ({string.Join(", ", GetOpenPortNames(withDefaults))})");
    }
    
    protected static AssociativeNode MapEnumIntoNode(Enum value)
    {
        return AstFactory.BuildFunctionCall(
            new Func<string, string?, object?>(DynamicArgumentDelegation.TryParseEnum),
            new List<AssociativeNode>() 
            { 
                AstFactory.BuildStringNode(value.GetType().FullName), 
                AstFactory.BuildStringNode(Enum.GetName(value.GetType(), value)) 
            });
    }

    protected static AssociativeNode CacheObjects(params object[] args)
    {
        return AstFactory.BuildFunctionCall(
                    new Func<string, object[]?>(DynamicArgumentDelegation.GetArgs),
                    new List<AssociativeNode>() 
                    { 
                        AstFactory.BuildStringNode(DynamicArgumentDelegation.PutArguments(args)) 
                    });
    }

    protected static AssociativeNode CacheObject(object arg)
    {
        return AstFactory.BuildFunctionCall(
                    new Func<string, object?>(DynamicArgumentDelegation.GetArg),
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
    public T[] GetCachedInput<T>(int inPortNo, EngineController? engineController)
    {
        if (null != engineController)
        {
            var nodes = InPorts[inPortNo].Connectors.Select(c => (c.Start.Index, c.Start.Owner));
            var ids = nodes.Select(n => n.Owner.GetAstIdentifierForOutputIndex(n.Index).Name);

            var data = ids.Select(id => engineController.GetMirror(id)?.GetData());
            return data.SelectMany(Unwrap<T>).Distinct().ToArray();
        }
        else
        {
            return Array.Empty<T>();
        }
    }

    private static T[] Unwrap<T>(MirrorData? data)
    {
        if (data?.IsCollection ?? false)
        {
            return data.GetElements().SelectMany(e =>
            {
                if (e.IsCollection)
                    return Unwrap<T>(e);
                else if (e.Data is T obj)
                    return new T[] { obj };
                else
                    return new T[] { };
            }).ToArray();
        }
        else if (data?.Data is T obj)
        {
            return new T[] { obj };
        }
        else
        {
            return new T[] { };
        }
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

    private static AstValue<T>[] UnwrapAstValue<T>(MirrorData? data, IdentifierNode idn, params long[] indexes)
    {
        if (data?.IsCollection ?? false)
        {
            return data.GetElements().SelectMany((e, index) =>
            {
                if (e.IsCollection)
                    return UnwrapAstValue<T>(e, idn, indexes.Concat(new long[] { index }).ToArray());
                else if (e.Data is T obj)
                    return new AstValue<T>[] { new AstValue<T>(idn.Name, obj, indexes.Concat(new long[] { index }).ToArray()) };
                else
                    return new AstValue<T>[] { };
            }).ToArray();
        }
        else if (data?.Data is T obj)
        {
            return new AstValue<T>[] { new AstValue<T>(idn.Name, obj) };
        }
        else
        {
            return new AstValue<T>[] { };
        }
    }
    
    /// <summary>
    /// Gets wrapped logger from <see cref="Logger"/> from embedding <see cref="IfcModel"/>.
    /// </summary>
    /// <param name="ifcModelNode">The Ifc model node</param>
    /// <returns></returns>
    protected AssociativeNode GetLoggerFromIfcModel(AssociativeNode ifcModelNode)
    {
        return AstFactory.BuildFunctionCall(
            new Func<IfcModel, Logger>(IfcModel.GetLogger),
            new List<AssociativeNode>() { ifcModelNode }
        );
    }

    /// <summary>
    /// Wrap a single string node into a list of strings.
    /// </summary>
    /// <param name="stringNode">A node</param>
    /// <returns>If given node is string node, returns a node of list of string nodes, otherwise the node itself</returns>
    protected AssociativeNode NestingStringArray(AssociativeNode stringNode)
    {
        // Wrap single string into list
        if (stringNode is StringNode)
        {   // Rewrite input AST 
            return AstFactory.BuildExprList(new List<AssociativeNode>() { stringNode });
        }
        else
        {
            return stringNode;
        }
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
    public T[] GetCachedOutput<T>(int outPortNo, EngineController? engineController)
    {
        var data = engineController?.GetMirror(GetAstIdentifierForOutputIndex(outPortNo).Name)?.GetData();
        if (null == data)
        {
            return new T[] { };
        }
        else
        {
            return Unwrap<T>(data).Distinct().ToArray();
        }
    }

    protected static AssociativeNode NodeToExprList<N>(AssociativeNode n) where N : AssociativeNode
    {
        if (n is N valueNode)
            return AstFactory.BuildExprList(new List<AssociativeNode>() { valueNode });
        else
            return n;
    }

    protected static AssociativeNode BuildEnumNameNode<T>(T n) where T : Enum
    {
        var serialized = Enum.GetName(typeof(T), n);            
        return AstFactory.BuildStringNode(serialized ?? n.ToString());
    }

    protected AssociativeNode[] BuildNullResult()
    {
        return Enumerable
            .Range(0, OutPorts.Count)
            .Select(i => AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(i), AstFactory.BuildNullNode()))
            .Cast<AssociativeNode>()
            .ToArray();
    }

    protected AssociativeNode[] BuildResult(params AssociativeNode[] value)
    {
        return Enumerable
            .Range(0, OutPorts.Count)
            .Select(i => AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(i), (i < value.Length ? value[i] : AstFactory.BuildNullNode())))
            .Cast<AssociativeNode>()
            .ToArray();
    }
}