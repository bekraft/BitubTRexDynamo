using System.Linq;
using System.Collections.Generic;

using Dynamo.Graph.Nodes;

using Newtonsoft.Json;

using TRex.Internal;

using AstObjectValue = TRex.Internal.AstValue<object>;

namespace TRex.Task;

#pragma warning disable CS1591

public abstract class SelectableItemListNodeModel : BaseNodeModel
{
    #region Internals
    private List<AstObjectValue> _items = new ();
    private List<AstReference> _selected = new ();
    private List<string> _persistentSelected = new ();
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
        get => _items;
        protected internal set 
        {
            _items = value;
            RaisePropertyChanged(nameof(Items));
        }
    }

    [JsonIgnore]
    public List<AstReference> Selected => _selected;

    public List<string> SelectedValue 
    { 
        get => _persistentSelected;
        set 
        {
            _persistentSelected = value;
            RaisePropertyChanged(nameof(SelectedValue));
        }
    } 

    protected internal bool SetItems(params AstObjectValue[] items)
    {
        if (!AstReference.IsEqualTo(items, _items))
        {
            Items = new List<AstObjectValue>(items);
            return true;
        }
        else
        {
            return false;
        }
    }

    protected internal bool SetSelected(AstReference[] selected, bool forceModified)
    {
        if (!AstReference.IsEqualTo(selected, _selected))
        {
            _selected = new List<AstReference>(selected);
            SilentSetPersistentValue();
            RaisePropertyChanged(nameof(SelectedValue));

            OnNodeModified(forceModified);
            
            return true;
        }
        else
        {
            return false;
        }
    }

    protected void SilentSetPersistentValue()
    {
        _persistentSelected = _selected.Select(v => v.ToString()!).ToList();
    }
}

#pragma warning restore CS1591
