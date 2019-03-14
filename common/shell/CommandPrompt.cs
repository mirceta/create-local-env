namespace si.birokrat.next.common.shell {
    public static class CommandPrompt {
        public const string FILENAME = "cmd";

        public static string Execute(string command, bool administrator = false, string workingDirectory = "") {
            return ShellUtils.Execute(FILENAME, command, administrator, workingDirectory);
        }

        public static void ExecuteInBackground(string command, bool administrator = false, bool waitForExit = false, string workingDirectory = "") {
            ShellUtils.ExecuteInBackground(FILENAME, command, administrator, waitForExit, workingDirectory);
        }

        public static string Execute(string command, out int id, bool administrator = false, string workingDirectory = "") {
            return ShellUtils.ExecutePID(FILENAME, command, administrator, workingDirectory, out id);
        }

        public static void ExecuteInBackgroundPID(string command, out int id, bool administrator = false, bool waitForExit = false, string workingDirectory = "") {
            ShellUtils.ExecuteInBackgroundPID(FILENAME, command, administrator, waitForExit, workingDirectory, out id);
        }
    }
}


