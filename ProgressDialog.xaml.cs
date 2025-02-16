using System.Windows;
using System.Windows.Threading;

namespace Cherish
{
    /// <summary>
    /// Window3.xaml の相互作用ロジック
    /// </summary>
    public partial class ProgressDialog : Window
    {
        public ProgressDialog()
        {
            InitializeComponent();
            Closing += (sender, e) =>
            {
                e.Cancel = MainProgress.Value != MainProgress.Maximum;
            };
        }
        public void Refresh()
        {
            DispatcherFrame frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(obj =>
            {
                ((DispatcherFrame)obj).Continue = false;
                return null;
            });
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }

        private void OnExpanded(object sender, RoutedEventArgs e)
        {
            Height += 35;
            MainGrid.Height += 35;
        }

        private void OnCollapsed(object sender, RoutedEventArgs e)
        {
            Height -= 35;
            MainGrid.Height -= 35;
        }
    }
}
