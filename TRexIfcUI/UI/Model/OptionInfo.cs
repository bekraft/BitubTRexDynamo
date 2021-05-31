using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

using Autodesk.DesignScript.Runtime;

using Newtonsoft.Json;

namespace TRex.UI.Model
{
    /// <summary>
    /// An information tag to given data option.
    /// </summary>
    /// <typeparam name="TData">The data type</typeparam>
    [IsVisibleInDynamoLibrary(false)]
    public sealed class OptionInfo<TData> : INotifyPropertyChanged where TData : new()
    {
#pragma warning disable CS1591

        private TData data;

        [JsonConstructor]
        public OptionInfo()
        { }

        public OptionInfo(TData data)
        {
            Data = data;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonProperty]
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

        [JsonIgnore]
        public Func<TData, string> InfoDelegate { get; set; } = (data) => data?.ToString();

        public override int GetHashCode()
        {
            return Data?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is OptionInfo<TData> other)
                return other.Data?.Equals(Data) ?? false;
            else
                return false;
        }

        public override string ToString()
        {
            return InfoDelegate?.Invoke(data) ?? base.ToString();
        }

        public static IEnumerable<OptionInfo<TData>> ByOptions(Func<TData, string> tagDelegate, params TData[] options)
        {
            return options.Select(o => new OptionInfo<TData>(o) { InfoDelegate = tagDelegate });
        }

#pragma warning restore CS1591
    }
}
