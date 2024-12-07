using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections.Immutable;
using System.Text.Json;

namespace Cherish
{
    public class Manager
    {
        private string[] AudioExts = new string[5] { ".m4a" ,".mp3", ".wav", ".aiff", ".aif" };
        private string[] ImageExts = new string[9] { ".bmp", ".jpg", ".gif", ".png", ".exif", ".tiff", ".ico", ".wmf", ".emf" };
        private string[] MovieExts = new string[6] { ".avi", ".mpg", ".mpeg", ".mov", ".qt", ".mp4" };
        public static string program_dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Cherish");
        public static string config_file = Path.Combine(program_dir, "config.json");
        public string root = Path.Combine(program_dir, "Files");
        public Config config;
        public string drive;
        public string current;
        public string dcurrent;
        public string search_word;
        public bool isChanged;
        public List<string> files;
        public List<string> audioFiles;
        public List<string> imageFiles;
        public List<string> movieFiles;
        public List<string> otherFiles;
        public List<string> categories;
        public List<string> faudioFiles;
        public List<string> fimageFiles;
        public List<string> fmovieFiles;
        public List<string> fotherFiles;
        public List<string> fcategories;
        public Manager()
        {
            Init();
            if (!Directory.Exists(program_dir))
            {
                Directory.CreateDirectory(program_dir);
                File.SetAttributes(program_dir, FileAttributes.Normal);
                Directory.CreateDirectory(Path.Combine(program_dir, "Files"));
                config = new Config();
                config.Update();
            }
            else if (!File.Exists(config_file))
            {
                config = new Config();
                config.Update();
            }
            else
            {
                config = JsonSerializer.Deserialize<Config>(File.ReadAllText(config_file))!;
            }
            UpdateInfo();
        }
        public void Init()
        {
            current = root;
            drive = "";
            dcurrent = "";
            search_word = "";
            files = new();
            audioFiles = new();
            imageFiles = new();
            movieFiles = new();
            otherFiles = new();
            categories = new();
            faudioFiles = new();
            fimageFiles = new();
            fmovieFiles = new();
            fotherFiles = new();
            fcategories = new();
    }
        public void AddFavorite(string name)
        {
            var p = GetPath(name);
            var ext = Path.GetExtension(p);
            config.favorites.Add(p);
            if (Directory.Exists(p))
            {
                fcategories.Add(p);
                categories.Remove(name);
            }
            else if (AudioExts.Contains(ext))
            {
                faudioFiles.Add(p);
                audioFiles.Remove(name);
            }
            else if (ImageExts.Contains(ext))
            {
                fimageFiles.Add(p);
                imageFiles.Remove(name);
            }
            else if (MovieExts.Contains(ext))
            {
                fmovieFiles.Add(p);
                movieFiles.Remove(name);
            }
            else
            {
                fotherFiles.Add(p);
                otherFiles.Remove(name);
            }
            config.Update();
        }
        public void RemoveFavorite(string name)
        {
            var p = GetPath(name);
            config.favorites.Remove(p);
            if (fcategories.Contains(name))
            {
                fcategories.Remove(name);
                categories.Add(name);
                categories.Sort();
            }
            if (faudioFiles.Contains(name))
            {
                faudioFiles.Remove(name);
                audioFiles.Add(name);
                audioFiles.Sort();
            }
            else if (fimageFiles.Contains(name))
            {
                fimageFiles.Remove(name);
                imageFiles.Add(name);
                imageFiles.Sort();
            }
            else if (fmovieFiles.Contains(name))
            {
                fmovieFiles.Remove (name);
                movieFiles.Add(name);
                movieFiles.Sort();
            }
            else
            {
                fotherFiles.Remove (name);
                otherFiles.Add(name);
                otherFiles.Sort();
            }
            config.Update();
        }
        public void SetDrive(string d)
        {
            drive = d;
            dcurrent = drive;
            UpdateInfo();
        }
        public void RemoveDrive()
        {
            drive = "";
            UpdateInfo();
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
            return files.Contains(name) | categories.Contains(name) | fcategories.Contains(name);
        }
        public void Search(string word)
        {
            search_word = word;
            UpdateInfo();
        }
        public void Search()
        {
            search_word = "";
            UpdateInfo();
        }
        public void UpdateInfo()
        {
            List<string> tfiles = new();
            List<string> taudioFiles = new();
            List<string> timageFiles = new();
            List<string> tmovieFiles = new();
            List<string> totherFiles = new();
            List<string> tcategories = new();
            var target = drive == "" ? current : dcurrent;
            var ecategories = Directory.GetDirectories(target).Select(d => Path.GetFileName(d));
            if (search_word != "")
            {
                ecategories = ecategories.Where(d => d.Contains(search_word));
            }
            tcategories = ecategories.ToList();
            foreach (string f in Directory.GetFiles(target))
            {
                var name = Path.GetFileName(f);
                if (search_word != "")
                {
                    if (!name.Contains(search_word)) continue;
                }
                tfiles.Add(name);
                var ext = Path.GetExtension(name);
                if (AudioExts.Contains(ext)) taudioFiles.Add(name);
                else if (ImageExts.Contains(ext)) timageFiles.Add(name);
                else if (MovieExts.Contains(ext)) tmovieFiles.Add(name);
                else totherFiles.Add(name);
            }
            isChanged = tcategories.Except(categories).Except(fcategories).Any() | categories.Except(tcategories).Any() | tfiles.Except(files).Any() | files.Except(tfiles).Any();
            fcategories = tcategories.Where(d => config.favorites.Contains(GetPath(d))).ToList();
            faudioFiles = taudioFiles.Where(d => config.favorites.Contains(GetPath(d))).ToList();
            fmovieFiles = tmovieFiles.Where(d => config.favorites.Contains(GetPath(d))).ToList();
            fimageFiles = timageFiles.Where(d => config.favorites.Contains(GetPath(d))).ToList();
            fotherFiles = tfiles.Except(taudioFiles).Except(tmovieFiles).Except(timageFiles).Where(d => config.favorites.Contains(GetPath(d))).ToList();
            categories = tcategories.Except(fcategories).ToList();
            files = tfiles;
            audioFiles = taudioFiles.Except(faudioFiles).ToList();
            imageFiles = timageFiles.Except(fimageFiles).ToList();
            movieFiles = tmovieFiles.Except(fmovieFiles).ToList();
            otherFiles = totherFiles.Except(fotherFiles).ToList();
            categories.Sort();
            audioFiles.Sort();
            imageFiles.Sort();
            movieFiles.Sort();
            otherFiles.Sort();
            fcategories.Sort();
            faudioFiles.Sort();
            fimageFiles.Sort();
            fmovieFiles.Sort();
            fotherFiles.Sort();
        }

