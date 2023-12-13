using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Drawing;

using NAudio.Wave;
using NAudio.WaveFormRenderer;
using System.Windows.Input;

namespace Cherish
{
    internal class AudioPlayer: StackPanel
    {
        private string path;
        private string filename;
        public bool isBarDrawn = false;
        private bool isStoppedByUser = false;
        private bool isSliderChangeAble = true;
        private bool isSliderChangedByUser = false;
        private int beforePos = 0;
        private double ignoreMinChange;
        private StackPanel spectrum;
        private Slider slider;
        private System.Windows.Controls.Image played_spectrum;
        private System.Windows.Controls.Image yet_play_spectrum;
        private DispatcherTimer timer;
        private DispatcherTimer autoReplayTimer;
        private AudioFileReader stream;
        public WaveOutEvent device = new WaveOutEvent();
        public AudioPlayer(Window1 w, string p)
        {
            path = p;
            filename = System.IO.Path.GetFileName(path);
            var grid = new Grid();
            spectrum = new StackPanel();
            var grid2 = new Grid();
            played_spectrum = new System.Windows.Controls.Image();
            yet_play_spectrum = new System.Windows.Controls.Image();
            played_spectrum.Width = 0;
            yet_play_spectrum.Width = 700;
            played_spectrum.Stretch = Stretch.None;
            yet_play_spectrum.Stretch = Stretch.None;
            played_spectrum.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            yet_play_spectrum.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            played_spectrum.MouseDown += DragFile;
            yet_play_spectrum.MouseDown += DragFile;
            grid2.Children.Add(yet_play_spectrum);
            grid2.Children.Add(played_spectrum);
            spectrum.Children.Add(grid2);
            spectrum.Height = 500;
            spectrum.Width = 1500;
            grid.Children.Add(spectrum);
            slider = new();
            slider.Width = 400;
            slider.Height = 30;
            slider.Minimum = 0;
            slider.Maximum = 100;
            slider.Margin = new Thickness(50, 110, 50, 0);
            slider.IsMoveToPointEnabled= true;
            slider.ValueChanged += OnSlider;
            grid.Children.Add(slider);
            Children.Add(grid);
            timer = new(DispatcherPriority.Normal);
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Update;
            autoReplayTimer = new();
            autoReplayTimer.Interval= TimeSpan.FromSeconds(1);
            autoReplayTimer.Tick += (sender, e) =>
            {
                try
                {
                    stream.Position = 0;
                    Play();
                }
                catch { }
            };
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    stream = new AudioFileReader(path);
                    ignoreMinChange = stream.Length * 0.02; // 無視するスライダー変異数、メモリ二つ分
                    var rstream = new AudioFileReader(path);
                    var renderer = new WaveFormRenderer();
                    var averagePeakProvider = new AveragePeakProvider(4);
                    var played = new SoundCloudBlockWaveFormSettings(System.Drawing.Color.FromArgb(77, 90, 175), System.Drawing.Color.FromArgb(77, 90, 175), System.Drawing.Color.FromArgb(125, 171, 255), System.Drawing.Color.FromArgb(199, 213, 255));
                    var yet_play = new SoundCloudBlockWaveFormSettings(System.Drawing.Color.FromArgb(52, 52, 52), System.Drawing.Color.FromArgb(55, 55, 55), System.Drawing.Color.FromArgb(27, 27, 30), System.Drawing.Color.FromArgb(40, 40, 40));
                    yet_play.Width = (int)spectrum.Width;
                    yet_play.TopHeight = (int)spectrum.Height / 4 * 3;
                    yet_play.BottomHeight = (int)spectrum.Height / 4;
                    yet_play.BackgroundColor = ColorTranslator.FromHtml("#FF1B1B1B");
                    yet_play.PixelsPerPeak = 2;
                    played.Width = (int)spectrum.Width;
                    played.TopHeight = (int)spectrum.Height / 4 * 3;
                    played.BottomHeight = (int)spectrum.Height / 4;
                    played.BackgroundColor = ColorTranslator.FromHtml("#FF1B1B1B");
                    played.PixelsPerPeak = 2;
                    yet_play_spectrum.Source = Util.ConvertImage(renderer.Render(rstream, averagePeakProvider, yet_play));
                    rstream.Position = 0;
                    played_spectrum.Source = Util.ConvertImage(renderer.Render(rstream, averagePeakProvider, played));
                    rstream.Close();
                    played.Width = 0;
                    stream.Position = 0;
                    device.Init(stream);
                    device.Play();
                    timer.Start();
                }
                catch (Exception e)
                {
                    System.Windows.MessageBox.Show($"エラーが発生しました\n({e.Message})", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    Finish();
                    w.Close();
                }
            });
        }
        private void DragFile(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var tmp = Path.Combine(Path.GetTempPath(), filename);
                File.Copy(path, tmp, true);
                var data = new DataObject(DataFormats.FileDrop, new[] { tmp });
                data.SetData("Source", this);
                DragDrop.DoDragDrop(this, data, DragDropEffects.All);
            }
        }
        private void OnSlider(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var pos = (int)(stream.Length * (double)slider.Value / 100);
            autoReplayTimer.Stop();
            if (isSliderChangedByUser & Math.Abs(pos - beforePos) > ignoreMinChange)
            {
                stream.Position = pos;
                beforePos = pos;
            }
            isSliderChangedByUser = true;
            var p = (double)pos / stream.Length;
            var width = (int)(p * yet_play_spectrum.Width);
            if (width != played_spectrum.Width) played_spectrum.Width = width;
        }

        public void Finish()
        {
            device.Stop();
            device.Dispose();
            if (stream is not null) stream.Close();
            timer.Stop();
        }
        public void Pause()
        {
            device.Pause();
            device.Stop();
            timer.Stop();
            isSliderChangeAble = false;
            isStoppedByUser = true;
        }
        public void Play()
        {
            device.Play();
            isSliderChangeAble = true;
            isStoppedByUser = false;
            timer.Start();
        }

        private void Update(object sender, EventArgs e)
        {
            if (!System.Windows.Application.Current.Windows.OfType<Window1>().Any()) Finish();
            else if (device.PlaybackState == PlaybackState.Playing)
            {
                var p = (double)stream.Position / stream.Length;
                var width = (int)(p * yet_play_spectrum.Width);
                if (width != played_spectrum.Width) played_spectrum.Width = width;
                if (isSliderChangeAble)
                {
                    isSliderChangedByUser = false;
                    slider.Value = (int)(p * 100);
                    beforePos = (int)stream.Position;
                }
            }
            else if (!isStoppedByUser)
            {
                timer.Stop();
                autoReplayTimer.Start();
            }
            else timer.Stop();
        }
    }
}
