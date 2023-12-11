using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace Cherish
{
    public class Util
    {
        public static int CountString(string s)
        {
            return Encoding.GetEncoding("Shift_JIS").GetByteCount(s);
        }
        public static string FormatString(string s, int col, int row=1, bool space=true)
        {
            var length = CountString(s);
            if (length <= col)
            {
                if (!space) return s;
                string sp = new string(' ', (col - length));
                return sp + s + sp;
            }
            else
            {
                string res = "";
                for (int i = 0; i < row; i++)
                {
                    var start = 24 * i;
                    if (s.Length < start)
                    {
                        break;
                    }
                    else if (i+1 < row)
                    {
                        res += s.Substring(start, col - 3) + "\n";
                    }
                    else
                    {
                        res += s.Length <= start+col-6 ? s.Substring(start) : s.Substring(start, col-6);
                    }
                }
                return res + "...";
            }
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
            using (var ms = new System.IO.MemoryStream())
            {
                i.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                var res = (BitmapSource)BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return res;
            }
        }
    }
}
