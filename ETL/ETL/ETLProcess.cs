using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ETL.BusinessLogic
{
    public class ETLProcess
    {
        FileLogger logger;
        List<FileSystemWatcher> watchers;
        object obj = new object();
        private string _path;
        private string _currFile;
        Meta meta = new Meta();
        public ETLProcess(string path)
        {

            logger = new FileLogger(ConfigurationManager.AppSettings["path2"]);
            _path = path;
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

        public void Stop()
        {
            foreach (var w in watchers)
            {
                w.EnableRaisingEvents = false;
            }
            logger.Log("Service has been stopped successfully");
        }
        public void Triggered()
        {
            string date = DateTime.Now.Date.ToString("yyyy-MM-dd");
            string path = ConfigurationManager.AppSettings["path2"] + date;
            if (Directory.Exists(path))
            {
                meta.CreateFile(path);
                Reset();
            }

        }

        private void FileSystemWatcherCreated(object sender, FileSystemEventArgs e)
        {
                _currFile = e.FullPath;
                try
                {
                    Process(_currFile);
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
            List<User> users;
            List<List<string>> rowsInCols;
            if (File.Exists(filePath))
            {
                rowsInCols = ParseFile(filePath);
                if (rowsInCols.Count == 0)
                    logger.Log("Invalid file: " + filePath);
                else
                {
                    if (type == ".csv")
                    {
                        users = TransformCsv(rowsInCols);
                        Save(ConfigurationManager.AppSettings["path2"], users);
                        Meta.ParsedFiles++;
                    }
                    else if (type == ".txt")
                    {
                        users = TransformTxt(rowsInCols);
                        Save(ConfigurationManager.AppSettings["path2"], users);
                        Meta.ParsedFiles++;
                    }
                    else
                        meta.InvalidFiles.Add(filePath);
                }
            }
        }
        static decimal GetTotal(List<User> users)
        {
            return users.Sum(x => x.Payment);
        }
        void Save(string path, List<User> users)
        {
            var options = new JsonSerializerOptions() { WriteIndented = true };
            options.Converters.Add(new CustomDateTimeConverter("yyyy-MM-dd"));

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

            string json = JsonSerializer.Serialize(preparedList, options);

            string date = DateTime.Now.Date.ToString("yyyy-MM-dd");
            string currPath = path + date;
            if (!Directory.Exists(currPath))
            {
                Directory.CreateDirectory(currPath);
            }
            int fileCount = CountJsonInFolder(currPath);
                    }
        private static int CountJsonInFolder(string path)
        {
            return Directory.GetFiles(path, "*.json").Length;
        }

        private List<User> TransformCsv(List<List<string>> strings)
        {
            List<User> users = new List<User>();

            foreach (var item in strings.Skip(1))
            {
                List<string> row = item;
                bool isCorrect = Check(row);
                if (isCorrect)
                {
                    users.Add(MapLine(row));
                    Meta.ParsedLines++;
                }
                else
                {
                    meta.InvalidFiles.Add(_currFile);
                }

            }
            return users;
        }
        private List<User> TransformTxt(List<List<string>> strings)
        {
            List<User> users = new List<User>();

            foreach (var item in strings)
            {
                List<string> row = item;
                bool isCorrect = Check(row);
                if (isCorrect)
                {
                    users.Add(MapLine(row));
                    Meta.ParsedLines++;
                }
                else
                {
                    meta.InvalidFiles.Add(_currFile);
                }
            }
            return users;
        }

        private static bool Check(List<string> strings)
        {
            var style = NumberStyles.AllowDecimalPoint;
            bool isCorrect = true;
            if (strings.Count != 7)
            {
                Meta.FoundErrors++;
                isCorrect = false;
            }
            else
            {
                foreach (string s in strings)
                {
                    if (s == string.Empty)
                    {
                        Meta.FoundErrors++;
                        isCorrect = false;
                    }
                }
                try
                {
                    if (!decimal.TryParse(strings[3], style, CultureInfo.InvariantCulture, out var x))
                    {
                        isCorrect = false;
                        Meta.FoundErrors++;

                    }
                    if (!DateTime.TryParseExact(strings[4], "yyyy-dd-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var z))
                    {
                        isCorrect = false;
                        Meta.FoundErrors++;
                    }
                    if (!long.TryParse(strings[5], out var y))
                    {
                        isCorrect = false;
                        Meta.FoundErrors++;
                    }

                }
                catch (ArgumentOutOfRangeException)
                {
                    Meta.FoundErrors++;
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

        private List<List<string>> ParseFile(string path, char delimeter = ',')
        {
            var output = new List<List<string>>();
            try
            {
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
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch
                {
                    logger.Log("Файл " + path + "не вдалось обробити. Спробуйте перезапустити сервіс.");
                }
                
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message);
                logger.Log("Перевірте чи правильно відформатовано файл. " + path+  "У файлі має бути однакова кількість стовпчиків," +
                    "розділених комою.");
                meta.InvalidFiles.Add(path);
                return new List<List<string>>();
            }            
            return output;
        }
        public void Reset()
        {
            Meta.ParsedFiles = 0;
            Meta.ParsedLines = 0;
            meta.InvalidFiles = new List<string>();
            Meta.FoundErrors = 0;
        }
    }

}

