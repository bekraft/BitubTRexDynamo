using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dynamo.Graph.Nodes;

using Newtonsoft.Json;

// Disable comment warning
#pragma warning disable CS1591

namespace Task
{
    public abstract class CancelableProgressingOptionNodeModel : CancelableProgressingNodeModel
    {
        #region Internals

        protected object _selectedOption;

        #endregion

        protected CancelableProgressingOptionNodeModel() : base()
        {
            ResetState();
        }

        protected CancelableProgressingOptionNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            ResetState();
        }

        /// <summary>
        /// Reflects the available options.
        /// </summary>
        [JsonIgnore]
        public ICollection<object> AvailableOptions { get; } = new ObservableCollection<object>();

        /// <summary>
        /// Reflects & changes the current option.
        /// </summary>
        public object SelectedOption
        {
            get {
                return _selectedOption;
            }
            set {
                _selectedOption = value;
                RaisePropertyChanged(nameof(SelectedOption));
                OnNodeModified(true);
            }
        }
    }
}

#pragma warning restore CS1591