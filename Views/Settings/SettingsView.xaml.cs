using System;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using QuanLyHoSo.Models;
using QuanLyHoSo.ViewModels;

namespace QuanLyHoSo.Views.Settings
{
    public partial class SettingsView : UserControl
    {
        private Point _dragStartPoint;

        public SettingsView()
        {
            InitializeComponent();
        }

        private void CatalogValuesListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void CatalogValuesListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var currentPosition = e.GetPosition(null);
            if (Math.Abs(currentPosition.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPosition.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject) is not ListBoxItem listBoxItem ||
                listBoxItem.DataContext is not CatalogValueSetting catalogValue)
            {
                return;
            }

            DragDrop.DoDragDrop(listBoxItem, catalogValue, DragDropEffects.Move);
        }

        private void CatalogValuesListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(CatalogValueSetting))
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void CatalogValuesListBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(CatalogValueSetting)) ||
                e.Data.GetData(typeof(CatalogValueSetting)) is not CatalogValueSetting source ||
                FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject) is not ListBoxItem targetContainer ||
                targetContainer.DataContext is not CatalogValueSetting target ||
                DataContext is not SettingsViewModel viewModel)
            {
                return;
            }

            viewModel.MoveCatalogValue(source, target);
            e.Handled = true;
        }

        private static T FindAncestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
