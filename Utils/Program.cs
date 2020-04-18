using si.birokrat.next.common.shell;
using System;
using System.IO;
using System.Linq;

namespace Utils {
    class Program {
        static void Main(string[] args) {

            string program = args[0];
            args = args.Skip(1).ToArray();
            switch (program) {
                case "envarlist":
                    call_envvarlist(args);
                    break;
                case "startsln":
                    call_startsln(args);
                    break;
                default:
                    break;

            }
        }

        private static void call_startsln(string[] args) {
            Startsln.Start(args);
        }

        private static void call_envvarlist(string[] args) {
            if (args.Length == 2) {
                envvarlist(args[1]);
            } else {
                Console.WriteLine("There should be exactly two args");
                Environment.Exit(1);
            }
        }


        private static void envvarlist(string searchstring) {
            string profile_path = PowerShell.Execute("$PROFILE").Trim();
            string[] lines = File.ReadAllLines(profile_path);
            string[] found_lines = lines.Where(
                (x) => {
                    if (x.Contains("$") && x.Contains(searchstring))
                        return true;
                    return false;
                }).ToArray();
            found_lines.ToList().Sort();
            found_lines.ToList().ForEach((x) => Console.WriteLine(x));
        }
    }
}
