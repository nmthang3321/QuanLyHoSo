using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuanLyHoSo.Presentation
{
    public static class DataGridCopyBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(DataGridCopyBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid dataGrid)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                dataGrid.PreviewKeyDown += DataGrid_PreviewKeyDown;
            }
            else
            {
                dataGrid.PreviewKeyDown -= DataGrid_PreviewKeyDown;
            }
        }

        private static void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not DataGrid dataGrid ||
                e.Key != Key.C ||
                (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            {
                return;
            }

            var text = BuildClipboardText(dataGrid);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                Clipboard.SetText(text);
                e.Handled = true;
            }
            catch (Exception)
            {
                // Clipboard can be temporarily locked by another process; keep the UI responsive.
            }
        }

        private static string BuildClipboardText(DataGrid dataGrid)
        {
            var selectedCells = dataGrid.SelectedCells
                .Where(cell => cell.Item != null && cell.Column != null)
                .ToList();

            if (selectedCells.Count > 0)
            {
                return BuildSelectedCellsText(dataGrid, selectedCells);
            }

            var selectedItems = dataGrid.SelectedItems
                .Cast<object>()
                .Where(item => item != null)
                .ToList();

            return selectedItems.Count == 0
                ? string.Empty
                : BuildRowsText(dataGrid, selectedItems);
        }

        private static string BuildSelectedCellsText(DataGrid dataGrid, IReadOnlyList<DataGridCellInfo> selectedCells)
        {
            var rows = selectedCells
                .GroupBy(cell => cell.Item)
                .OrderBy(group => dataGrid.Items.IndexOf(group.Key))
                .ToList();
            var columns = selectedCells
                .Select(cell => cell.Column)
                .Distinct()
                .Where(column => column.Visibility == Visibility.Visible)
                .OrderBy(column => column.DisplayIndex)
                .ToList();

            var builder = new StringBuilder();

            foreach (var row in rows)
            {
                var selectedColumnSet = new HashSet<DataGridColumn>(row.Select(cell => cell.Column));
                builder.AppendLine(string.Join("\t", columns.Select(column =>
                    selectedColumnSet.Contains(column) ? GetCellText(column, row.Key) : string.Empty)));
            }

            return builder.ToString().TrimEnd('\r', '\n');
        }

        private static string BuildRowsText(DataGrid dataGrid, IReadOnlyList<object> rows)
        {
            var columns = dataGrid.Columns
                .Where(column => column.Visibility == Visibility.Visible)
                .OrderBy(column => column.DisplayIndex)
                .ToList();
            var builder = new StringBuilder();

            foreach (var row in rows.OrderBy(item => dataGrid.Items.IndexOf(item)))
            {
                builder.AppendLine(string.Join("\t", columns.Select(column => GetCellText(column, row))));
            }

            return builder.ToString().TrimEnd('\r', '\n');
        }

        private static string GetCellText(DataGridColumn column, object item)
        {
            return column.OnCopyingCellClipboardContent(item)?.ToString() ?? string.Empty;
        }
    }
}
