using System;
using System.Threading;
using System.Globalization;

namespace CodeLogic
{
    public partial class CodeLogic_Framework
    {

        public static void CacheLocalizationFiles()
        {
            string filePath = CodeLogic_Defaults.GetLocalizationFilePath();
            DirectoryInfo d = new DirectoryInfo(filePath);
            var isCached = CodeLogic_Funcs.GetCacheBool("IsLocalizationCached");

            if (isCached == false)
            {
                foreach (var file in d.GetFiles("*.json"))
                {
                    var jsonData = CodeLogic_Funcs.ReadTextFile(file.FullName);
                    if (CodeLogic_Funcs.TryParseJSON(jsonData))
                    {
                        CodeLogic_Framework.AddLogEntry(LogType.INFO, "Loading Localization: " + file.FullName);

                        var jsonObject = CodeLogic_Funcs.DeserializeJsonDictionary(jsonData);

                        CodeLogic_Funcs.SetCachedObject(file.Name, jsonObject, 60); // Default Caching time 30 mins
                    }
                    else
                    {
                        CodeLogic_Framework.AddLogEntry(LogType.INFO, "Error loading Localization(not a valid json format): " + file.FullName);
                        throw new Exception("Error loading Localization:" + file.FullName + " - not a valid json format");
                    }
                }
                isCached = true;

                CodeLogic_Funcs.SetCachedObject("IsLocalizationCached", isCached, 60);
            }
        }

        public static void ValidateLocalizationFile(string filename)
        {
            if (!CodeLogic_Funcs.CheckCachedObject(filename))
            {
                AddLogEntry(LogType.INFO, "Checking Localization file exists: " + CodeLogic_Defaults.GetConfigFilePath() + filename);

                if (!File.Exists(CodeLogic_Defaults.GetConfigFilePath() + filename))
                {
                    AddLogEntry(LogType.INFO, "Writing Localization file: " + CodeLogic_Defaults.GetConfigFilePath() + filename);
                    
                    // CodeLogic_Framework.WriteConfig(filename, dataModel);
                }
                else
                {
                    AddLogEntry(LogType.INFO, "Localization has already been loaded and cached: " + CodeLogic_Defaults.GetConfigFilePath() + filename);
                }

            }

        }

        public static string GetLocalizationValueString(string keyFilename, string key)
        {
            object LocalizationObject = CodeLogic_Funcs.GetCachedObject(keyFilename);

            var list = (Dictionary<string, string>)LocalizationObject;

            return list.GetValueOrDefault(key);

        }
        public static bool GetLocalizationValueBool(string keyFilename, string key)
        {
            object LocalizationObject = CodeLogic_Funcs.GetCachedObject(keyFilename);

            var list = (Dictionary<string, string>)LocalizationObject;

            return bool.Parse(list.GetValueOrDefault(key));

        }
        public static int GetLocalizationValueInt(string keyFilename, string key)
        {
            object LocalizationObject = CodeLogic_Funcs.GetCachedObject(keyFilename);

            var list = (Dictionary<string, string>)LocalizationObject;

            return int.Parse(list.GetValueOrDefault(key));

        }

    }
}
