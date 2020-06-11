using System;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using Dynamo.Controls;
using Dynamo.Utilities;

using Internal;
using Log;

namespace UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class LogMessageTableNodeCustomization : BaseNodeViewCustomization<LogMessageTableNodeModel>
    {
        class EnumToTextConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value.ToString();
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (targetType != typeof(Enum))
                    throw new NotSupportedException($"Not support {targetType}");
                return Enum.Parse(typeof(Enum), value as string);
            }
        }

        #region Internals

        private NodeProgressing[] _eventSources = new NodeProgressing[] { };
        private SortableTableControl _control;

        #endregion

        public override void CustomizeView(LogMessageTableNodeModel model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);

            var tableControl = new SortableTableControl();
            nodeView.inputGrid.Children.Add(tableControl);

            tableControl.DataContext = model;
            _control = tableControl;

            _control.DataGrid.AutoGeneratingColumn += DataGrid_AutoGeneratingColumn;
        }

        // Override autogeneration for enums
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType.IsEnum)
            {
                var textColumn = new DataGridTextColumn();
                textColumn.Header = e.Column.Header;
                textColumn.Binding = new Binding(e.PropertyName) { Converter = new EnumToTextConverter() };
                e.Column = textColumn;
            }
        }

        protected override void OnCachedValueChange(object sender)
        {
            // Get recently updated event sources
            var eventSources = NodeModel.GetCachedInput<NodeProgressing>(0, ModelEngineController);
            // Assumes that sources are unique with the current array
            if (eventSources.Length == _eventSources.Length
                // No change action if same, or both empty
                && (_eventSources.All(i => eventSources.Any(j => i == j)))) return;

            foreach (var eventSource in _eventSources)
                eventSource.ActionLog.CollectionChanged -= ActionLog_CollectionChanged;

            DispatchUI(() =>
            {
                NodeModel.DataTable.Clear();
                foreach (var eventSource in eventSources)
                    NodeModel.DataTable.AddRange(eventSource.ActionLog);
            });
            
            foreach (var eventSource in eventSources)
                eventSource.ActionLog.CollectionChanged += ActionLog_CollectionChanged;

            _eventSources = eventSources;
        }

        private void ActionLog_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            DispatchUI( () => NodeModel.DataTable.AddRange(e.NewItems.Cast<LogMessage>()) );
        }

        public override void Dispose()
        {
            foreach (var eventSource in _eventSources)
                eventSource.ActionLog.CollectionChanged -= ActionLog_CollectionChanged;

            _eventSources = null;
            _control = null;
        }
    }

#pragma warning restore CS1591
}
