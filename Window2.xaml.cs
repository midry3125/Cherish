using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cherish
{
    /// <summary>
    /// Window2.xaml の相互作用ロジック
    /// </summary>
    public partial class Window2 : Window
    {
        private Manager manager;
        public Window2(Manager m)
        {
            InitializeComponent();
            Title = "Cherish";
            ResizeMode = ResizeMode.NoResize | ResizeMode.CanMinimize;
            manager = m;
            CategoryName.Focus();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            CategoryName.Text = CategoryName.Text.TrimStart();
            if (CategoryName.Text == "")
            {
                CreateButton.IsEnabled = false;
                ErrorLabel.Content = "";
            }
            else if (manager.Contains(CategoryName.Text))
            {
                CreateButton.IsEnabled = false;
                ErrorLabel.Content = "既に存在しています";
            }
            else
            {
                CreateButton.IsEnabled = true;
                ErrorLabel.Content = "";
            }
        }

        private void OnButtonClicked(object sender, RoutedEventArgs e)
        {
            if (manager.CreateCategory(CategoryName.Text.Trim())) Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter & CreateButton.IsEnabled)
            {
                if (manager.CreateCategory(CategoryName.Text)) Close();
            }
        }
    }
}
