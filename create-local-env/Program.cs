using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using si.birokrat.next.common.build;
using System;
using System.IO;
using System.Net;

namespace create_local_env {
    class Program {

        IConfiguration configuration;

        static void Main(string[] args) {
            Program p = new Program();
            p.configuration = new ConfigurationBuilder()
                .SetBasePath(Build.ProjectPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            p.Start();
        }

        void Start() {
            
            //new InstallsDownloader(configuration).DownloadInstalls();
            new PowershellProfileCompiler(configuration).WriteVarsAndToolsToProfile();
            
            Console.ReadLine();
        }
    }    
}
