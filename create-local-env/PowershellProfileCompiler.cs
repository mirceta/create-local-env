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
            string profile_path = PowerShell.Execute("$PROFILE");
            if (File.Exists(profile_path)) {
                string profilecontent = File.ReadAllText(profile_path);
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(profile_path), "profile_backup.txt"), profilecontent);
            }

            // assemble the profile using the program's resources
            string vars_dir = Path.Combine(resource_root_path, "vars");
            List<string> lines = new List<string>();
            Array.ForEach(Directory.GetFiles(Path.Combine(resource_root_path, "vars")), (x) => lines.AddRange(File.ReadAllLines(x).ToList()));
            Array.ForEach(Directory.GetFiles(Path.Combine(resource_root_path, "tools")), (x) => lines.AddRange(File.ReadAllLines(x).ToList()));

            // create the profile if this does not exist
            CreateProfilePathIfNotExists();
            File.WriteAllLines(PowerShell.Execute("$PROFILE"), lines);
        }

        #region [auxiliary]
        private void CreateProfilePathIfNotExists() {
            string profile_path = PowerShell.Execute("$PROFILE");
            string[] parts = profile_path.Split("/\\");

            Func<int, string> assemble_parts = (max) => {
                string acc = "";
                for (int i = 0; i < max; i++) {
                    acc += parts[i];
                }
                return acc;
            };

            for (int i = 1; i < parts.Length - 2; i++) {
                if (!Directory.Exists(assemble_parts(parts.Length - i))) {
                    Directory.CreateDirectory(assemble_parts(parts.Length - i));
                    Console.WriteLine($"Created directory {assemble_parts(parts.Length - i)}");
                }
            }
        }
        #endregion
    }
}
