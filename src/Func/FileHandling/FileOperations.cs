using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        public static string ReadTextFile(string file)
        {
            string fileContent = File.ReadAllText(file);
            return fileContent;
        }

        public static void WriteTextFile(string file, string fileContent)
        {
            File.WriteAllText(file, fileContent);
        }
        public static void WriteTextFileAllLines(string file, string[] fileContent)
        {
            File.WriteAllLines(file, fileContent);
        }
    }
}
