using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeLogic
{
    public partial class CodeLogic_Defaults
    {
        // Define base dir

        private static string baseDir = AppDomain.CurrentDomain.BaseDirectory + "/"; // App dir + ../

        // Returns path to logs, config etc.
        public static string GetBaseFilePath()
        {
            return $"{baseDir}/";
        }
        public static string GetDataFilePath()
        {
            return $"{baseDir}data/";
        }
        public static string GetLogFilePath()
        {
            return $"{GetDataFilePath()}/logs/";
        }
        public static string GetStorageFilePath()
        {
            return $"{GetDataFilePath()}/storage/";
        }
        public static string GetConfigFilePath()
        {
            return $"{GetDataFilePath()}/configs/";
        }
        public static string GetLocalizationFilePath()
        {
            return $"{GetDataFilePath()}/localization/";
        }
    }
}
