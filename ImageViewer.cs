using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Cherish
{
    public class ImageViewer: StackPanel
    {
        public Image image;
        private string path;
        private string filename;
        private Window1 window;
        public ImageViewer(Window1 w, string p)
        {
            window = w;
            path = p;
            filename = Path.GetFileName(path);
            image = new Image();
            image.Height = window.Height - 50;
            image.Width = window.Width - 30;
            image.MouseDown += DragFile;
            Dispatcher.BeginInvoke(() =>
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path);
                bmp.EndInit();
                var widthP = (double)bmp.Width / image.Width;
                var heightP = (double)bmp.Height / image.Height;
                var p = widthP < heightP ? heightP : widthP;
                if (1 < p)
                {
                    bmp.DecodePixelWidth = (int)(bmp.Width / p);
                    bmp.DecodePixelHeight= (int)(bmp.Height / p);
                }
                image.Source = bmp;
                Children.Add(image);
            });
        }
        private void DragFile(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), filename);
                File.Copy(path, tmp, true);
                var data = new System.Windows.DataObject(System.Windows.DataFormats.FileDrop, new[] { tmp });
                data.SetData("Source", this);
                DragDrop.DoDragDrop(this, data, System.Windows.DragDropEffects.All);
            }
        }
    }
}
