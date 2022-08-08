using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ETL
{
    internal class Meta
    {
        public static int ParsedFiles { get; set; }
        public static int ParsedLines { get; set; }
        public static int FoundErrors { get; set; }
        public List<string> InvalidFiles { get; set; }
        public Meta()
        {
            InvalidFiles = new List<string>();
        }
       
        public void CreateFile(string path)
        {
            var fullPath = path + '\\' +"meta.log";
            var options = new JsonSerializerOptions() { WriteIndented = true };
            string json = JsonSerializer.Serialize(InvalidFiles,options);
            using (StreamWriter writer = new StreamWriter(fullPath))
            {
                writer.WriteLine("parsed_files: {0}", ParsedFiles);
                writer.WriteLine("parsed_lines: {0}", ParsedLines);
                writer.WriteLine("found_errors: {0}", FoundErrors);
                writer.WriteLine("Invalid_files:"+json.Replace("\\\\","\\"));

            }
            // Read a file  
            string readText = File.ReadAllText(fullPath);
            Console.WriteLine(readText);
        }
    }
}
