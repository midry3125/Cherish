using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cherish
{
    public class Util
    {
        static string[] suffix = { "", "K", "M", "G", "T", "P", "E", "Z", "Y" };
        const int BORDER = 1024;
        public static int CountString(string s)
        {
            return Encoding.GetEncoding("Shift_JIS").GetByteCount(s);
        }
        public static string Substring(string s, int start, int num=-1)
        {
            if (CountString(s) <= start) return "";
            var res = "";
            var count = 0;
            if (num < 0) num = s.Length*2;
            for (int i=0; ; i++)
            {
                if (s.Length <= i) break;
                var c = s[i].ToString();
                count += CountString(c);
                if (count < start) continue;
                else if (start+num < count) break;
                else res += c;
            }
            return res;
        }
        public static string FormatString(string s, int col, int row=1, bool space=true)
        {
            var length = CountString(s);
            if (length <= col)
            {
                if (!space) return s;
                string sp = new string(' ', (col - length) / 2);
                return sp + s + sp;
            }
            else
            {
                string res = "";
                int end = 0;
                for (int i = 0; i < row; i++)
                {
                    var start = col * i + 1;
                    if (length < start)
                    {
                        break;
                    }
                    else if (i+1 < row)
                    {
                        res += Substring(s, start, col) + "\n";
                    }
                    else
                    {
                        res += Substring(s, start, col-6);
                    }
                    end = start + col;
                }
                return end <= length ?  res.Trim() + "..." : res.Trim();
            }
        }
        public static BitmapImage GenerateBmp(string path)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(path);
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        public static BitmapImage GenerateBmp(byte[] b)
        {
            var stream = new MemoryStream(b);
            stream.Seek(0, SeekOrigin.Begin);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = stream;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        public static BitmapImage GenerateBmp(System.Drawing.Bitmap b)
        {
            using (var ms = new MemoryStream())
            {
                b.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);
                var res = new BitmapImage();
                res.BeginInit();
                res.CacheOption = BitmapCacheOption.OnLoad;
                res.StreamSource = ms;
                res.EndInit();
                return res;
            }
        }
        public static BitmapSource ConvertImage(System.Drawing.Image i)
        {
            using (var ms = new MemoryStream())
            {
                i.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                var res = (BitmapSource)BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return res;
            }
        }
        public static RenderTargetBitmap GetThumbnail(MediaPlayer p)
        {
            p.Position = TimeSpan.FromSeconds(5);
            var v = new DrawingVisual();
            using (var ctx = v.RenderOpen())
            {
                ctx.DrawVideo(p, new System.Windows.Rect(0, 0, p.NaturalVideoWidth, p.NaturalVideoHeight));
            }
            var bmp = new RenderTargetBitmap(p.NaturalVideoWidth, p.NaturalVideoHeight, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(v);
            return bmp;
        }
        public static string GetFormatFileSize(string path)
        {
            var f = new FileInfo(path);
            var idx = 0;
            double s = f.Length;
            while (BORDER <= s)
            {
                s /= BORDER;
                idx++;
            }
            return $"{Math.Round(s, 1, MidpointRounding.AwayFromZero)}{suffix[idx]}";
        }
        public static string GetFormatFileSize(double s)
        {
            var idx = 0;
            while (BORDER <= s)
            {
                s /= BORDER;
                idx++;
            }
            return $"{Math.Round(s, 1, MidpointRounding.AwayFromZero)}{suffix[idx]}B";
        }
        public static string GetUnUsedOath(string name, string dir)
        {
            string ext = Path.GetExtension(name);
            string b = Path.GetFileNameWithoutExtension(name);
            int num = 0;
            var p = Path.Combine(dir, b + ext);
            while (File.Exists(p) || Directory.Exists(p))
            {
                num += 1;
                p = Path.Combine(dir, $"{b}({num}){ext}");
            }
            return p;
        }
        public static BitmapSource? GetAssociatedImage(string path)
        {
            var icon = Icon.ExtractAssociatedIcon(path);
            if (icon is null)
            {
                return null;
            }
            else
            {
                using (var s = new MemoryStream())
                {
                    icon.ToBitmap().Save(s, System.Drawing.Imaging.ImageFormat.Bmp);
                    s.Seek(0, SeekOrigin.Begin);
                    return BitmapFrame.Create(s, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }
            }
        }
    }
}