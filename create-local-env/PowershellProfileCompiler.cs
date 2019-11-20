using Microsoft.Extensions.Configuration;
using si.birokrat.next.common.build;
using si.birokrat.next.common.shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace create_local_env {
    class PowershellProfileCompiler {

        IConfiguration configuration;
        string resource_root_path;

        public PowershellProfileCompiler(IConfiguration con) {
            configuration = con;
            resource_root_path = $"{Build.ProjectPath}/resources";
        }

        public void WriteVarsAndToolsToProfile() {

            // backup profile if already exists
            string profile_path = PowerShell.Execute("$PROFILE").Trim();
            if (File.Exists(profile_path)) {
                string profilecontent = File.ReadAllText(profile_path);
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(profile_path), "profile_backup.txt"), profilecontent);
            }

            // assemble the profile using the program's resources
            string vars_dir = Path.Combine(resource_root_path, "vars");
            List<string> lines = new List<string>();

            Array.ForEach(Directory.GetFiles(Path.Combine(resource_root_path, "vars")), 
                         (x) => lines.AddRange(
                             File.ReadAllLines(x).Select((y) => y.Replace("[USER]", configuration["USER"]).Replace("[DISK]", configuration["DISK"]))));


            Array.ForEach(Directory.GetFiles(Path.Combine(resource_root_path, "tools")),
                         (x) => lines.AddRange(
                             File.ReadAllLines(x).Select((y) => 
                                y.Replace("[EXPLORER]", configuration["EXPLORER"])
                                 .Replace("[CODE-EDITOR]", configuration["CODE-EDITOR"])
                                 .Replace("[android-studio-exe]", configuration["android-studio-exe"])
                                 .Replace("{{{utils}}}", configuration["utils"])
                                 )));

            // create the profile if this does not exist
            CreateProfilePathIfNotExists();
            File.WriteAllLines(profile_path, lines);
        }

        #region [auxiliary]
        private void CreateProfilePathIfNotExists() {
            string profile_path = PowerShell.Execute("$PROFILE");
            string[] parts = profile_path.Split("\\");

            Func<int, string> assemble_parts = (max) => {
                return string.Join('\\', parts.Take(max));
            };

            for (int i = 1; i < parts.Length - 2; i++) {
                string str = assemble_parts(parts.Length - i);
                if (!Directory.Exists(str)) {
                    Directory.CreateDirectory(str);
                    Console.WriteLine($"Created directory {assemble_parts(parts.Length - i)}");
                }
            }
        }
        #endregion
    }
}
