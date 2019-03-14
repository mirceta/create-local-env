using si.birokrat.next.common.conversion;
using si.birokrat.next.common.shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace si.birokrat.next.common.processing {
    public class ProcessUtils {
        public static bool KillbyPID(int pid, bool recurse = false) {
            string command = $"taskkill /f /pid {pid}";

            List<int> children = new List<int>();

            if (recurse) {
                children = GetChildren(pid, recurse);
                foreach (int childPid in children) {
                    command += $" /pid {childPid}";
                }
            }
            
            string result = CommandPrompt.Execute(command);

            return Regex.Matches(result, "SUCCESS").Count == children.Count + 1;
        }

        public static bool KillByName(string name, List<int> exclude = null) {
            string command;
            string result;

            if (exclude == null) {
                exclude = new List<int>();
            }

            command = $"wmic process where(Name = \"{name}\") get ProcessId";
            result = CommandPrompt.Execute(command);

            List<int> pids = result
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .Select(x => TypeConverter.StringToInteger(x.Trim()))
                .Where(x => !exclude.Contains(x))
                .ToList();

            command = $"taskkill /f";
            foreach (int pid in pids) {
                command += $" /pid {pid}";
            }
            result = CommandPrompt.Execute(command);

            return Regex.Matches(result, "SUCCESS").Count == pids.Count;
        }

        public static List<int> GetChildren(int pid, bool recurse = false) {
            string command = $"wmic process where(ParentProcessId = {pid}) get ProcessId";
            string result = CommandPrompt.Execute(command);

            List<int> children = result
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .Select(x => TypeConverter.StringToInteger(x.Trim()))
                .ToList();

            if (recurse) {
                for (int i = 0; i < children.Count; i++) {
                    children.AddRange(GetChildren(children[i], recurse));
                }                
            }

            return children;
        }
    }
}
