using si.birokrat.next.common.build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace si.birokrat.next.common.logging {
    public class Logger2 {

        private static StreamWriter writer = null;

        public static void Log(string method, string message = "", bool toConsole = false) {
            string text = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] ({method}){(string.IsNullOrEmpty(message) ? string.Empty : " -> ")}{message}{Environment.NewLine}";

            if (writer == null) {
                writer = new StreamWriter(Path.Combine(Build.ProjectPath, "log2.txt"), true);
            }

            writer.Write(text);

            if (toConsole) {
                Console.Write(text);
            }
        }
    }
}
