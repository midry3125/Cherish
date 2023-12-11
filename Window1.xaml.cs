using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        public StackPanel panel;
        private MainWindow window;
        private WaveOutEvent device;
        private AudioPlayer audioPlayer;
        private ImageViewer imageViewer;
        public string filename;
        private int index;
        private int maxIndex;
        private bool nowLoading = false;
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
                        if (audioPlayer is not null)
                        {
                            if (device.PlaybackState == PlaybackState.Playing) audioPlayer.Pause();
                            else audioPlayer.Play();
                        }
                        break;
                    case Key.Left:
                        Back();
                        break;
                    case Key.Right:
                        Next();
                        break;
                }
            };
        }
        public void Init()
        {
            if (audioPlayer is not null) audioPlayer.Finish();
            else if (imageViewer is not null) imageViewer.image.Source = null;
        }
        public void Next()
        {
            maxIndex = window.availableContents.Count - 1;
            if (nowLoading) return;
            var idx = maxIndex <= index ? 0 : index + 1;
            window.availableContents[idx].DoFocus();
        }
        public void Back()
        {
            maxIndex = window.availableContents.Count - 1;
            if (nowLoading) return;
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
            audioPlayer = null;
            imageViewer = null;
            filename = System.IO.Path.GetFileName(path);
            Title = $"Cherish  {filename}";
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    switch (info.type)
                    {
                        case ContentInfo.AUDIO:
                            Height = 380;
                            Width = 735;
                            audioPlayer = new(this, path);
                            device = audioPlayer.device;
                            panel = audioPlayer;
                            break;
                        case ContentInfo.IMAGE:
                            Height = 500;
                            Width = 400;
                            imageViewer = new(this, path);
                            panel = imageViewer;
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
    }
    public class ContentInfo
    {
        public const string AUDIO = "a";
        public const string IMAGE = "i";
        public const string OTHER = "unknown";
        private string[] AudioExts = new string[4]{".mp3", ".wav", ".aiff", ".aif"};
        private string[] ImageExts = new string[9]{".bmp", ".jpg", ".gif", ".png", ".exif", ".tiff", ".ico", ".wmf", ".emf" };
        public string type;
        public ContentInfo(string path)
        {
            string ext = System.IO.Path.GetExtension(path);
            if (AudioExts.Contains(ext))
            {
                type = AUDIO;
            }else if (ImageExts.Contains(ext))
            {
                type = IMAGE;
            }
            else
            {
                type = OTHER;
            }
        }
    }
}
