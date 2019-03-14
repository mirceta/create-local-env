using si.birokrat.next.common.shell;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace si.birokrat.next.common.registration {
    public static class RegistryUtils {
        public static string GetRegistryValue(string key, string value) {
            string response = null;

            string command = $"reg query \"{key}\" /v \"{value}\"";
            string result = CommandPrompt.Execute(command);

            if (!result.Contains("ERROR: The system was unable to find the specified registry key or value.")) {
                response = ExtractDataByValue(result, value);
            }

            return response;
        }

        public static string GetRegistryValueByPattern(string key, string pattern, string value) {
            string response = null;

            string command = $"reg query \"{key}\" /s /f \"{pattern}\"";
            string result = CommandPrompt.Execute(command);

            if (!result.Contains("End of search: 0 match(es) found.")) {
                response = ExtractDataByValue(result, value);
            }

            return response;
        }

        public static void RegisterService(string filename, string path, bool unregister = true) {
            string command;

            if (unregister) {
                string result = GetRegistryValueByPattern(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\TypeLib", filename, "(Default)");

                if (result != null) {
                    command = $"regsvr32 /s /u \"{result}\"";
                    CommandPrompt.ExecuteInBackground(command, administrator: true, waitForExit: true);
                }
            }

            command = $"regsvr32 /s \"{Path.Combine(path, filename)}\"";
            CommandPrompt.ExecuteInBackground(command, administrator: true, waitForExit: true);
        }

        private static string ExtractDataByValue(string content, string value) {
            string data = null;

            var match = Regex.Match(content, $"{value}.*REG_SZ.*{Environment.NewLine}");
            if (match.Success) {
                data = Regex.Split(match.Value.Trim(), @"\s{4}")[2];
            }

            return data;
        }
    }
}
