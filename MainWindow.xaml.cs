using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private const int DEFAULT_WIDTH = 980;
        private const int DEFAULT_HEIGHT = 560;
        private const string TRASH = @"C:\$Recycle.Bin";
        private const string NEWCATEGORY = "NewCategory";
        private const string DELETECATEGORY = "DeleteCategory";
        private const string DELETEFILE = "Delete";
        private const string RENAME = "Rename";
        private const string OPENBYEXPLORER = "OpenByExplorer";
        private const string OPENWITHSTANDARD = "OpenWithStandard";
        private const string OPEN = "Open";
        private const string COPYPATH = "CopyPath";
        private const string PREVIEWCONFIG = "PreviewConfig";
        private const string CONTINUOUSPLAY = "ContinuousPlayConfig";
        public DispatcherTimer timer = new DispatcherTimer();
        public DispatcherTimer filemoveTimer;
        public BitmapImage category_icon;
        public BitmapImage audio_icon;
        public BitmapImage movie_icon;
        public BitmapImage image_icon;
        public BitmapImage file_icon;
        public List<Content> availableContents;
        public List<Content> audioContents;
        public Manager manager;
        private SolidColorBrush TextHintColor = (SolidColorBrush) new BrushConverter().ConvertFromString("#FF475054");
        private SolidColorBrush NormalSearchTextColor = (SolidColorBrush) new BrushConverter().ConvertFromString("#FF000000");
        private string drive = "";
        private int content_counter = 0;
        private int row = 0;
        private bool isSearchMode;
        private bool ignore;
        private bool nowRendering;
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
            var contextMenu = new ContextMenu();
            var menuItem2 = new MenuItem();
            menuItem2.Header = "エクスプローラーで開く";
            menuItem2.Name = OPENBYEXPLORER;
            menuItem2.Click += MenuItemClicked;
            contextMenu.Items.Add(menuItem2);
            contextMenu.Items.Add(new Separator());
            var menuItem1 = new MenuItem();
            menuItem1.Header = "ディレクトリを作成する";
            menuItem1.Name = NEWCATEGORY;
            menuItem1.Click += MenuItemClicked;
            contextMenu.Items.Add(menuItem1);
            ContextMenu = contextMenu;
            manager = new();
            PreviewConfig.IsChecked = manager.config.preview;
            SetLayout();
            UpdateDrive();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (sender, e) =>
            {
                try
                {
                    manager.UpdateInfo();
                    UpdateDrive();
                }catch (FileNotFoundException)
                {
                    manager.Init();
                    drive = "";
                }catch (DirectoryNotFoundException)
                {
                    manager.Init();
                    drive = "";
                }
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
            audioContents = new();
            row = 0;
            content_counter = 0;
            var current = manager.current;
            Task.Run(() =>
            {
                if (nowRendering) return;
                nowRendering = true;
                Dispatcher.Invoke(() =>
                {
                    ContentView.Children.Clear();
                    Refresh();
                });
                try
                {
                    lock (manager)
                    {
                        foreach (string f in manager.fcategories)
                        {
                            if (current != manager.current) return;
                            AddContent(f, category_icon, true);
                        }
                        foreach (string f in manager.fmovieFiles)
                        {
                            if (current != manager.current) return;
                            AddContent(f, movie_icon, true);
                        }
                        foreach (string f in manager.faudioFiles)
                        {
                            if (current != manager.current) return;
                            AddContent(f, audio_icon, true);
                        }
                        foreach (string f in manager.fimageFiles)
                        {
                            if (current != manager.current) return;
                            AddContent(f, image_icon, true);
                        }
                        foreach (string f in manager.fotherFiles)
                        {
                            if (current != manager.current) return;
                            AddContent(f, file_icon, true);
                        }
                        foreach (string f in manager.categories)
                        {
                            if (current != manager.current) return;
                            AddContent(f, category_icon);
                        }
                        foreach (string f in manager.movieFiles)
                        {
                            if (current != manager.current) return;
                            AddContent(f, movie_icon);
                        }
                        foreach (string f in manager.audioFiles)
                        {
                            if (current != manager.current) return;
                            AddContent(f, audio_icon);
                        }
                        foreach (string f in manager.imageFiles)
                        {
                            if (current != manager.current) return;
                            AddContent(f, image_icon);
                        }
                        foreach (string f in manager.otherFiles)
                        {
                            if (current != manager.current) return;
                            AddContent(f, file_icon);
                        }
                        Refresh();
                    }
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                finally
                {
                    nowRendering = false;
                }
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
                case OPENBYEXPLORER:
                    Process.Start("explorer.exe", drive.Any() ? manager.dcurrent : manager.current);
                    break;
                case PREVIEWCONFIG:
                    manager.config.ChangePreviewState();
                    SetLayout();
                    break;
                case CONTINUOUSPLAY:
                    manager.config.ChangeContinuousState();
                    break;
            }
        }
        private void Delete(string target)
        {
            var fileNum = 0;
            var paths = new List<string>();
            var dirs = new List<string>();
            void SearchFiles(string dir, string p)
            {
                var dirname = Path.GetFileName(dir);
                foreach (string f in Directory.GetFiles(p))
                {
                    paths.Add(Path.Combine(p, f));
                    fileNum += 1;
                    Dispatcher.BeginInvoke(() =>
                    {
                        progress.Label.Content = $"ファイルパスを取得中...  {fileNum}";
                    });
                    progress.Refresh();
                }
                foreach (string d in Directory.GetDirectories(p))
                {
                    dirs.Add(d);
                    SearchFiles(dir, d);
                }
            }
            var data = new FileMoveData();
            progress = new()
            {
                ResizeMode = ResizeMode.NoResize | ResizeMode.CanMinimize
            };
            progress.VerboseExpander.Visibility = Visibility.Hidden;
            Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    progress.MainProgress.IsIndeterminate = true;
                    progress.Refresh();
                });
                if (Directory.Exists(target))
                {
                    var d = Path.GetFileName(target);
                    dirs.Add(target);
                    SearchFiles(target, target);
                }
                else
                {
                    paths.Add(target);
                }
                Dispatcher.Invoke(() =>
                {
                    progress.MainProgress.IsIndeterminate = false;
                    progress.MainProgress.Maximum = paths.Count;
                    progress.MainProgress.Minimum = 0;
                    progress.MainProgress.Value = 0;
                    progress.Refresh();
                });
                var skip = false;
                foreach (var p in paths)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        progress.Label.Content = $"削除中 {Util.FormatString(Path.GetFileName(p), 25)}  {progress.MainProgress.Value}/{fileNum}";
                    });
                    while (true)
                    {
                        try
                        {
                            File.Delete(p);
                            break;
                        }
                        catch (IOException)
                        {
                            if (skip) break;
                            int r = 0;
                            bool a = false;
                            Dispatcher.Invoke(() =>
                            {
                                var dialog = new UsingOtherProcessDialog();
                                dialog.OpenDialog(Path.GetFileName(p));
                                r = dialog.result;
                                a = dialog.allSkip;
                            });
                            if (r == UsingOtherProcessDialog.SKIP)
                            {
                                skip = a;
                                break;
                            }
                            else if (r == UsingOtherProcessDialog.STOP)
                            {
                                Dispatcher.BeginInvoke(() =>
                                {
                                    progress.Close();
                                });
                                return;
                            }
                        }
                        finally
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                progress.MainProgress.Value++;
                            });
                        }
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    progress.MainProgress.Value += 1;
                    progress.Refresh();
                });
                foreach (string d in dirs)
                {
                    try
                    {
                        Directory.Delete(d, true);
                    }
                    catch { }
                }
                Dispatcher.Invoke(() =>
                {
                    progress.Close();
                });
            });
            progress.ShowDialog();
        }
        private void Delete(string[] targets)
        {
            var fileNum = 0;
            var paths = new List<string>();
            var dirs = new List<string>();
            void SearchFiles(string dir, string p)
            {
                var dirname = Path.GetFileName(dir);
                foreach (string f in Directory.GetFiles(p))
                {
                    paths.Add(Path.Combine(p, f));
                    fileNum += 1;
                    Dispatcher.BeginInvoke(() =>
                    {
                        progress.Label.Content = $"ファイルパスを取得中...  {fileNum}";
                    });
                    progress.Refresh();
                }
                foreach (string d in Directory.GetDirectories(p))
                {
                    dirs.Add(d);
                    SearchFiles(dir, d);
                }
            }
            var data = new FileMoveData();
            progress = new()
            {
                ResizeMode = ResizeMode.NoResize | ResizeMode.CanMinimize
            };
            progress.VerboseExpander.Visibility = Visibility.Hidden;
            Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    progress.MainProgress.IsIndeterminate = true;
                    progress.Refresh();
                });
                foreach (string t in targets)
                {
                    if (Directory.Exists(t))
                    {
                        var d = Path.GetFileName(t);
                        dirs.Add(t);
                        SearchFiles(t, t);
                    }
                    else
                    {
                        paths.Add(t);
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    progress.MainProgress.IsIndeterminate = false;
                    progress.MainProgress.Maximum = paths.Count;
                    progress.MainProgress.Minimum = 0;
                    progress.MainProgress.Value = 0;
                    progress.Refresh();
                });
                var skip = false;
                foreach (var p in paths)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        progress.Label.Content = $"削除中 {Util.FormatString(Path.GetFileName(p), 25)}  {progress.MainProgress.Value}/{fileNum}";
                    });
                    while (true)
                    {
                        try
                        {
                            File.Delete(p);
                            break;
                        }
                        catch (IOException)
                        {
                            if (skip) break;
                            int r = 0;
                            bool a = false;
                            Dispatcher.Invoke(() =>
                            {
                                var dialog = new UsingOtherProcessDialog();
                                dialog.OpenDialog(Path.GetFileName(p));
                                r = dialog.result;
                                a = dialog.allSkip;
                            });
                            if (r == UsingOtherProcessDialog.SKIP)
                            {
                                skip = a;
                                break;
                            }
                            else if (r == UsingOtherProcessDialog.STOP)
                            {
                                Dispatcher.BeginInvoke(() =>
                                {
                                    progress.Close();
                                });
                                return;
                            }
                        }
                        finally
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                progress.MainProgress.Value++;
                            });
                        }
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    progress.MainProgress.Value += 1;
                    progress.Refresh();
                });
                foreach (string d in dirs)
                {
                    try
                    {
                        Directory.Delete(d, true);
                    }
                    catch { }
                }
                Dispatcher.Invoke(() =>
                {
                    progress.Close();
                });
            });
            progress.ShowDialog();
        }
        private void Register(string[] targets,bool copy = true)
        {
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
                    Dispatcher.BeginInvoke(() =>
                    {
                        progress.Label.Content = $"ファイルパスを取得中...  {fileNum}";
                    });
                }
                foreach (string d in Directory.GetDirectories(p)){
                    dirs.Add(d);
                    manager.CreateCategory(Path.Combine(dirname, Path.GetRelativePath(dir, d)), false);
                    SearchFiles(dir, d);
                }
            }
            var data = new FileMoveData();
            Dispatcher.Invoke(() =>
            {
                progress = new()
                {
                    ResizeMode = ResizeMode.NoResize | ResizeMode.CanMinimize
                };
            });
            filemoveTimer = new()
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };
            if (copy)
            {
                filemoveTimer.Tick += (sender, e) =>
                {
                    var p = (int)(data.FinishedSize / data.Size * 100);
                    progress.VerbosePercentProgress.Content = $"{p}%";
                    progress.VerboseProgressLabel.Content = $"{Util.GetFormatFileSize(data.FinishedSize)} / {data.FormatSize}";
                    progress.VerboseProgress.Value = p;
                    progress.Refresh();
                };
            }
            else
            {
                filemoveTimer.Tick += (sender, e) => { };
            }
            Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    progress.MainProgress.IsIndeterminate = true;
                    progress.Refresh();
                });
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
                Dispatcher.Invoke(() =>
                {
                    progress.MainProgress.IsIndeterminate = false;
                    progress.MainProgress.Maximum = paths.Count;
                    progress.MainProgress.Minimum = 0;
                    progress.MainProgress.Value = 0;
                    progress.VerboseExpander.Visibility = copy ? Visibility.Visible : Visibility.Collapsed;
                    progress.Refresh();
                });
                var skip = false;
                var current = manager.current;
                var header = copy ? "コピー中" : "移動中";
                filemoveTimer.Start();
                foreach (var item in paths)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        progress.Label.Content = $"{header} {Util.FormatString(Path.GetFileName(item.Key), 25)}  {progress.MainProgress.Value}/{fileNum}";
                        progress.VerbosePercentProgress.Content = "0%";
                        progress.VerboseProgress.Value = 0;
                    });
                    var dirname = Path.GetDirectoryName(item.Value);
                    manager.Cd(dirname);
                    while (true)
                    {
                        try
                        {
                            if (copy)
                            {
                                manager.AddFileWithProgress(item.Key, data, false);
                            }
                            else
                            {
                                manager.AddFile(item.Key, false);
                            }
                            break;
                        }
                        catch (IOException)
                        {
                            if (skip) break;
                            filemoveTimer.Stop();
                            int r = 0;
                            bool a = false;
                            Dispatcher.Invoke(() =>
                            {
                                var dialog = new UsingOtherProcessDialog();
                                dialog.OpenDialog(Path.GetFileName(item.Key));
                                r = dialog.result;
                                a = dialog.allSkip;
                            });
                            if (r == UsingOtherProcessDialog.SKIP)
                            {
                                skip = a;
                                break;
                            }
                            else if (r == UsingOtherProcessDialog.STOP)
                            {
                                Dispatcher.BeginInvoke(() =>
                                {
                                    progress.Close();
                                });
                                return;
                            }
                            else
                            {
                                filemoveTimer.Start();
                            }
                        }
                        finally
                        {
                            manager.current = current;
                            Dispatcher.BeginInvoke(() =>
                            {
                                progress.MainProgress.Value++;
                            });
                        }
                    }
                }
                filemoveTimer.Stop();
                Dispatcher.Invoke(() =>
                {
                    progress.Refresh();
                });
                if (!copy)
                {
                    foreach (string d in dirs)
                    {
                        try
                        {
                            Directory.Delete(d, true);
                        }
                        catch { }
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    progress.Close();
                });
            });
            Dispatcher.Invoke(() =>
            {
                progress.ShowDialog();
            });
        }

        private void AddContent(string fname, BitmapImage img, bool fav=false)
        {
            content_counter++;
            var path = manager.GetPath(fname);
            var isCategory = img == category_icon;
            Content panel = null;
            Dispatcher.Invoke(() =>
            {
                panel = new Content(this, path, img, isCategory, fav);
            });
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
                Dispatcher.Invoke(() =>
                {
                    panel.Drop += (s, e) =>
                    {
                        var current = manager.current;
                        var paths = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
                        if (CheckDropAble(e))
                        {
                            manager.Cd(fname);
                            Register(paths, Keyboard.Modifiers == ModifierKeys.Control);
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
                                finally
                                {
                                    manager.current = current;
                                }
                            }
                            SetLayout();
                        }
                        e.Handled = true;
                    };
                    panel.DragEnter += (sender, e) =>
                    {
                        panel.Selected();
                        e.Effects = CheckIfSelf(e, path) ? DragDropEffects.None : DragDropEffects.Move;
                        e.Handled = true;
                    };
                    panel.DragOver += (sender, e) =>
                    {
                        e.Effects = CheckIfSelf(e, path) ? DragDropEffects.None : DragDropEffects.Move;
                        e.Handled = true;
                    };
                    panel.DragLeave += (sender, e) =>
                    {
                        panel.UnSelected();
                    };
                });
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
                                var res = MessageBox.Show($"ディレクトリ名 \"{Util.FormatString(fname, 20, 1, false)}\"を削除します", "Library", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);
                                if (res == MessageBoxResult.Yes)
                                {
                                    Delete(manager.GetPath(fname));
                                    SetLayout();
                                }
                                break;
                            case COPYPATH:
                                Clipboard.SetText(manager.GetPath(fname));
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
                Dispatcher.Invoke(() =>
                {
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
                    var menuItem6 = new MenuItem();
                    menuItem6.Header = "ショートカットを作成";
                    menuItem6.Click += (sender, e) =>
                    {
                        manager.CreateShortCut(fname);
                    };
                    contextMenu.Items.Add(menuItem6);
                    var menuItem5 = new MenuItem();
                    menuItem5.Header = "パスをコピー";
                    menuItem5.Name = COPYPATH;
                    menuItem5.Click += CategoryMenuItemClicked;
                    contextMenu.Items.Add(menuItem5);
                    contextMenu.Items.Add(new Separator());
                    var menuItem4 = new MenuItem();
                    menuItem4.Header = "エクスプローラーで開く";
                    menuItem4.Name = OPENBYEXPLORER;
                    menuItem4.Click += CategoryMenuItemClicked;
                    contextMenu.Items.Add(menuItem4);
                    contextMenu.Items.Add(new Separator());
                    var menuItem1 = new MenuItem();
                    menuItem1.Header = "ディレクトリを作成";
                    menuItem1.Name = NEWCATEGORY;
                    menuItem1.Click += CategoryMenuItemClicked;
                    contextMenu.Items.Add(menuItem1);
                    panel.ContextMenu = contextMenu;
                    panel.ContextMenuOpening += ContentMenuItemOpenning;
                    panel.ContextMenuClosing += ContentMenuItemClosing;
                });
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
                                Process.Start("explorer.exe", drive.Any() ? manager.dcurrent : manager.current);
                                break;
                            case OPEN:
                                panel.DoFocus();
                                break;
                            case OPENWITHSTANDARD:
                                Process.Start(new ProcessStartInfo()
                                {
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
                                    Delete(manager.GetPath(fname));
                                    SetLayout();
                                }
                                break;
                            case COPYPATH:
                                Clipboard.SetText(manager.GetPath(fname));
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
                Dispatcher.Invoke(() =>
                {
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
                    var menuItem8 = new MenuItem();
                    menuItem8.Header = "ショートカットを作成";
                    menuItem8.Click += (sender, e) =>
                    {
                        manager.CreateShortCut(fname);
                    };
                    contextMenu.Items.Add(menuItem8);
                    var menuItem7 = new MenuItem();
                    menuItem7.Header = "パスをコピー";
                    menuItem7.Name = COPYPATH;
                    menuItem7.Click += FileMenuItemClicked;
                    contextMenu.Items.Add(menuItem7);
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
                    menuItem1.Header = "ディレクトリを作成";
                    menuItem1.Name = NEWCATEGORY;
                    menuItem1.Click += FileMenuItemClicked;
                    contextMenu.Items.Add(menuItem1);
                    panel.ContextMenu = contextMenu;
                    panel.ContextMenuOpening += ContentMenuItemOpenning;
                    panel.ContextMenuClosing += ContentMenuItemClosing;
                });
            }
            Dispatcher.Invoke(() =>
            {
                ContentView.Children.Add(panel);
            });
            int col = (content_counter - 1) % 5;
            if (col == 0)
            {
                row++;
                Dispatcher.Invoke(() =>
                {
                    ContentView.RowDefinitions.Add(new RowDefinition());
                });
            }
            Dispatcher.Invoke(() =>
            {
                Grid.SetColumn(panel, col);
                Grid.SetRow(panel, row);
            });
            if (img == image_icon | img == movie_icon)
            {
                Dispatcher.Invoke(() =>
                {
                    availableContents.Add(panel);
                    panel.index = availableContents.Count - 1;
                });
            }
            else if (img == audio_icon)
            {
                Dispatcher.Invoke(() =>
                {
                    availableContents.Add(panel);
                    audioContents.Add(panel);
                    panel.index = availableContents.Count - 1;
                    panel.audio_index = audioContents.Count - 1;
                });
            }
            else if (!isCategory)
            {
                var icon = Util.GetAssociatedImage(path);
                if (icon is not null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        panel.image.Source = icon;
                    });
                }
            }
            if (content_counter % 5 == 0)
            {
                Refresh();
            }
            if (manager.config.preview)
            {
                Task.Run(() =>
                {
                    if (!File.Exists(path)) return;
                    if (img == image_icon)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            panel.image.Source = Util.GenerateBmp(path);
                        });
                    }
                    else if (img == movie_icon)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            var p = new MediaPlayer
                            {
                                ScrubbingEnabled = true,
                                Volume = 0,
                            };
                            p.Open(new Uri(manager.GetPath(fname)));
                            p.Play();
                            p.Pause();
                            var counter = 0;
                            var t = new DispatcherTimer();
                            t.Interval = TimeSpan.FromSeconds(1);
                            t.Tick += (sender, e) =>
                            {
                                if (10 < counter) t.Stop();
                                else if (1 <= p.DownloadProgress && 0 < p.NaturalVideoHeight)
                                {
                                    panel.image.Source = Util.GetThumbnail(p);
                                    t.Stop();
                                    Refresh();
                                }
                                counter++;
                            };
                            t.Start();
                        });
                    }
                });
            }
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
            if (CheckDropAble(e))
            {
                Dispatcher.BeginInvoke(() =>
                {
                    Register((string[])e.Data.GetData(DataFormats.FileDrop), Keyboard.Modifiers == ModifierKeys.Control);
                });
            }
            else
            {
                e.Handled = true;
            }
        }
        private bool CheckDropAble(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return false;
            return Path.GetDirectoryName(((string[])e.Data.GetData(DataFormats.FileDrop))[0]) != manager.current;
        }
        private bool CheckIfSelf(DragEventArgs e, string self)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var d = (string[])e.Data.GetData(DataFormats.FileDrop);
                return d.ToList().Contains(self);
            }
            return false;
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
            
                if (!string.IsNullOrEmpty(SearchBox.Text))
                {
                    manager.Search(SearchBox.Text);
                    SetLayout();
                    isSearchMode = true;
                    ClearSearchWordButton.IsEnabled = true;
                }
                else if (isSearchMode)
                {
                    manager.Search();
                    SetLayout();
                    isSearchMode = false;
                    ClearSearchWordButton.IsEnabled = false;
                }
        }
        private void InitSearch()
        {
            SearchBox.Text = "名前で検索";
            SearchBox.Foreground = TextHintColor;
            ClearSearchWordButton.IsEnabled = false;
        }
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchBox.Foreground == TextHintColor) return;
            if (string.IsNullOrEmpty(SearchBox.Text) & !SearchBox.IsFocused)
            {
                InitSearch();
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            InitSearch();
            manager.Search();
            SetLayout();
            isSearchMode = false;
        }

        private void OnBackButtonDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = BackButton.IsEnabled && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnBackButtonDragLeave(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void OnBackButtonDragOver(object sender, DragEventArgs e)
        {
            e.Effects = BackButton.IsEnabled && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnBackButtonDrop(object sender, DragEventArgs e)
        {
            var copy = Keyboard.Modifiers == ModifierKeys.Control;
            Task.Run(() =>
            {
                var current = manager.current;
                manager.Cd();
                Register((string[])e.Data.GetData(DataFormats.FileDrop), copy);
                manager.current = current;
            });
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Scale.ScaleX = ActualWidth / DEFAULT_WIDTH;
            Scale.ScaleY = ActualHeight / DEFAULT_HEIGHT;
        }
    }

    public class Content: StackPanel
    {
        private const int FilenameWidth = 20;
        private MainWindow window;
        public int index = -1;
        public int audio_index = -1;
        public string path;
        public string filename;
        private bool isCategory;
        private bool isOtherFile;
        private bool isMenuOpen = false;
        private bool isUseAbleName = false;
        private bool isFavorite = false;
        private bool isMousePressing = false;
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
            filename = Path.GetFileName(path);
            Background = isFavorite ? FavoriteBackColor: NormalBackColor;
            Width = 250;
            Height = 120;
            Margin = new Thickness(10);
            ToolTip = filename;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            MouseUp += (sender, e) =>
            {
                isMousePressing = false;
            };
            MouseLeave += (sender, e) =>
            {
                isMousePressing = false;
            };
            MouseDown += (object sender, MouseButtonEventArgs e) =>
            {
                if (2 <= e.ClickCount)
                {
                    DoFocus();
                    if (window.subWindow is not null & !isCategory) window.subWindow.UpdateContents();
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control)
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
                else if (e.LeftButton == MouseButtonState.Pressed)
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
                    var data = new DataObject(DataFormats.FileDrop, new[] { manager.GetPath(filename) });
                    if (DragDrop.DoDragDrop(this, data, DragDropEffects.All) != DragDropEffects.None)
                    {
                        manager.UpdateInfo();
                        window.SetLayout();
                    }
                }
            };
            DragEnter += w.OnDragEnter;
            image = new();
            image.Height = 80;
            image.Width = Width;
            image.Source = img;
            image.Margin = new Thickness(0, 0, 70, 0);
            image.VerticalAlignment = VerticalAlignment.Center;
            Children.Add(image);
            name = new();
            name.Text = Util.FormatString(filename, FilenameWidth, 2);
            name.Height = 37;
            name.Width = Width-80;
            name.FontSize = 14;
            name.Margin = new Thickness(0, 3, 80, 0);
            name.HorizontalContentAlignment = HorizontalAlignment.Left;
            name.VerticalContentAlignment = VerticalAlignment.Center;
            name.TextAlignment = TextAlignment.Center;
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
            name.Text = Util.FormatString(filename, FilenameWidth, 2);
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
                        window.subWindow = new(window, index, audio_index);
                        window.subWindow.Show();
                    }
                    window.subWindow.OpenFile(path, index, audio_index);
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