        public string GetPath(string name)
        {
            return Path.Combine(drive=="" ? current : dcurrent, name);
        }
        public void Cd()
        {
            if (drive == "")
            {
                if (current != root)
                {
                    current = Path.GetDirectoryName(current);
                    UpdateInfo();
                }
            }else if (drive != dcurrent)
            {
                dcurrent = Path.GetDirectoryName(dcurrent);
                UpdateInfo();
            }
        }

        public void Cd(string name)
        {
            if (drive == "")
            {
                current = Path.Combine(current, name);
                UpdateInfo();
            }
            else
            {
                dcurrent = Path.Combine(dcurrent, name);
                UpdateInfo();
            }
        }
        public bool CreateCategory(string name, bool update=true)
        {
            if (name == "") return false;
            var r = !Contains(name);
            if (r)
            {
                Directory.CreateDirectory(GetPath(name));
                if (update) UpdateInfo();
            }
            return r;
        }

        public string AddFile(string path, bool update=true)
        {
            string name = GetUnusedName(path);
            if (File.Exists(path))
            {
                File.Move(path, GetPath(name));
            }
            else if (Directory.Exists(path))
            {
                Directory.Move(path, GetPath(name));
            }
            if (update) UpdateInfo();
            return GetPath(name);
        }
        public void Delete(string name, bool update=true)
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
            if (update) UpdateInfo();
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

    public class Config
    {
        public List<string> favorites { set; get; } = new();
        public bool preview { set; get; } = true;
        public bool continuous { set; get; } = false;
        public bool spectrum { set; get; } = false;
        public void Update()
        {
            File.WriteAllText(Manager.config_file, JsonSerializer.Serialize(this));
        }
        public void ChangePreviewState()
        {
            preview = !preview;
            Update();
        }
        public void ChangeContinuousState()
        {
            continuous = !continuous;
            Update();
        }
        public void ChangeSpectrumState()
        {
            spectrum = !spectrum;
            Update();
        }
    }
}