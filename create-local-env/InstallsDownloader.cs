using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using si.birokrat.next.common.build;
using System.IO;
using System.Net;
using System.Text;

namespace create_local_env {
    class InstallsDownloader {

        IConfiguration configuration;

        public InstallsDownloader(IConfiguration configuration) {
            this.configuration = configuration;
        }

        public void DownloadInstalls() {
            CreateInstallsDirectoryIfNotExists();
            dynamic[] arr = GetItemsFromResources();
            DownloadEachResource(arr);
        }

        #region [auxiliary]
        private void CreateInstallsDirectoryIfNotExists() {
            if (!Directory.Exists(configuration["installs_save_directory"])) {
                Directory.CreateDirectory(configuration["installs_save_directory"]);
            }
        }

        private void DownloadEachResource(dynamic[] arr) {
            foreach (var x in arr) {
                WebClient client = new WebClient();
                client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(OnDownloadDataCompleted(
                    Path.Combine(configuration["installs_save_directory"], (string)x.name)));
                client.DownloadDataAsync(new Uri((string)x.link));
            }
        }

        private static dynamic[] GetItemsFromResources() {
            string json = File.ReadAllText($"{Build.ProjectPath}\\resources\\installs\\windows.json");
            dynamic[] arr = JsonConvert.DeserializeObject<dynamic[]>(json);
            return arr;
        }

        private Action<object, DownloadDataCompletedEventArgs> OnDownloadDataCompleted(string path_to_save) {
            return new Action<object, DownloadDataCompletedEventArgs>(
                (x, y) => {
                    File.WriteAllBytes($"{path_to_save}.exe", y.Result);
                    Console.WriteLine(path_to_save + "download completed");
                }
            );
        }
        #endregion
    }
}
