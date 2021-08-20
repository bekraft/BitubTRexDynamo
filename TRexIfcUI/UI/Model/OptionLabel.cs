using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

using Autodesk.DesignScript.Runtime;

using Newtonsoft.Json;

namespace TRex.UI.Model
{
    /// <summary>
    /// An tagging label to a given option.
    /// </summary>
    /// <typeparam name="TData">The data type</typeparam>
    [IsVisibleInDynamoLibrary(false)]
    public class OptionLabel<TData> : INotifyPropertyChanged
    {
#pragma warning disable CS1591

        #region Internals

        private TData data;
        private string id;
        private string label;

        [JsonConstructor]
        public OptionLabel()
        { }

        #endregion

        public OptionLabel(string id, string label, TData data)
        {
            if (null == id)
                throw new ArgumentNullException(nameof(id));

            this.id = id;
            this.label = label;
            this.data = data;
        }

        public OptionLabel(string labelId, TData data) : this(labelId, labelId, data)
        { }

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonIgnore]
        public string Label
        {
            get
            {
                return label;
            }
            set
            {
                label = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
            }
        }

        [JsonProperty]
        public string ID
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ID)));
            }
        }

        [JsonIgnore]
        public TData Data 
        { 
            get {
                return data;
            }
            set {
                data = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Data)));
            }
        }

        public override string ToString()
        {
            return label ?? id;
        }

        public static IEnumerable<OptionLabel<TData>> ByOptions(Func<TData, string> labelIdDelegate, params TData[] options)
        {
            return options.Select(o => new OptionLabel<TData>(labelIdDelegate(o), labelIdDelegate(o), o));
        }

        public override bool Equals(object obj)
        {
            return obj is OptionLabel<TData> option && id == option.id;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

#pragma warning restore CS1591
    }
}
