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
        private string[] AudioExts = new string[4] { ".mp3", ".wav", ".aiff", ".aif" };
        private string[] ImageExts = new string[9] { ".bmp", ".jpg", ".gif", ".png", ".exif", ".tiff", ".ico", ".wmf", ".emf" };
        private string[] MovieExts = new string[6] { ".avi", ".mpg", ".mpeg", ".mov", ".qt", ".mp4" };
        public string drive="";
        public static string program_dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Cherish");
        public static string config_file = Path.Combine(program_dir, "config.json");
        public Config config;
        public string current;
        public string dcurrent="";
        public string root;
        public string search_word="";
        public bool isChanged;
        public List<string> files = new();
        public List<string> audioFiles = new();
        public List<string> imageFiles = new();
        public List<string> movieFiles = new();
        public List<string> otherFiles = new();
        public List<string> categories = new();

        public List<string> faudioFiles = new();
        public List<string> fimageFiles = new();
        public List<string> fmovieFiles = new();
        public List<string> fotherFiles = new();
        public List<string> fcategories = new();
        public Manager()
        {
            root = Path.Combine(program_dir, "Files");
            current = root;
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
            File.WriteAllText(config_file, JsonSerializer.Serialize(config));
        }
        public void RemoveFavorite(string name)
        {
            var p = GetPath(name);
            config.favorites.Remove(p);
            fcategories.Remove(name);
            faudioFiles.Remove(name);
            fmovieFiles.Remove(name);
            fimageFiles.Remove(name);
            fotherFiles.Remove(name);
            File.WriteAllText(config_file, JsonSerializer.Serialize(config));
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
    }
}