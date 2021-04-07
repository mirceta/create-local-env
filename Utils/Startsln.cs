using si.birokrat.next.common.build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Utils
{
    class Startsln
    {
        static string projectsFile = Path.Combine(Build.ProjectPath, "projects.txt");

        public static void Start(string[] args)
        {
            if (args[0] == "help")
            {
                Console.WriteLine(@"
                    Usage: 
                        startsln .    = start the solution in current folder
                        startsln list = list all saved projects
                        startsln go {proj} = start project at list index
                        startsln store {optional:project_name} = store current solution folder as project
                ");
                return;
            }
            try
            {
                Console.WriteLine(Work(args));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static string Work(string[] args)
        {
            switch (args[0])
            {

                case "list":
                    return List();
                case "go":
                    return Go(args[1]);
                case "store":
                    NoSlnFileInCurrentDirectoryGuard();

                    string key = null;
                    if (args.Length > 1)
                    {
                        key = args[1];
                    }
                    return Store(key);

            }

            return "Incorrect option";
        }

        static string List()
        {
            ProjectRepositoryExistsGuard();

            string[] s = File.ReadAllLines(projectsFile);
            return string.Join("\n", s.Select(x => {
                string[] parts = x.Split(";");
                return parts[0] + string.Join("", Enumerable.Range(0, Math.Max(30 - parts[0].Length, 0)).Select(y => " ").ToArray()) + parts[1];
            }));
        }

        static string Go(string key)
        {
            ProjectRepositoryExistsGuard();

            var projectDictionary = GetProjectDictionary();

            if (projectDictionary.ContainsKey(key))
            {
                Directory.SetCurrentDirectory(projectDictionary[key]);
                return projectDictionary[key];
            }
            else
            {
                throw new Exception("No such project");
            }
        }

        static string Store(string key = null)
        {
            if (key == null)
                key = Directory.GetCurrentDirectory().Split('\\', '/').TakeLast(1).First();
            string path = Directory.GetCurrentDirectory();
            Dictionary<string, string> projs = GetProjectDictionary();
            projs[key] = path;
            SetProjectFile(projs);
            return "Successfully stored";
        }

        #region // auxiliary //
        static void ProjectRepositoryExistsGuard()
        {
            if (!File.Exists(Path.Combine(Build.ProjectPath, "projects.txt")))
                throw new Exception("You need to store something first!");
        }

        static void NoSlnFileInCurrentDirectoryGuard()
        {
            if (Directory.GetFiles(Directory.GetCurrentDirectory()).Where(x => x.Contains(".sln")).ToArray().Length != 1)
                throw new Exception("There is no solution file in the current directory!");
        }

        static Dictionary<string, string> GetProjectDictionary()
        {
            if (!File.Exists(projectsFile)) {
                return new Dictionary<string, string>();
            }
            return File.ReadAllLines(projectsFile)
                            .Select(x => {
                                string[] parts = x.Split(";");
                                return new { key = parts[0], path = parts[1] };
                            })
                            .ToDictionary(x => x.key, x => x.path);
        }

        static void SetProjectFile(Dictionary<string, string> projectDictionary)
        {
            string[] vals = projectDictionary.Keys.Select(x => x + ";" + projectDictionary[x]).ToArray();
            File.WriteAllText(projectsFile, string.Join("\n", vals));
        }
        #endregion
    }
}
