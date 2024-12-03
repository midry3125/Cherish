using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Cherish
{
    public class MoviePlayer: StackPanel
    {
        private double maxWidth = SystemParameters.PrimaryScreenWidth;
        private double maxHeight = SystemParameters.PrimaryScreenHeight;
        private string path;
        private string filename;
        private MediaElement player;
        private Window1 window;
        public bool isPlaying;
        private DispatcherTimer timer;
        public MoviePlayer(Window1 w, string p)
        {
            path = p;
            window = w;
            filename = System.IO.Path.GetFileName(path);
            player = new()
            {
                ScrubbingEnabled = true,
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                Source = new Uri(path),
                Stretch = System.Windows.Media.Stretch.Uniform
            };
            player.MediaOpened += (sender, e) =>
            {
                w.SeekBar.Maximum = player.NaturalDuration.TimeSpan.TotalSeconds;
            };
            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;
            Fit();
            Children.Add(player);
            Play();
            timer = new();
            timer.Interval = TimeSpan.FromMilliseconds(2);
            timer.Tick += (sender, e) =>
            {
                if (!System.Windows.Application.Current.Windows.OfType<Window1>().Any()) Finish();
            };
            timer.Start();
        }
        public void Pause()
        {
            player.Pause();
            isPlaying = false;
        }
        public void Play()
        {
            player.Play();
            isPlaying = true;
        }
        public int GetTime()
        {
            return (int)player.Position.TotalSeconds;
        }

        public void Fit(bool max=false)
        {
            player.Width = max ? maxWidth : window.Width*0.9;
            player.Height = max ? maxHeight : window.Height*0.9;
        }
        public void Move(int seconds)
        {
            player.Position += TimeSpan.FromSeconds(seconds);
        }
        public void MoveTo(int seconds)
        {
            player.Position = TimeSpan.FromSeconds(seconds);
        }
        public void Finish()
        {
            player.Stop();
            timer.Stop();
            isPlaying = false;
        }
    }
}
