using System.Linq;

using System.Collections.Generic;
using System.Collections.ObjectModel;

using Dynamo.Graph.Nodes;

using Newtonsoft.Json;

namespace TRex.Task
{
    /// <summary>
    /// Cancelable progress option node model template-
    /// </summary>
    public abstract class CancelableProgressingOptionNodeModel<TOption> : CancelableProgressingNodeModel
    {

#pragma warning disable CS1591

        #region Internals

        protected TOption option;        

        protected CancelableProgressingOptionNodeModel() : base()
        {
            ResetState();
            Options = new ObservableCollection<TOption>(GetInitialOptions());
        }

        protected CancelableProgressingOptionNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            ResetState();
            Options = new ObservableCollection<TOption>(GetInitialOptions());
        }

        protected abstract IEnumerable<TOption> GetInitialOptions();

        protected bool IsNotNullSelected()
        {
            if (null == Selected)
            {
                Warning($"{typeof(TOption).Name} must not be null.");
                return false;
            }
            return true;
        }

        #endregion

#pragma warning restore CS1591

        /// <summary>
        /// Reflects the available options.
        /// </summary>
        [JsonIgnore]
        public ICollection<TOption> Options { get; private set; }

        /// <summary>
        /// Reflects and changes the current option.
        /// </summary>
        [JsonProperty]
        public TOption Selected
        {
            get {
                return option;
            }
            set {
                var found = Options.FirstOrDefault(o => o?.Equals(value) ?? false);
                option = found ?? value;
                RaisePropertyChanged(nameof(Selected));
                OnNodeModified(true);
            }
        }
    }
}