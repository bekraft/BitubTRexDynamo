using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dynamo.Utilities;
using Autodesk.DesignScript.Geometry;

using Dynamo.Graph.Nodes;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using Internal;

using AstObjectValue = Internal.AstValue<object>;

namespace Task
{
#pragma warning disable CS1591

    public abstract class SelectableItemListNodeModel : BaseNodeModel
    {
        internal protected const int ID_ITEMS_IN = 0;

        /// <summary>
        /// New selective items node.
        /// </summary>
        protected SelectableItemListNodeModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("items", "Provided candidates")));
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("selected", "Selected candidates")));

            ArgumentLacing = LacingStrategy.Disabled;
            RegisterAllPorts();
        }

        [JsonConstructor]
        protected SelectableItemListNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
        }

        [JsonIgnore]
        public ObservableCollection<AstObjectValue> Items { get; } = new ObservableCollection<AstObjectValue>();

        public List<AstReference> Selected { get; set; } = new List<AstReference>();

        internal protected bool SetItems(params AstObjectValue[] items)
        {
            // Remove non-existent values
            var dropItems = Items
                .Where(j => -1 == Array.FindIndex(items, k => StringComparer.Ordinal.Equals(k, j)))
                .ToArray();

            DispatchOnUIThread(() => dropItems.ForEach(j => Items.Remove(j)));

            // Find new by string equality
            var newItems = items
                .Where(k => !Items.Any(j => StringComparer.Ordinal.Equals(j, k)))
                .ToArray();

            DispatchOnUIThread(() => Items.AddRange(newItems));

            if (newItems.Length > 0 || dropItems.Length > 0)
            {
                RaisePropertyChanged(nameof(Items));
                SynchronizeSelected(Selected, true);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal protected void SynchronizeSelected(IEnumerable<AstReference> selection, bool forceUpdate = false)
        {
            Selected = Items.Where(c => selection.Contains(c)).Cast<AstReference>().ToList();
            RaisePropertyChanged(nameof(Selected));
            OnNodeModified(forceUpdate);
        }
    }
}
