using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ETL.BusinessLogic
{
    public class ETLProcess
    {
        List<FileSystemWatcher> watchers;
        private static int _parsedFiles;
        private static int _parsedLines;
        private static int FoundErrors { get; set; }
        private string _path;
        private static List<string> InvalidFiles { get; set; }
        public ETLProcess(string path)
        {
            _path = path;
        }
        public void Log()
        {

        }
        public void MonitorDirectory()
        {
            string[] filters = { "*.txt", "*.csv" };
            watchers = new List<FileSystemWatcher>();

            foreach (string f in filters)
            {
                FileSystemWatcher w = new FileSystemWatcher(_path);
                w.Filter = f;
                w.Changed += FileSystemWatcherCreated;
                w.EnableRaisingEvents = true;
                watchers.Add(w);

            }
        }
        private void FileSystemWatcherCreated(object sender, FileSystemEventArgs e)
        {

            Console.WriteLine("File created!");
            string filePath = e.FullPath;
            try
            {
                Process(filePath);
                foreach (var w in watchers)
                {
                    w.EnableRaisingEvents = false;
                }
            }
            finally
            {
                foreach (var w in watchers)
                {
                    w.EnableRaisingEvents = true;
                }
            }

        }
        public void ProcessAll()
        {
            DirectoryInfo d = new DirectoryInfo(_path);
            foreach (var file in d.GetFiles())
            {
                Process(file.FullName);
            }
        }
        private void Process(string filePath)
        {

            string type = Path.GetExtension(filePath);
            List<User> users = new List<User>();
            List<List<string>> rowsInCols;
            if (File.Exists(filePath))
            {
                rowsInCols = ParseCsv(filePath);
                if (type == ".csv")
                {
                    users = TransformCsv(rowsInCols);
                    Save(ConfigurationManager.AppSettings["path2"], users);
                    _parsedFiles++;
                }
                else if (type == ".txt")
                {
                    users = TransformTxt(rowsInCols);
                    Save(ConfigurationManager.AppSettings["path2"], users);
                    _parsedFiles++;
                }
                else
                {
                    InvalidFiles.Add(filePath);
                }

            }


        }
        static decimal GetTotal(List<User> users)
        {
            return users.Sum(x => x.Payment);
        }
        static void Save(string path, List<User> users)
        {
            var options = new JsonSerializerOptions() { WriteIndented = true };
            options.Converters.Add(new CustomDateTimeConverter("yyyy-MM-dd"));

            //Producing the result in a specified format
            var preparedList = from item in users
                         group item by item.Address.City into cityGroup
                         select new
                         {
                             city = cityGroup.Key,
                             services = (
                                 from item2 in cityGroup
                                 group item2 by item2.Service into serviceGroup
                                 select new
                                 {
                                     name = serviceGroup.Key,
                                     payers = (
                                        from item3 in serviceGroup
                                        select new
                                        {
                                            name = item3.FirstName + ' ' + item3.LastName,
                                            payment = item3.Payment,
                                            date = item3.Date,
                                            account_number = item3.AccountNumber
                                        }),
                                     total = GetTotal(serviceGroup.ToList())
                                 }
                             ),
                             total = GetTotal(cityGroup.ToList())
                         };

            //Serializing list of objects to json string
            string json = JsonSerializer.Serialize(preparedList, options);

            string date = DateTime.Now.Date.ToShortDateString();
            string currPath = path + date;
            if (!Directory.Exists(currPath))
            {
                Directory.CreateDirectory(currPath);
            }
            int fileCount = CountJsonInFolder(currPath);
            File.WriteAllText(currPath + @"\input" + (fileCount + 1) + ".json", json);
        }
        private static int CountJsonInFolder(string path)
        {
            return Directory.GetFiles(path, "*.json").Length;
        }

        private static List<User> TransformCsv(List<List<string>> strings)
        {
            List<User> users = new List<User>();

            foreach (var item in strings.Skip(1))
            {
                List<string> row = item;
                bool isCorrect = Check(row);
                if (isCorrect)
                    users.Add(MapLine(row));
            }
            return users;
        }
        private static List<User> TransformTxt(List<List<string>> strings)
        {
            List<User> users = new List<User>();

            foreach (var item in strings)
            {
                List<string> row = item;
                bool isCorrect = Check(row);
                if (isCorrect)
                    users.Add(MapLine(row));
            }
            return users;
        }

        private static bool Check(List<string> strings)
        {
            bool isCorrect = true;
            if (strings.Count != 7)
            {
                FoundErrors++;
                isCorrect = false;
            }
            else
            {
                foreach (string s in strings)
                {
                    if (s == string.Empty)
                    {
                        FoundErrors++;
                        isCorrect = false;
                    }
                }
                try
                {
                    if (!decimal.TryParse(strings[3], out var x))
                    {
                        isCorrect = false;
                        FoundErrors++;
                    }
                    if (!DateTime.TryParseExact(strings[4], "yyyy-dd-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var z))
                    {
                        isCorrect = false;
                        FoundErrors++;
                    }
                    if (!long.TryParse(strings[5], out var y))
                    {
                        isCorrect = false;
                        FoundErrors++;
                    }

                }
                catch (ArgumentOutOfRangeException)
                {
                    FoundErrors++;
                    isCorrect = false;
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.Message);
                }
            }
            return isCorrect;
        }

        private static User MapLine(List<string> row)
        {
            var myDate = DateTime.ParseExact(row[4], "yyyy-dd-MM", CultureInfo.InvariantCulture, DateTimeStyles.None);
            var address = row[2];
            User user = new User()
            {
                FirstName = row[0],
                LastName = row[1],
                Address = new Address()
                {
                    City = (from p in address.Split(',')
                            select p).First(),
                    AddressLine = (from p in address.Split(',').Skip(1)
                                   select p).ToString()
                },
                Payment = Convert.ToDecimal(row[3]),
                Date = myDate,
                AccountNumber = long.Parse(row[5]),
                Service = row[6]
            };
            return user;
        }


        private List<List<string>> ParseCsv(string path, char delimeter = ',')
        {
            var output = new List<List<string>>();

            using (var csv = new CsvReader(new StreamReader(path), false, delimeter))
            {
                while (csv.ReadNextRecord())
                {
                    var row = new List<string>();
                    for (int i = 0; i < csv.FieldCount; i++)
                    {
                        row.Add(csv[i]);
                    }
                    output.Add(row);
                }
            }
            File.Delete(path);
            return output;
        }

        private static void PrintException(Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine("Stacktrace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                PrintException(ex.InnerException);
            }
        }
    }
}


