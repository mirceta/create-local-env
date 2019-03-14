using si.birokrat.next.common.build;
using System;
using System.IO;

namespace si.birokrat.next.common.logging {
    public static class Logger {
        public const string FILE = "log.txt";

        public static void Log(string method, string message = "", bool toLog = true, bool toConsole = false, string path = FILE) {
            string text = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] ({method}){(string.IsNullOrEmpty(message) ? string.Empty : " -> ")}{message}{Environment.NewLine}";

            if (toLog) {
                if (path == FILE) {
                    path = Path.Combine(Build.ProjectPath, FILE);
                }

                File.AppendAllText(path, text);
            }

            if (!toLog || toConsole) {
                Console.Write(text);
            }
        }
    }
}
