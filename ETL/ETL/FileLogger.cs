using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETL
{
    internal class FileLogger
    {
        string _path;
        public FileLogger(string path)
        {
            _path = path;
        }
        public void Log(string message)
        {
            using (StreamWriter writer = File.AppendText(_path+"log.txt"))
            {
                writer.WriteLine(DateTime.Now.ToString() + " " + message);
            }
        }
    }
}
