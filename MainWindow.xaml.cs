using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;


namespace Cherish
{
    public partial class MainWindow : Window
    {
        private const string NEWCATEGORY = "NewCategory";
        private const string DELETECATEGORY = "DeleteCategory";
        private const string DELETEFILE = "Delete";
        private const string RENAME = "Rename";
        private const string OPENBYEXPLORER = "OpenByExplorer";
        private const string OPENWITHSTANDARD = "OpenWithStandard";
        private const string OPEN = "Open";
        public DispatcherTimer timer = new DispatcherTimer();
        public BitmapImage category_icon;
        public BitmapImage audio_icon;
        public BitmapImage movie_icon;
        public BitmapImage image_icon;
        public BitmapImage file_icon;
        public List<Content> availableContents;
        public Manager manager;
        private SolidColorBrush TextHintColor = (SolidColorBrush) new BrushConverter().ConvertFromString("#FF475054");
        private SolidColorBrush NormalSearchTextColor = (SolidColorBrush) new BrushConverter().ConvertFromString("#FF000000");
        private string drive = "";
        private int content_counter = 0;
        private int row = 0;
        private bool isSearchMode;
        private bool ignore;
        private ProgressDialog progress;
        public Content NowFocusing;
        public Window1 subWindow;
        public MainWindow()
        {
            InitializeComponent();
            Title = "Cherish";
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);  //おまじない
            BackButton.IsEnabled = false;
            SearchBox.Foreground = TextHintColor;
            Closing += new System.ComponentModel.CancelEventHandler(OnClose);
            category_icon = Util.GenerateBmp(Properties.Resources.folder);
            audio_icon = Util.GenerateBmp(Properties.Resources.onnpu);
            movie_icon = Util.GenerateBmp(Properties.Resources.movie);
            image_icon = Util.GenerateBmp(Properties.Resources.image);
            file_icon = Util.GenerateBmp(Properties.Resources.file);
            var contextMenu= new ContextMenu();
            var menuItem1 = new MenuItem();
            menuItem1.Header = "カテゴリを作成する";
            menuItem1.Name = NEWCATEGORY;
            menuItem1.Click += MenuItemClicked;
            contextMenu.Items.Add(menuItem1);
            ContextMenu = contextMenu;
            manager = new();
            SetLayout();
            UpdateDrive();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (sender, e) =>
            {
                manager.UpdateInfo();
                UpdateDrive();
                if (manager.isChanged) SetLayout();
            };
            timer.Start();
        }
        private void UpdateDrive()
        {
            Drive.Items.Clear();
            var drivers = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Removable)
                .ToList();
            Drive.IsEnabled = drivers.Any();
            if (!Drive.IsEnabled) return;
            else if (!drivers.Select(d => d.ToString()).Contains(drive))
            {
                drive = "";
                manager.RemoveDrive();
            }
            if (Drive.IsEnabled)
            {
                if (drive != "") Drive.Items.Add("戻る");
                foreach (var d in drivers)
                {
                    Drive.Items.Add(d);
                }
                Drive.Text = drive;
            }
        }
        public void SetLayout()
        {
            availableContents = new();
            row = 0;
            content_counter = 0;
            Dispatcher.BeginInvoke(() =>
            {
                ContentView.Children.Clear();
                try
                {
                    foreach (string f in manager.fcategories)
                    {
                        AddContent(f, category_icon, true);
                    }
                    foreach (string f in manager.fmovieFiles)
                    {
                        AddContent(f, movie_icon, true);
                    }
                    foreach (string f in manager.faudioFiles)
                    {
                        AddContent(f, audio_icon, true);
                    }
                    foreach (string f in manager.fimageFiles)
                    {
                        AddContent(f, image_icon, true);
                    }
                    foreach (string f in manager.fotherFiles)
                    {
                        AddContent(f, file_icon, true);
                    }
                    foreach (string f in manager.categories)
                    {
                        AddContent(f, category_icon);
                    }
                    foreach (string f in manager.movieFiles)
                    {
                        AddContent(f, movie_icon);
                    }
                    foreach (string f in manager.audioFiles)
                    {
                        AddContent(f, audio_icon);
                    }
                    foreach (string f in manager.imageFiles)
                    {
                        AddContent(f, image_icon);
                    }
                    foreach (string f in manager.otherFiles)
                    {
                        AddContent(f, file_icon);
                    }
                }
                catch (System.IO.FileNotFoundException e) {
                    System.Diagnostics.Debug.WriteLine(e);
                }
                catch (System.IO.DirectoryNotFoundException) { }
            });
            BackButton.IsEnabled = drive == "" ?  manager.root != manager.current : manager.drive != manager.dcurrent;
        }
        private void MenuItemClicked(object sender, RoutedEventArgs e)
        {
            var m = (MenuItem)sender;
            switch (m.Name.ToString()) {
                case NEWCATEGORY:
                    var subWindow = new Window2(manager);
                    subWindow.ShowDialog();
                    SetLayout();
                    break;
            }
        }
        private void Register(string[] targets)
        {
            Debug.WriteLine(manager.current);
            var fileNum = 0;
            var paths = new Dictionary<string, string>();
            var dirs = new List<string>();
            void SearchFiles(string dir, string p)
            {
                var dirname = Path.GetFileName(dir);
                foreach (string f in Directory.GetFiles(p))
                {
                    paths.Add(f, Path.Combine(dirname, Path.GetRelativePath(dir, f)));
                    fileNum += 1;
                    progress.Label.Content = $"ファイルパスを取得中...  {fileNum}";
                    progress.Refresh();
                }
                foreach (string d in Directory.GetDirectories(p)){
                    dirs.Add(d);
                    manager.CreateCategory(Path.Combine(dirname, Path.GetRelativePath(dir, d)), false);
                    SearchFiles(dir, d);
                }
            }
            progress = new();
            Dispatcher.BeginInvoke(() =>
            {
                progress.Progress.IsIndeterminate = true;
                progress.Refresh();
                foreach (string t in targets)
                {
                    if (Directory.Exists(t))
                    {
                        var d = Path.GetFileName(t);
                        dirs.Add(t);
                        manager.CreateCategory(d, false);
                        SearchFiles(t, t);
                    }
                    else
                    {
                        paths.Add(t, Path.GetFileName(t));
                    }
                }
                progress.Progress.IsIndeterminate = false;
                progress.Progress.Maximum = paths.Count;
                progress.Progress.Minimum = 0;
                progress.Progress.Value = 0;
                progress.Refresh();
                var skip = false;
                var current = manager.current;
                foreach (KeyValuePair<string, string> item in paths)
                {
                    progress.Label.Content = $"移動中...  {progress.Progress.Value}/{fileNum}";
                    var dirname = Path.GetDirectoryName(item.Value);
                    manager.Cd(dirname);
                    while (true)
                    {
                        try
                        {
                            manager.AddFile(item.Key, false);
                            break;
                        }
                        catch (IOException)
                        {
                            if (skip) break;
                            var dialog = new UsingOtherProcessDialog();
                            dialog.OpenDialog(Path.GetFileName(item.Key));
                            if (dialog.result == UsingOtherProcessDialog.SKIP){
                                skip = dialog.allSkip;
                                break;
                            }
                            else if (dialog.result == UsingOtherProcessDialog.STOP)
                            {
                                progress.Close();
                                SetLayout();
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show($"エラーが発生しました\n({e.Message})", "Cherish  エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    manager.current = current;
                    progress.Progress.Value += 1;
                    progress.Refresh();
                }
                foreach (string d in dirs)
                {
                    try
                    {
                        Directory.Delete(d, true);
                    }
                    catch { }
                }
                progress.Close();
            });
            progress.ShowDialog();
            SetLayout();
        }

        private void AddContent(string fname, BitmapImage img, bool fav=false)
        {
            content_counter++;
            var isCategory = img == category_icon;
            var panel = new Content(this, manager.GetPath(fname), img, isCategory, fav);
            if (img == image_icon) panel.image.Source = new WriteableBitmap(new BitmapImage(new Uri(manager.GetPath(fname))));
            else if (img == movie_icon)
            {
                var p = new MediaElement
                {
                    Source = new Uri(manager.GetPath(fname)),
                    ScrubbingEnabled = true,
                    Volume=0,
                    LoadedBehavior= MediaState.Manual,
                    UnloadedBehavior= MediaState.Manual
                };
                p.MediaOpened += (sender, e) =>
                {
                    var bmp = new RenderTargetBitmap(p.NaturalVideoWidth, p.NaturalVideoHeight, 96, 96, PixelFormats.Pbgra32);
                    bmp.Render(p);
                    panel.image.Source = bmp;
                    p.Stop();
                };
                p.Loaded += (sender, e) =>
                {
                    p.Play();
                    p.Pause();
                };
            }
            void ContentMenuItemOpenning(object sender, RoutedEventArgs e)
            {
                if (NowFocusing != panel)
                {
                    panel.Selected();
                }
            }
            void ContentMenuItemClosing(object sender, RoutedEventArgs e)
            {
                if (NowFocusing != panel)
                {
                    panel.UnSelected();
                }
            }
            if (isCategory)
            {
                panel.Drop += (s, e) =>
                {
                    var paths = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                    if (CheckDropAble(e))
                    {
                        manager.Cd(fname);
                        Register(paths);
                        manager.Cd();
                    }
                    else
                    {
                        foreach (string f in paths)
                        {
                            manager.Cd(fname);
                            try
                            {
                                manager.AddFile(f, false);
                                manager.Cd();
                                manager.Delete(Path.GetFileName(f), false);
                                if (subWindow is not null)
                                {
                                    if (subWindow.filename == Path.GetFileName(f))
                                    {
                                        if (availableContents.Count == 1)
                                        {
                                            subWindow.Init();
                                            subWindow.Close();
                                        }
                                        else
                                        {
                                            subWindow.Next();
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"エラーが発生しました\n({ex.Message})", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }
                        SetLayout();
                    }
                    e.Handled = true;
                };
                panel.DragEnter += (sender, e) =>
                {
                    panel.Selected();
                    e.Effects = DragDropEffects.All;
                    e.Handled = true;
                };
                panel.DragOver += (sender, e) =>
                {
                    e.Effects = DragDropEffects.All;
                    e.Handled = true;
                };
                panel.DragLeave += (sender, e) =>
                {
                    panel.UnSelected();
                };
                void CategoryMenuItemClicked(object sender, RoutedEventArgs e)
                {
                    var m = (MenuItem)sender;
                    panel.Selected();
                    try
                    {
                        switch (m.Name.ToString())
                        {
                            case NEWCATEGORY:
                                var subWindow = new Window2(manager);
                                subWindow.ShowDialog();
                                SetLayout();
                                break;
                            case OPENBYEXPLORER:
                                Process.Start("explorer.exe", manager.GetPath(fname));
                                break;
                            case RENAME:
                                panel.ChangeAbleName();
                                break;
                            case DELETECATEGORY:
                                var res = MessageBox.Show($"カテゴリー名 \"{Util.FormatString(fname, 20, 1, false)}\"を削除します", "Library", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);
                                if (res == MessageBoxResult.Yes)
                                {
                                    manager.Delete(fname);
                                    SetLayout();
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"エラーが発生しました\n({ex.Message})", "Cherish  エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        panel.UnSelected();
                    }
                }
                var contextMenu = new ContextMenu();
                var menuItem2 = new MenuItem();
                menuItem2.Header = "名前を変更";
                menuItem2.Name = RENAME;
                menuItem2.Click += CategoryMenuItemClicked;
                contextMenu.Items.Add(menuItem2);
                var menuItem3 = new MenuItem();
                menuItem3.Header = "削除";
                menuItem3.Name = DELETECATEGORY;
                menuItem3.Click += CategoryMenuItemClicked;
                contextMenu.Items.Add(menuItem3);
                contextMenu.Items.Add(new Separator());
                var menuItem4 = new MenuItem();
                menuItem4.Header = "エクスプローラーで開く";
                menuItem4.Name = OPENBYEXPLORER;
                menuItem4.Click += CategoryMenuItemClicked;
                contextMenu.Items.Add(menuItem4);
                contextMenu.Items.Add(new Separator());
                var menuItem1 = new MenuItem();
                menuItem1.Header = "カテゴリを作成";
                menuItem1.Name = NEWCATEGORY;
                menuItem1.Click += CategoryMenuItemClicked;
                contextMenu.Items.Add(menuItem1);
                panel.ContextMenu = contextMenu;
                panel.ContextMenuOpening += ContentMenuItemOpenning;
                panel.ContextMenuClosing+= ContentMenuItemClosing;
            }
            else
            {
                void FileMenuItemClicked(object sender, RoutedEventArgs e)
                {
                    var m = (MenuItem)sender;
                    panel.Selected();
                    try
                    {
                        switch (m.Name.ToString())
                        {
                            case NEWCATEGORY:
                                var subWindow = new Window2(manager);
                                subWindow.ShowDialog();
                                SetLayout();
                                break;
                            case OPENBYEXPLORER:
                                Process.Start("explorer.exe",　drive.Any() ? manager.dcurrent : manager.current);
                                break;
                            case OPEN:
                                panel.DoFocus();
                                break;
                            case OPENWITHSTANDARD:
                                Process.Start(new ProcessStartInfo(){
                                    FileName = manager.GetPath(fname),
                                    UseShellExecute = true,
                                    CreateNoWindow = true
                                });
                                break;
                            case RENAME:
                                panel.ChangeAbleName();
                                break;
                            case DELETEFILE:
                                var res = MessageBox.Show($"ファイル \"{Util.FormatString(fname, 20, 1, false)}\"を削除します", "Cherish", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);
                                if (res == MessageBoxResult.Yes)
                                {
                                    manager.Delete(fname);
                                    SetLayout();
                                    System.Diagnostics.Debug.WriteLine(string.Join(" ", manager.audioFiles));
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"エラーが発生しました\n({ex.Message})", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        panel.UnSelected();
                    }
                }
                var contextMenu = new ContextMenu();
                var menuItem2 = new MenuItem();
                menuItem2.Header = "名前を変更";
                menuItem2.Name = RENAME;
                menuItem2.Click += FileMenuItemClicked;
                contextMenu.Items.Add(menuItem2);
                var menuItem3 = new MenuItem();
                menuItem3.Header = "削除";
                menuItem3.Name = DELETEFILE;
                menuItem3.Click += FileMenuItemClicked;
                contextMenu.Items.Add(menuItem3);
                contextMenu.Items.Add(new Separator());
                var menuItem6 = new MenuItem();
                menuItem6.Header = "開く";
                menuItem6.Name = OPEN;
                menuItem6.Click += FileMenuItemClicked;
                contextMenu.Items.Add(menuItem6);
                var menuItem5 = new MenuItem();
                menuItem5.Header = "既定のアプリで開く";
                menuItem5.Name = OPENWITHSTANDARD;
                menuItem5.Click += FileMenuItemClicked;
                contextMenu.Items.Add(menuItem5);
                var menuItem4 = new MenuItem();
                menuItem4.Header = "エクスプローラーで開く";
                menuItem4.Name = OPENBYEXPLORER;
                menuItem4.Click += FileMenuItemClicked;
                contextMenu.Items.Add(menuItem4);
                contextMenu.Items.Add(new Separator());
                var menuItem1 = new MenuItem();
                menuItem1.Header = "カテゴリを作成";
                menuItem1.Name = NEWCATEGORY;
                menuItem1.Click += FileMenuItemClicked;
                contextMenu.Items.Add(menuItem1);
                panel.ContextMenu = contextMenu;
                panel.ContextMenuOpening += ContentMenuItemOpenning;
                panel.ContextMenuClosing += ContentMenuItemClosing;
            }
            ContentView.Children.Add(panel);
            int col = (content_counter - 1) % 5;
            if (col == 0)
            {
                row++;
                ContentView.RowDefinitions.Add(new RowDefinition());
            }
            Grid.SetColumn(panel, col);
            Grid.SetRow(panel, row);
            if (img == audio_icon | img == image_icon)
            {
                availableContents.Add(panel);
                panel.index = availableContents.Count - 1;
            }
            Refresh();
        }

        public void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = CheckDropAble(e) ? DragDropEffects.Move : DragDropEffects.None;
        }
        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = CheckDropAble(e) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!CheckDropAble(e))
            {
                e.Handled= true;
                return;
            }
            Register((string[])e.Data.GetData(DataFormats.FileDrop));
        }
        private bool CheckDropAble(DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Source"))
            {
                var source = e.Data.GetData("Source");
                return source.GetType().Assembly != Assembly.GetExecutingAssembly();
            }
            return true;
        }

        private void OnClose(object sender, CancelEventArgs e)
        {
            if (Application.Current.Windows.OfType<Window1>().Any()){
                subWindow.Close();
            }
        }

        private void OnBackButtonClicked(object sender, RoutedEventArgs e)
        {
            manager.Cd();
            SetLayout();
        }

        private void OnDriveSelected(object sender, SelectionChangedEventArgs e)
        {
            if (Drive.Text == drive) return;
            if (drive != "" && Drive.SelectedIndex <= 0)
            {
                drive = "";
                Drive.Text = "";
                manager.RemoveDrive();
                Refresh();
            }
            else if (drive != Drive.Text)
            {
                drive = Drive.Text;
                manager.SetDrive(drive);
            }
            SetLayout();
            manager.isChanged = false;
        }
        private void Refresh()
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

        private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (SearchBox.Text.Any())
                {
                    manager.Search(SearchBox.Text);
                    SetLayout();
                    isSearchMode = true;
                }else if (isSearchMode)
                {
                    manager.Search();
                    SetLayout();
                    isSearchMode = false;
                }
            }
        }
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchBox.Foreground == TextHintColor) return;
            if (string.IsNullOrEmpty(SearchBox.Text) & !SearchBox.IsFocused)
            {
                SearchBox.Text = "名前で検索";
                SearchBox.Foreground = TextHintColor;
            }
        }

        private void OnSearchBoxFocused(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchBox.Text) & SearchBox.Foreground != NormalSearchTextColor)
            {
                SearchBox.Text = "";
                SearchBox.Foreground = NormalSearchTextColor;
            }
        }

        private void OnSearchBoxLostFocused(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                SearchBox.Text = "名前で検索";
                SearchBox.Foreground = TextHintColor;
            }
        }
    }

    public class Content: StackPanel
    {
        private MainWindow window;
        public int index = -1;
        public string path;
        public string filename;
        private bool isCategory;
        private bool isOtherFile;
        private bool isMenuOpen = false;
        private bool isUseAbleName = false;
        private bool isFavorite = false;
        private bool isMousePressing = false;
        private DispatcherTimer dragTimer;
        private Manager manager;
        public TextBox name;
        public Image image;
        private static SolidColorBrush NormalNameTextColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFBAFFB8");
        private static SolidColorBrush NormalBackColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#FF2B2B2B");
        private static SolidColorBrush OnMouseBackColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#474a4d");
        private static SolidColorBrush OnFocusBackColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#223a70");
        private static SolidColorBrush OnSelectedBackColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#5c6471");
        private static SolidColorBrush FavoriteBackColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#ffd6f8");
        private static SolidColorBrush OnFavoriteFocusBackColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#e9dbf5");
        private static SolidColorBrush OnFavoriteSelectedBackColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#fff0f5");
        private static SolidColorBrush OnFavoriteMouseBackColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#ffeef8");
        public Content(MainWindow w, string p, BitmapImage img, bool isC, bool fav=false)
        {
            window = w;
            path = p;
            isCategory = isC;
            isOtherFile = img == window.file_icon;
            isFavorite = fav;
            manager = window.manager;
            filename = System.IO.Path.GetFileName(path);
            Background = isFavorite ? FavoriteBackColor: NormalBackColor;
            Width = 250;
            Height = 120;
            Margin = new Thickness(10);
            ToolTip = filename;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            MouseUp += (sender, e) =>
            {
                if (dragTimer is not null) dragTimer.Stop();
                isMousePressing = false;
            };
            MouseDown += (object sender, MouseButtonEventArgs e) =>
            {
                if (2 <= e.ClickCount)
                {
                    DoFocus();
                }else if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (isFavorite)
                    {
                        manager.RemoveFavorite(filename);
                        Background = NormalBackColor;
                        isFavorite = false;
                    }
                    else
                    {
                        manager.AddFavorite(filename);
                        Background = FavoriteBackColor;
                        isFavorite = true;
                    }
                    window.SetLayout();
                }
                else if (!isCategory & e.LeftButton == MouseButtonState.Pressed)
                {
                    isMousePressing = true;
                    if (e.ClickCount == 1 & window.NowFocusing != this & window.NowFocusing is not null) window.NowFocusing.Leave();
                }
                else if (e.ClickCount == 1 & window.NowFocusing != this & window.NowFocusing is not null)
                {
                    window.NowFocusing.Leave();
                }
            };
            MouseMove += (sender, e) =>
            {
                if (isMousePressing)
                {
                    var data = new System.Windows.DataObject(System.Windows.DataFormats.FileDrop, new[] { manager.GetPath(filename) });
                    data.SetData("Source", this);
                    DragDrop.DoDragDrop(this, data, System.Windows.DragDropEffects.All);
                }
            };
            DragEnter += w.OnDragEnter;
            image = new();
            image.Height = 80;
            image.Width = 100;
            image.Source = img;
            image.Margin = new Thickness(0, 0, 70, 0);
            Children.Add(image);
            name = new();
            name.Text = Util.FormatString(filename, 26, 2);
            name.Height = 37;
            name.Width = 170;
            name.FontSize = 14;
            name.Margin = new Thickness(0, 3, 79, 0);
            name.HorizontalContentAlignment = HorizontalAlignment.Left;
            name.VerticalContentAlignment = VerticalAlignment.Center;
            name.Foreground = NormalNameTextColor;
            name.Background = NormalBackColor;
            name.BorderBrush = NormalBackColor;
            name.IsReadOnly = true;
            name.IsEnabled = false;
            name.TextChanged += OnNameInput;
            name.KeyDown += (sender, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    if (name.Text == filename)
                    {
                        FinishChamgeName();
                        e.Handled = true;
                    }
                    else if (isUseAbleName)
                    {
                        manager.Rename(filename, name.Text);
                        FinishChamgeName();
                        window.SetLayout();
                        e.Handled = true;
                    }

                }
            };
            name.LostFocus += (sender, e) =>
            {
                name.Text = filename;
                FinishChamgeName();

            };
            Children.Add(name);
        }
        private void OnNameInput(object sender, EventArgs e)
        {
            isUseAbleName = name.Text != "" & !manager.Contains(name.Text);
        }
        public void ChangeAbleName()
        {
            name.Text = filename; 
            name.IsReadOnly = false;
            name.IsEnabled = true;
            name.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#fafafa");
            name.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#000000");
            name.Select(0, System.IO.Path.GetFileNameWithoutExtension(filename).Length);
            name.Focus();
        }
        public void FinishChamgeName()
        {
            filename = name.Text.Trim();
            path = manager.GetPath(filename);
            name.Text = Util.FormatString(filename, 24, 2);
            name.Select(0, 0);
            name.IsReadOnly = true;
            name.IsEnabled = false;
            name.Foreground = NormalNameTextColor;
            name.Background = NormalBackColor;
        }
        private void Refresh()
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
        public void DoFocus()
        {
            if (window.NowFocusing is not null)
            {
                window.NowFocusing.Leave();
            }
            Background = isFavorite ? OnFavoriteFocusBackColor :  OnFocusBackColor;
            Refresh();
            if (isCategory)
            {
                manager.Cd(filename);
                window.SetLayout();
            }
            else
            {
                window.NowFocusing = this;
                if (isOtherFile)
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = path,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });
                }
                else
                {
                    if (!Application.Current.Windows.OfType<Window1>().Any())
                    {
                        window.subWindow = new(window, index);
                        window.subWindow.Show();
                    }
                    window.subWindow.OpenFile(path, index);
                }
            }
        }
        public void Leave()
        {
            Background = isFavorite ? FavoriteBackColor : NormalBackColor;
            window.NowFocusing = null;
        }
        public void Selected()
        {
            Background = isFavorite ? OnFavoriteSelectedBackColor : OnSelectedBackColor;
            isMenuOpen= true;
        }
        public void UnSelected()
        {
            Background = isFavorite ? FavoriteBackColor : NormalBackColor;
            isMenuOpen= false;
        }

        private void OnMouseEnter(object sender, EventArgs e)
        {
            if (window.NowFocusing != this) Background = isFavorite ? OnFavoriteMouseBackColor : OnMouseBackColor;
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            if (window.NowFocusing != this & !isMenuOpen)
            {
                Background = isFavorite ? FavoriteBackColor : NormalBackColor;
            }
        }
    }
}
