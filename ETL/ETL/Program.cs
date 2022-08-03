using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETL.BusinessLogic
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ETLProcess etl = new ETLProcess(ConfigurationManager.AppSettings["path"]);  
            etl.ProcessAll();
            etl.MonitorDirectory();
            Console.ReadKey();
        }
    }
}
