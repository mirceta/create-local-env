using System;
using System.IO;

namespace si.birokrat.next.common.io {
    public static class FileUtils {
        public static void CopyDirectory(string sourcePath, string targetPath) {
            DirectoryInfo sourceInfo = new DirectoryInfo(sourcePath);
            DirectoryInfo targetInfo = new DirectoryInfo(targetPath);

            CopyAll(sourceInfo, targetInfo);
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target) {
            if (source.FullName.ToLower() == target.FullName.ToLower()) {
                return;
            }

            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(target.FullName) == false) {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into it's new directory.
            foreach (FileInfo fileInfo in source.GetFiles()) {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fileInfo.Name);
                fileInfo.CopyTo(Path.Combine(target.ToString(), fileInfo.Name), overwrite: true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo sourceInfo in source.GetDirectories()) {
                DirectoryInfo targetInfo = target.CreateSubdirectory(sourceInfo.Name);
                CopyAll(sourceInfo, targetInfo);
            }
        }
    }
}
