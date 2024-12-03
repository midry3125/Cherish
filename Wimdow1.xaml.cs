using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

using NAudio.Wave;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Cherish
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class Window1 : System.Windows.Window
    {
        private int type;
        public bool isDrag;
        public StackPanel panel;
        private MainWindow window;
        private WaveOutEvent device;
        private AudioPlayer audioPlayer;
        private ImageViewer imageViewer;
        private MoviePlayer moviePlayer;
        public string filename;
        private int index;
        private int maxIndex;
        private bool nowLoading = false;
        private DispatcherTimer SeekCancelTimer;
        public Window1(MainWindow w, int idx)
        {
            InitializeComponent();
            window = w;
            index = idx;
            KeyDown += (sender, e) =>
            {
                maxIndex = window.availableContents.Count - 1;
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
                        if (type == ContentInfo.MOVIE)
                        {
                            if (moviePlayer is not null)
                            {
                                moviePlayer.Move(-5);
                            }
                        }
                        else Back();
                        break;
                    case Key.Right:
                        if (type == ContentInfo.MOVIE)
                        {
                            if (moviePlayer is not null)
                            {
                                moviePlayer.Move(5);
                            }
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
                    SeekBar.Value = moviePlayer.GetTime();
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
        public void Init()
        {
            if (audioPlayer is not null) audioPlayer.Finish();
            else if (imageViewer is not null) imageViewer.image.Source = null;
            else if (moviePlayer is not null) moviePlayer.Finish();
        }
        public void Next()
        {
            if (nowLoading) return;
            maxIndex = window.availableContents.Count - 1;
            var idx = maxIndex <= index ? 0 : index + 1;
            window.availableContents[idx].DoFocus();
        }
        public void Back()
        {
            if (nowLoading) return;
            maxIndex = window.availableContents.Count - 1;
            var idx = index <= 0 ? maxIndex : index - 1;
            window.availableContents[idx].DoFocus();
        }
        public void OpenFile(string path, int idx)
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
                            ResizeMode = ResizeMode.NoResize;
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
                    Topmost = true;
                    Activate();
                    index = idx;
                }
                finally
                {
                    nowLoading = false;
                }
            });
        }
        private void OnDragStarted(object sender, DragStartedEventArgs e)
        {
            isDrag = true;
        }
        private void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            moviePlayer.MoveTo((int)SeekBar.Value);
            isDrag = false;
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
            if (!isDrag & moviePlayer is not null)
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
