using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms.Design;
using System.Collections.Immutable;
using System.DirectoryServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Cherish
{
    public class Manager
    {
        private string[] AudioExts = new string[4] { ".mp3", ".wav", ".aiff", ".aif" };
        private string[] ImageExts = new string[9] { ".bmp", ".jpg", ".gif", ".png", ".exif", ".tiff", ".ico", ".wmf", ".emf" };
        public string program_dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "LibraryDir");
        public string current;
        public string root;
        public List<string> files = new();
        public List<string> audioFiles;
        public List<string> imageFiles;
        public List<string> otherFiles;
        public List<string> categories;
        public Manager()
        {
            root = Path.Combine(program_dir, "Files");
            current = root;
            Console.WriteLine(program_dir);
            if (Directory.Exists(program_dir))
            {
                UpdateInfo();
            }
            else 
            {
                Directory.CreateDirectory(program_dir);
                File.SetAttributes(program_dir, FileAttributes.Normal);
                Directory.CreateDirectory(Path.Combine(program_dir, "Files"));
            }
        }

        private string GetUnusedName(string name)
        {
            string ext = Path.GetExtension(name);
            string b = Path.GetFileNameWithoutExtension(name);
            string res = b + ext;
            int num = 0;
            while (Contains(res)){
                num += 1;
                res = $"{b}({num}){ext}";
            }
            return res;
        }
        public bool Contains(string name)
        {
            return files.Contains(name) | categories.Contains(name);
        }
        public void UpdateInfo()
        {
            files = new();
            audioFiles = new();
            imageFiles = new();
            otherFiles = new();
            categories = Directory.GetDirectories(current).Select(d => Path.GetFileName(d)).ToList();
            categories.Sort();
            foreach (string f in Directory.GetFiles(current))
            {
                var path = Path.GetFileName(f);
                files.Add(path);
                var ext = Path.GetExtension(path);
                if (AudioExts.Contains(ext)) audioFiles.Add(path);
                else if (ImageExts.Contains(ext)) imageFiles.Add(path);
                else otherFiles.Add(path);
                audioFiles.Sort();
                imageFiles.Sort();
                otherFiles.Sort();
            }
        }

        public string GetPath(string name)
        {
            return Path.Combine(current, name);
        }

        public void Cd(string name="")
        {
            if (name == "")
            {
                current = Path.GetDirectoryName(current);
            }
            else
            {
                current = Path.Combine(current, name);
            }
            UpdateInfo();
        }
        public bool CreateCategory(string name)
        {
            var r = !Contains(name);
            if (r)
            {
                Directory.CreateDirectory(GetPath(name));
                UpdateInfo();
            }
            return r;
        }

        public (string?, Exception?) AddFile(string path)
        {
            string name = GetUnusedName(path);
            try
            {
                if (File.Exists(path))
                {
                    File.Move(path, GetPath(name));
                }
                else
                {
                    Directory.Move(path, GetPath(name));
                }
            }
            catch (Exception e)
            {
                return (null, e);
            }
            UpdateInfo();
            return (GetPath(name), null);
        }
        public void Delete(string name)
        {
            var path = GetPath(name);
            if (categories.Contains(name))
            {
                Directory.Delete(path, true);
            }
            else
            {
                File.Delete(path);
            }
            UpdateInfo();
        }
        public void Rename(string from, string to)
        {
            var fpath = GetPath(from);
            var tpath = GetPath(to);
            if (Directory.Exists(fpath)) Directory.Move(fpath, tpath);
            else File.Move(fpath, GetPath(tpath));
            UpdateInfo();
        }
    }
}