using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;

using NAudio.Wave;
using System.Diagnostics;
using System.Xml.Linq;

namespace Cherish
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class Window1 : System.Windows.Window
    {
        private const string CONTINOUS = "ContinousPlayConfig";
        private const string OPENWITHSTANDARD = "OpenWithStandard";
        private const string SPECTRUM = "SpectrumConfig";
        private int type;
        private bool continuous;
        public bool ignore;
        public StackPanel panel;
        public MainWindow window;
        private WaveOutEvent device;
        private AudioPlayer audioPlayer;
        private ImageViewer imageViewer;
        private MoviePlayer moviePlayer;
        public string filename;
        private int index;
        private int audio_index;
        private int maxIndex;
        private bool nowLoading = false;
        private Menu menu = new()
        {
            Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#FF4D5866"),
            Width = 735,
            Height=17,
            VerticalAlignment = VerticalAlignment.Top,
        };
        private DispatcherTimer SeekCancelTimer;
        public Window1(MainWindow w, int idx, int audioidx)
        {
            InitializeComponent();
            window = w;
            index = idx;
            audio_index = audioidx;
            continuous = window.manager.config.continuous;
            var menuItem = new MenuItem()
            {
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("White"),
                Header = "設定",
            };
            var menuItem1 = new MenuItem()
            {
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("Black"),
                Header = "連続再生",
                Name = CONTINOUS,
                IsCheckable = true,
                IsChecked = window.manager.config.continuous
            };
            menuItem1.Click += MenuItemClicked;
            menuItem.Items.Add(menuItem1);
            var menuItem3 = new MenuItem()
            {
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("Black"),
                Header = "スペクトラム表示",
                Name = SPECTRUM,
                IsCheckable = true,
                IsChecked = window.manager.config.spectrum
            };
            menuItem3.Click += MenuItemClicked;
            menuItem.Items.Add(menuItem3);
            menu.Items.Add(menuItem);
            var contextMenu = new ContextMenu();
            var menuItem2 = new MenuItem();
            menuItem2.Header = "既定のアプリで開く";
            menuItem2.Name = OPENWITHSTANDARD;
            menuItem2.Click += MenuItemClicked;
            contextMenu.Items.Add(menuItem2);
            ContextMenu = contextMenu;
            KeyDown += (sender, e) =>
            {
                maxIndex = (audioPlayer is not null & continuous ? window.audioContents : window.availableContents).Count - 1;
                switch (e.Key)
                {
                    case Key.Space:
                        switch (type)
                        {
                            case ContentInfo.AUDIO:
                                if (audioPlayer is not null)
                                {
                                    if (device.PlaybackState == PlaybackState.Playing) audioPlayer.Pause();
                                    else audioPlayer.Play();
                                }
                                break;
                            case ContentInfo.MOVIE:
                                if (moviePlayer is not null)
                                {
                                    if (moviePlayer.isPlaying) moviePlayer.Pause();
                                    else moviePlayer.Play();
                                }
                                break;
                        }
                        break;
                    case Key.Left:
                        if (type == ContentInfo.MOVIE & Keyboard.Modifiers != ModifierKeys.Control)
                        {
                            if (moviePlayer is not null) moviePlayer.Move(-5);
                        }
                        else Back();
                        break;
                    case Key.Right:
                        if (type == ContentInfo.MOVIE & Keyboard.Modifiers != ModifierKeys.Control)
                        {
                            if (moviePlayer is not null) moviePlayer.Move(5);
                        }
                        else Next();
                        break;
                    case Key.F:
                        if (type == ContentInfo.AUDIO) return;
                        else if (WindowState == WindowState.Maximized) WindowState = WindowState.Normal;
                        else WindowState = WindowState.Maximized;
                        break;
                };
            };
            MouseEnter += (sender, e) =>
            {
                if (type == ContentInfo.MOVIE)
                {
                    ignore = true;
                    SeekBar.Value = moviePlayer.GetTime();
                    ignore = false;
                    SeekBar.Visibility = Visibility.Visible;
                    grid.Opacity = 0.5;
                    Cursor = Cursors.Arrow;
                    SeekCancelTimer = new()
                    {
                        Interval = TimeSpan.FromSeconds(3),
                    };
                    SeekCancelTimer.Tick += (sender, e) =>
                    {
                        SeekBar.Visibility = Visibility.Collapsed;
                        grid.Opacity = 1;
                        Cursor = Cursors.None;
                        SeekCancelTimer.Stop();
                    };
                    SeekCancelTimer.Start();
                }
            };
            MouseMove += (sender, e) =>
            {
                if (type == ContentInfo.MOVIE)
                {
                    if (ignore) return;
                    ignore = true;
                    SeekBar.Value = moviePlayer.GetTime();
                    ignore = false;
                    SeekBar.Visibility = Visibility.Visible;
                    grid.Opacity = 0.5;
                    Cursor = Cursors.Arrow;
                    SeekCancelTimer?.Stop();
                    SeekCancelTimer?.Start();
                }
            };
            MouseLeave += (sender, e) =>
            {
                if (type == ContentInfo.MOVIE)
                {
                    SeekBar.Visibility = Visibility.Collapsed;
                    grid.Opacity = 1;
                }
            };
        }
        private void MenuItemClicked(object sender, RoutedEventArgs e)
        {
            var m = (MenuItem)sender;
            switch (m.Name.ToString())
            {
                case CONTINOUS:
                    continuous = !continuous;
                    window.manager.config.ChangeContinuousState();
                    maxIndex = (audioPlayer is not null & continuous ? window.audioContents : window.availableContents).Count - 1;
                    break;
                case OPENWITHSTANDARD:
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = window.manager.GetPath(filename),
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });
                    break;
                case SPECTRUM:
                    window.manager.config.ChangeSpectrumState();
                    break;
            }
        }
        public void Init()
        {
            if (audioPlayer is not null) audioPlayer.Finish();
            if (imageViewer is not null) imageViewer.image.Source = null;
            if (moviePlayer is not null) moviePlayer.Finish();
            SeekBar.Visibility = Visibility.Collapsed;
        }
        public void Next()
        {
            if (nowLoading) return;
            maxIndex = (audioPlayer is not null & continuous ? window.audioContents : window.availableContents).Count - 1;
            var idx = audioPlayer is not null & continuous ? (maxIndex <= audio_index ? 0 : audio_index + 1) : (maxIndex <= index ? 0 : index + 1);
            (audioPlayer is not null & continuous ? window.audioContents : window.availableContents)[idx].DoFocus();
            System.Diagnostics.Debug.WriteLine($"{audioPlayer is not null} {continuous}");
        }
        public void Back()
        {
            if (nowLoading) return;
            maxIndex = (audioPlayer is not null & continuous ? window.audioContents : window.availableContents).Count - 1;
            var idx = audioPlayer is not null & continuous ? (audio_index <= 0 ? maxIndex : audio_index - 1) : (index <= 0 ? maxIndex : index - 1);
            (audioPlayer is not null & continuous ? window.audioContents : window.availableContents)[idx].DoFocus();
        }
        public void OpenFile(string path, int idx, int audioidx)
        {
            if (nowLoading) return;
            nowLoading = true;
            grid.Children.Clear();
            Init();
            var info = new ContentInfo(path);
            filename = System.IO.Path.GetFileName(path);
            Title = $"Cherish  {filename}";
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    type = info.type;
                    switch (type)
                    {
                        case ContentInfo.AUDIO:
                            Height = 380;
                            Width = 735;
                            audioPlayer = new(this, path);
                            device = audioPlayer.device;
                            panel = audioPlayer;
                            WindowState = WindowState.Normal;
                            ResizeMode = ResizeMode.NoResize　| ResizeMode.CanMinimize;
                            break;
                        case ContentInfo.IMAGE:
                            Height = 500;
                            Width = 400;
                            imageViewer = new(this, path);
                            panel = imageViewer;
                            ResizeMode = ResizeMode.CanResize;
                            break;
                        case ContentInfo.MOVIE:
                            Height = 650;
                            Width = 650;
                            moviePlayer = new(this, path);
                            panel = moviePlayer;
                            ResizeMode = ResizeMode.CanResize;
                            break;
                    }
                    grid.Children.Add(panel);
                    Grid.SetRow(panel, 1);
                    if (info.type == ContentInfo.AUDIO)
                    {
                        grid.Children.Add(menu);
                        Grid.SetRow(menu, 0);
                    }
                    Topmost = true;
                    Activate();
                    index = idx;
                    audio_index = audioidx;
                }
                finally
                {
                    nowLoading = false;
                }
            });
        }
        private void OnDragStarted(object sender, DragStartedEventArgs e)
        {
            ignore = true;
        }
        private void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            moviePlayer.MoveTo((int)SeekBar.Value);
            ignore = false;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var isFull = WindowState == WindowState.Maximized;
            if (moviePlayer is not null)
            {
                moviePlayer.Fit(isFull);
                if (isFull) WindowStyle = WindowStyle.None;
                else WindowStyle = WindowStyle.SingleBorderWindow;
            }
            else if (imageViewer is not null) {
                imageViewer.Fit(isFull);
                if (isFull) WindowStyle = WindowStyle.None;
                else WindowStyle = WindowStyle.SingleBorderWindow;
            }
        }

        private void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!ignore & moviePlayer is not null)
            {
                moviePlayer.MoveTo((int)SeekBar.Value);
            }
        }
    }
    public class ContentInfo
    {
        public const int AUDIO = 3;
        public const int IMAGE = 2;
        public const int MOVIE = 1;
        public const int OTHER = 0;
        private string[] AudioExts = new string[5]{".mp3", ".m4a", ".wav", ".aiff", ".aif"};
        private string[] ImageExts = new string[9]{".bmp", ".jpg", ".gif", ".png", ".exif", ".tiff", ".ico", ".wmf", ".emf" };
        private string[] MovieExts = new string[6]{".avi", ".mpg", ".mpeg", ".mov", ".qt", ".mp4"};
        public int type;
        public ContentInfo(string path)
        {
            string ext = System.IO.Path.GetExtension(path);
            if (AudioExts.Contains(ext))
            {
                type = AUDIO;
            }else if (ImageExts.Contains(ext))
            {
                type = IMAGE;
            }else if (MovieExts.Contains(ext))
            {
                type = MOVIE;
            }
            else
            {
                type = OTHER;
            }
        }
    }
}
