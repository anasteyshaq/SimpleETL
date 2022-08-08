using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Globalization;


namespace ETL.BusinessLogic
{
    internal class Program
    {
        private static ETLProcess etl = new ETLProcess(ConfigurationManager.AppSettings["path"]);
        static void Main(string[] args)
        {
            bool showMenu = true;
            while (showMenu)
            {
                showMenu = Menu();
            }

        }
        private static bool Menu()
        {
            Console.Clear();
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1) Start");
            Console.WriteLine("2) Stop");
            Console.WriteLine("3) Reset");
            Console.WriteLine("4) Exit");
            Console.Write("\r\nSelect an option: ");

            switch (Console.ReadLine())
            {
                case "1":
                    Start();
                    return true;
                case "2":
                    Stop();
                    return true;
                case "3":
                    return true;
                case "4":
                    return false;
                default:
                    return true;
            }
        }
        public static void Start()
        {
            Console.Clear();
            Console.WriteLine("Process started");         
            etl.ProcessAll();
            etl.MonitorDirectory();
            var trigger = new DailyTrigger(23, 59);
            trigger.OnTimeTriggered += () =>
            {
                etl.Triggered();
            };

        }
        public static void Stop()
        {
            Console.Clear();
            etl.Stop();
            Console.WriteLine("Process stopped");
        }
        public static void Reset()
        {
            Console.Clear();
            etl.Reset();
            Console.WriteLine("Reset is done");
        }
    }
}

