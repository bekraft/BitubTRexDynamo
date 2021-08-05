using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Dynamo.Graph.Nodes;

using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;

using TRex.Internal;

namespace TRex.Data
{
    /// <summary>
    /// Interactive data grid visualising incoming data.
    /// </summary>
    [NodeName("Data Preview")]
    [NodeDescription("Interactive table grid displaying incoming data with a given count threshold.")]
    [InPortTypes(typeof(List<object>))]
    [OutPortTypes(typeof(object[][]))]
    [NodeCategory("TRex.Data")]
    [IsDesignScriptCompatible]
    public class DataTablePreviewNodeModel : BaseNodeModel
    {
        #region Internals

        private ObservableCollection<object> dataTable;
        private long count;
        private long minCount;
        private long maxCount;

        [JsonConstructor]
        DataTablePreviewNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            DataTable = new ObservableCollection<object>();
            Init();
        }

        internal void DataTable_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnNodeModified(true);
        }

        private void Init()
        {
            minCount = 1;
            maxCount = 100;
            count = 10;
        }

        #endregion

        /// <summary>
        /// New interactive logging table.
        /// </summary>
        public DataTablePreviewNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("in", "Input grid data (i.e. LogMessage)")));
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("out", "Data property preview")));
            RegisterAllPorts();

            DataTable = new ObservableCollection<object>();
            Init();
        }

#pragma warning disable CS1591

        public long MinCount
        {
            get {
                return minCount;
            }
            set {
                var old = minCount;
                minCount = value;
                if (old != minCount)
                {
                    RaisePropertyChanged(nameof(MinCount));
                    var newCount = Math.Max(minCount, count);
                    if (newCount != count)
                        Count = newCount;
                    else                    
                        OnNodeModified(true);
                }
            }
        }

        public long MaxCount
        {
            get {
                return maxCount;
            }
            set {
                var old = maxCount;
                maxCount = value;
                if (old != maxCount)
                {
                    RaisePropertyChanged(nameof(MaxCount));
                    var newCount = Math.Min(maxCount, count);
                    if (newCount != count)
                        Count = newCount;
                    else
                        OnNodeModified(true);

                }
            }
        }

        public long Count
        {
            get {
                return count;
            }
            set {
                var old = count;
                count = value;
                if (old != count)
                {
                    RaisePropertyChanged(nameof(Count));
                    OnNodeModified(true);
                }
            }
        }

        [JsonIgnore]
        public ObservableCollection<object> DataTable
        {
            get {
                return dataTable;
            }
            set {
                if (null != dataTable)
                    dataTable.CollectionChanged -= DataTable_CollectionChanged;

                dataTable = value;

                if (null != dataTable)
                    dataTable.CollectionChanged += DataTable_CollectionChanged;

                RaisePropertyChanged(nameof(DataTable));
            }
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            ClearErrorsAndWarnings();
            // Get current logging data from event source
            AssociativeNode dataReturn = AstFactory.BuildFunctionCall(
                new Func<List<object>, int, object[][]>(DynamicArgumentDelegation.DecomposeArray),
                new List<AssociativeNode>()
                {
                    inputAstNodes[0],
                    AstFactory.BuildIntNode(Count)
                });

            return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), dataReturn) };
        }

#pragma warning restore CS1591
    }
}
