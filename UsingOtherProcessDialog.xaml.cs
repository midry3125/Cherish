using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Cherish
{
    /// <summary>
    /// Window3.xaml の相互作用ロジック
    /// </summary>
    public partial class UsingOtherProcessDialog : Window
    {
        public const int RETRY = 1;
        public const int SKIP = 2;
        public const int STOP = 3;
        public int result;
        public bool allSkip = false;
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        const int GWL_STYLE = -16;
        const int WS_SYSMENU = 0x80000;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr handle = new WindowInteropHelper(this).Handle;
            int style = GetWindowLong(handle, GWL_STYLE);
            style = style & (~WS_SYSMENU);
            SetWindowLong(handle, GWL_STYLE, style);
        }
        public UsingOtherProcessDialog()
        {
            InitializeComponent();
        }
        public int OpenDialog(string path)
        {
            Label.Content = Util.FormatString($"\"{Util.FormatString(path, 20, 1, false)}\"が他のプロセスによって開かれているため処理ができません", 50, 3, false);
            Debug.WriteLine(Label.Content);
            ShowDialog();
            return result;
        }

        private void Retry(object sender, RoutedEventArgs e)
        {
            result = RETRY;
            Close();
        }

        private void Skip(object sender, RoutedEventArgs e)
        {
            result = SKIP;
            allSkip = CheckBox.IsChecked ?? false;
            Close();
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            result = STOP;
            Close();
        }
    }
}
