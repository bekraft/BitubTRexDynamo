using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

using Dynamo.Controls;

using TRex.Data;

namespace TRex.UI.Customization
{
    // Disable comment warning
#pragma warning disable CS1591

    public class DataTablePreviewNodeCustomization : BaseNodeViewCustomization<DataTablePreviewNodeModel>
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

        private SortableTableControl _control;

        #endregion

        public override void CustomizeView(DataTablePreviewNodeModel model, NodeView nodeView)
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
            ObservableCollection<object> postedData = null;
            AsyncSchedule(
                () => { postedData = new ObservableCollection<object>(NodeModel.GetCachedInput<object>(0, ModelEngineController)); },
                () => { NodeModel.DataTable = postedData; }
            );
        }

        public override void Dispose()
        {
            _control = null;
        }
    }

#pragma warning restore CS1591
}
