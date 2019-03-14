using si.birokrat.next.common.build;
using si.birokrat.next.common.logging;
using si.birokrat.next.common.shell;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace si.birokrat.next.common.deployment {
    public static class Deployer {
        private const string DEFAULT_DESTINATION_PATH = "deploy";

        public static bool PublishCoreProject(string projectName, string destinationPath = DEFAULT_DESTINATION_PATH) {
            Console.Write($"Publishing {projectName} project");

            string solutionPath = Build.SolutionPath;
            string projectPath = Path.Combine(solutionPath, projectName);
            string projectDeployPath = Path.Combine(solutionPath, destinationPath, projectName);

            if (Directory.Exists(projectDeployPath)) {
                Directory.Delete(projectDeployPath, recursive: true);
            }

            string command = $"dotnet publish \"{projectPath}\" " +
                $"--configuration Release " +
                $"--framework netcoreapp2.1 " +
                $"--output \"{projectDeployPath}\" " +
                $"--verbosity normal";
            string result = CommandPrompt.Execute(command);

            var success = result.Contains("Build succeeded");
            if (success) {
                CopyAndDeleteSettings(projectPath, projectDeployPath);
                CreateCleanLog(projectDeployPath);
                Console.WriteLine("SUCCESS");
            } else {
                Console.WriteLine("...ERROR\n\nPress any key to exit.");
                Console.ReadLine();
                Environment.Exit(-1);
            }

            return success;
        }

        public static bool PublishFrameworkProject(string projectName, string destinationPath = DEFAULT_DESTINATION_PATH) {
            string msBuildPath = GetMSBuildPath();

            Console.Write($"Publishing {projectName} project");

            string solutionPath = Build.SolutionPath;
            string projectPath = Path.Combine(solutionPath, projectName);
            string projectDeployPath = Path.Combine(solutionPath, destinationPath, projectName);

            if (Directory.Exists(projectDeployPath)) {
                Directory.Delete(projectDeployPath, recursive: true);
            }

            string command = $"\"{msBuildPath}\" " +
                $"/target:Publish " +
                $"/p:Configuration=Release " +
                $"/p:OutDir={Path.Combine(projectDeployPath, @"bin\Release")} " +
                $"\"{projectPath}\"";
            string result = CommandPrompt.Execute(command);

            var success = result.Contains("Build succeeded");
            if (success) {
                CreateCleanLog(projectDeployPath);
                Console.WriteLine("SUCCESS");
            } else {
                Console.WriteLine("...ERROR\n\nPress any key to exit.");
                Console.ReadLine();
                Environment.Exit(-1);
            }

            return success;
        }

        private static string GetMSBuildPath() {
            var vsWherePath = Path.Combine(Build.SolutionPath, @"common\deployment\tools\vswhere.exe");
            string result = CommandPrompt.Execute(vsWherePath);

            var match = Regex.Match(result, $"installationPath:.*{Environment.NewLine}");
            if (match.Success) {
                var visualStudioInstallPath = match.Value.Trim().Split(new[] { ": " }, StringSplitOptions.None)[1];
                return Path.Combine(visualStudioInstallPath, @"MSBuild\15.0\Bin\MSBuild.exe");
            } else {
                Console.WriteLine("Could not determine 'MSBuild.exe' path.");
                Console.ReadLine();
                Environment.Exit(-1);
            }

            return null;
        }

        private static void CopyAndDeleteSettings(string projectPath, string projectDeployPath) {
            var secretsProductionPath = Path.Combine(projectPath, "appsettings.Secrets.Production.json");
            var secretsExamplePath = Path.Combine(projectPath, "appsettings.Secrets.Example.json");
            var secretsDeployPath = Path.Combine(projectDeployPath, "appsettings.Secrets.json");

            if (File.Exists(secretsProductionPath)) {
                File.Copy(secretsProductionPath, secretsDeployPath, overwrite: true);
            } else {
                File.Copy(secretsExamplePath, secretsDeployPath, overwrite: true);
            }

            var developmentDeployPath = Path.Combine(projectDeployPath, "appsettings.Development.json");
            File.Delete(developmentDeployPath);

            var secretsProductionDeployPath = Path.Combine(projectDeployPath, "appsettings.Secrets.Production.json");
            if (File.Exists(secretsProductionDeployPath)) {
                File.Delete(secretsProductionDeployPath);
            }

            var secretsExampleDeployPath = Path.Combine(projectDeployPath, "appsettings.Secrets.Example.json");
            File.Delete(secretsExampleDeployPath);
        }

        private static void CreateCleanLog(string projectDeployPath) {
            var logPath = Path.Combine(projectDeployPath, Logger.FILE);
            File.WriteAllText(logPath, string.Empty);
        }
    }
}
