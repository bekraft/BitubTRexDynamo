using System;
using System.Collections.Generic;
using System.Linq;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using TRex.Internal;

namespace TRex.Log
{
    /// <summary>
    /// A file logger capturing the logging output and redirected to file.
    /// </summary>
    [NodeName("File Logger")]
    [NodeDescription("Creates a new file logger with given template name.")]
    [NodeCategory("TRex.Log")]    
    [InPortTypes(typeof(string))]
    [OutPortTypes(typeof(Logger))]
    [IsDesignScriptCompatible]
    public class FileLog : BaseNodeModel
    {
        #region Internals
        private Serilog.Events.LogEventLevel _logEventLevel = Serilog.Events.LogEventLevel.Debug;
        #endregion

        /// <summary>
        /// New file logger.
        /// </summary>
        public FileLog()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("fileName", "Log file name")));
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("logger", "Logger instance")));

            RegisterAllPorts();
        }

        [JsonConstructor]
        FileLog(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        /// <summary>
        /// The minimum log event level.
        /// </summary>
        public Serilog.Events.LogEventLevel LogEventLevel
        {
            get {
                return _logEventLevel;
            }
            set {
                _logEventLevel = value;
                RaisePropertyChanged(nameof(LogEventLevel));
                OnNodeModified();
            }
        }

#pragma warning disable CS1591

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            ClearErrorsAndWarnings();
            if (InPorts.Any(p => !p.IsConnected))
            {
                return new[]
                {
                    AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode())
                };
            }

            var funcNode = AstFactory.BuildFunctionCall(
                new Func<string, string, object>(Logger.ByLogFileName),
                new List<AssociativeNode>() { inputAstNodes[0], AstFactory.BuildStringNode(Enum.GetName(typeof(Serilog.Events.LogEventLevel), LogEventLevel))});

            return new[]
            {
                AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), funcNode)
            };
        }

#pragma warning restore CS1591 
    }
}
