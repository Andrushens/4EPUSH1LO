using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lab2_service
{
    public partial class Service1 : ServiceBase
    {
        FileWatcher fileWatcher;
        public Service1()
        {
            InitializeComponent();
            DirectoryInfo sourceDirectory = new DirectoryInfo(@"C:\Users\thela\source\repos\953506\term3\dot_net\lab2\SourceDirectory");
            DirectoryInfo targetDirectory = new DirectoryInfo(@"C:\Users\thela\source\repos\953506\term3\dot_net\lab2\TargetDirectory");

            fileWatcher = new FileWatcher(sourceDirectory, targetDirectory);
            CanStop = true;
            CanPauseAndContinue = true;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            Thread fileWatcherThread = new Thread(new ThreadStart(fileWatcher.Process));
            fileWatcherThread.Start();
        }

        protected override void OnStop()
        {
        }
    }
}
