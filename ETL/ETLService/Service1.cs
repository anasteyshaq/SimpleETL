using ETL;
using ETL.BusinessLogic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETLService
{
    public partial class Service : ServiceBase
    {
        ETLProcess etl;
        public Service()
        {
            InitializeComponent();
            CanStop = true;
            CanPauseAndContinue = true;
            AutoLog = true;
        }
        
        protected override void OnStart(string[] args)
        {

                //if (File.Exists(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile))
                //{
                    etl = new ETLProcess(ConfigurationManager.AppSettings["path"]);
                    etl.ProcessAll();
                    etl.MonitorDirectory();
                    var trigger = new DailyTrigger(23, 59);
                    trigger.OnTimeTriggered += () => { etl.Triggered(); };
                    Console.ReadKey();
                //}
            }

        protected override void OnStop()
        {
            etl.Stop();
            Thread.Sleep(1000);
        }

    }
}
