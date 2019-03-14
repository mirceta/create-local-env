using System;
using System.Diagnostics;
using System.IO;

namespace si.birokrat.next.common.shell {
    internal static class ShellUtils {
        internal static string Execute(string filename, string command, bool administrator, string workingDirectory) {
            return ExecutePID(filename, command, administrator, workingDirectory, out int id);
        }

        internal static void ExecuteInBackground(string filename, string command, bool administrator, bool waitForExit, string workingDirectory) {
            ExecuteInBackgroundPID(filename, command, administrator, waitForExit, workingDirectory, out int id);
        }

        internal static string ExecutePID(string filename, string command, bool administrator, string workingDirectory, out int id) {
            var tempFile = Path.GetTempFileName();

            ProcessStartInfo info = new ProcessStartInfo {
                FileName = filename,
                Arguments = $"/c \"{command} > \"{tempFile}\"\"",
                Verb = administrator ? "runAs" : string.Empty,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true
            };

            if (!string.IsNullOrEmpty(workingDirectory)) {
                info.WorkingDirectory = workingDirectory;
            }

            Process process = new Process() { StartInfo = info };
            process.Start();
            id = process.Id;
            while (!process.WaitForExit(1000)) {
                Console.Write(".");
            }
            process.Close();

            string result = File.ReadAllText(tempFile);
            File.Delete(tempFile);

            if (result.Contains("Run as administrator")) {
                throw new UnauthorizedAccessException("The requested operation requires elevation (Run as administrator).");
            }

            return result;
        }

        internal static void ExecuteInBackgroundPID(string filename, string command, bool administrator, bool waitForExit, string workingDirectory, out int id) {
            ProcessStartInfo info = new ProcessStartInfo {
                FileName = filename,
                Arguments = filename == "cmd" || filename == "powershell" ? $"/c \"{command}\"" : command,
                Verb = administrator ? "runAs" : string.Empty,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true
            };

            if (!string.IsNullOrEmpty(workingDirectory)) {
                info.WorkingDirectory = workingDirectory;
            }

            Process process = new Process() { StartInfo = info };
            process.Start();
            id = process.Id;
            if (waitForExit) {
                process.WaitForExit();
            }
            process.Close();
        }
    }
}
