namespace si.birokrat.next.common.shell {
    public static class Filename {
        public static void ExecuteInBackground(string filename, string arguments, out int id, bool administrator = false, bool waitForExit = false, string workingDirectory = "") {
            ShellUtils.ExecuteInBackgroundPID(filename, arguments, administrator, waitForExit, workingDirectory, out id);
        }
    }
}
