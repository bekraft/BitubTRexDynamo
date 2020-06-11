using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.DesignScript.Geometry;

using Dynamo.Graph.Nodes;

using Newtonsoft.Json;

using Internal;

using AstObjectValue = Internal.AstValue<object>;

namespace Task
{
#pragma warning disable CS1591

    public abstract class SelectableItemListNodeModel : BaseNodeModel
    {
        #region Internals
        private List<AstObjectValue> _items = new List<AstObjectValue>();
        private List<AstReference> _selected = new List<AstReference>();
        private List<string> _persistentSelected = new List<string>();
        #endregion

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
        public List<AstObjectValue> Items
        {
            get {
                return _items;
            }
            private set {
                _items = value;
                RaisePropertyChanged(nameof(Items));
            }
        }

        [JsonIgnore]
        public List<AstReference> Selected
        {
            get => _selected;            
        }

        public List<string> SelectedValue 
        { 
            get {
                return _persistentSelected;
            }
            set {
                _persistentSelected = value;
                RaisePropertyChanged(nameof(SelectedValue));
            }
        } 

        internal protected bool SetItems(params AstObjectValue[] items)
        {
            var identical = (items.Length == _items.Count) && items.All(r => _items.Contains(r));

            if (!identical)
            {
                DispatchOnUIThread(() => Items = new List<AstObjectValue>(items));
                return true;
            }
            else
            {
                return false;
            }
        }

        internal protected bool SetSelected(AstReference[] selected, bool forceModified)
        {
            var identical = (_selected.Count == selected.Length) && (selected.All(r => _selected.Contains(r)));
            if (!identical)
            {
                _selected = new List<AstReference>(selected);
                _persistentSelected = _selected.Select(v => v.ToString()).ToList();
                if (forceModified)
                {
                    RaisePropertyChanged(nameof(SelectedValue));
                    OnNodeModified(true);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        internal protected AstReference[] SelectByValues(string[] selectedValue)
        {
            List<AstReference> selected = new List<AstReference>();
            foreach(var v in GlobalArgumentService.FilterBySerializationValue(Items.ToArray(), selectedValue, false))
            {
                selected.Add(v as AstReference);
            }
            return selected.ToArray();
        }
    }
}
