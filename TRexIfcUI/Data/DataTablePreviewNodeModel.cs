using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Dynamo.Graph.Nodes;

using Newtonsoft.Json;
using ProtoCore.AST.AssociativeAST;

using Internal;

namespace Data
{
    /// <summary>
    /// Interactive data grid visualising incoming data.
    /// </summary>
    [NodeName("Data Preview")]
    [NodeDescription("Interactive table grid displaying incoming data with a given count threshold.")]
    [InPortTypes(typeof(List<object>))]
    [OutPortTypes(typeof(object[][]))]
    [NodeCategory("TRexIfc.Data")]
    [IsDesignScriptCompatible]
    public class DataTablePreviewNodeModel : BaseNodeModel
    {
        #region Internals

        private ObservableCollection<object> _dataTable;
        private int _count;
        private int _minCount;
        private int _maxCount;

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
            _minCount = 1;
            _maxCount = 100;
            _count = 10;
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

        public int MinCount
        {
            get {
                return _minCount;
            }
            set {
                var old = _minCount;
                _minCount = value;
                if (old != _minCount)
                {
                    RaisePropertyChanged(nameof(MinCount));
                    var newCount = Math.Max(_minCount, _count);
                    if (newCount != _count)
                        Count = newCount;
                    else                    
                        OnNodeModified(true);
                }
            }
        }

        public int MaxCount
        {
            get {
                return _maxCount;
            }
            set {
                var old = _maxCount;
                _maxCount = value;
                if (old != _maxCount)
                {
                    RaisePropertyChanged(nameof(MaxCount));
                    var newCount = Math.Min(_maxCount, _count);
                    if (newCount != _count)
                        Count = newCount;
                    else
                        OnNodeModified(true);

                }
            }
        }

        public int Count
        {
            get {
                return _count;
            }
            set {
                var old = _count;
                _count = value;
                if (old != _count)
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
                return _dataTable;
            }
            set {
                if (null != _dataTable)
                    _dataTable.CollectionChanged -= DataTable_CollectionChanged;

                _dataTable = value;

                if (null != _dataTable)
                    _dataTable.CollectionChanged += DataTable_CollectionChanged;

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
