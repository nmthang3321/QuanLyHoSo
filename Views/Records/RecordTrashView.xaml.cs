using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QuanLyHoSo.Views.Records
{
    public partial class RecordTrashView : UserControl
    {
        public RecordTrashView()
        {
            InitializeComponent();
        }

        private void TrashView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
                Dispatcher.BeginInvoke(new Action(() => { if (IsVisible) TrashSearchBox.Focus(); }), DispatcherPriority.Input);
        }
    }
}
