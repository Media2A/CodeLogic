using System.Collections;
using System.Reflection;
using CodeLogic;

namespace CodeLogic
{
    public partial class CodeLogic_Framework
    {
        public static void CacheConfigFiles()
        {
            string filePath = CodeLogic_Defaults.GetConfigFilePath();
            DirectoryInfo d = new DirectoryInfo(filePath);
            var isCached = CodeLogic_Funcs.GetCacheBool("IsConfigsCached");

            if (isCached == false)
            {
                foreach (var file in d.GetFiles("*.json"))
                {
                    var jsonData = CodeLogic_Funcs.ReadTextFile(file.FullName);
                    if (CodeLogic_Funcs.TryParseJSON(jsonData))
                    {
                        CodeLogic_Framework.AddLogEntry(LogType.INFO, "Loading config: " + file.FullName);

                        var jsonObject = CodeLogic_Funcs.DeserializeJsonDictionary(jsonData);

                        CodeLogic_Funcs.SetCachedObject(file.Name, jsonObject, 120); // Default Caching time 30 mins
                    }
                    else
                    {
                        CodeLogic_Framework.AddLogEntry(LogType.INFO, "Error loading config(not a valid json format): " + file.FullName);
                        throw new Exception("Error loading config:" + file.FullName + " - not a valid json format");
                    }
                }
                isCached = true;

                CodeLogic_Funcs.SetCachedObject("IsConfigsCached", isCached, 120);
            }
        }

        public static void ValidateConfigFile(string filename, object dataModel)
        {
            if(!CodeLogic_Funcs.CheckCachedObject(filename))
                {
                AddLogEntry(LogType.INFO, "Checking Config file exists: " + CodeLogic_Defaults.GetConfigFilePath() + filename);

                if (!File.Exists(CodeLogic_Defaults.GetConfigFilePath() + filename))
                {
                    AddLogEntry(LogType.INFO, "Writing Config file: " + CodeLogic_Defaults.GetConfigFilePath() + filename);
                    CodeLogic_Framework.WriteConfig(filename, dataModel);
                }
                else
                {
                    AddLogEntry(LogType.INFO, "Config has already been loaded and cached: " + CodeLogic_Defaults.GetConfigFilePath() + filename);
                }

            }
            
        }

        public static void WriteConfig(string file, object obj)
        {
            var jsonData = CodeLogic_Funcs.SerializeObjectJson(obj);
            CodeLogic_Funcs.WriteTextFile(CodeLogic_Defaults.GetConfigFilePath() + file, jsonData);
        }

        public static string GetConfigValueString(string keyFilename, string key)
        {
            object ConfigObject = CodeLogic_Funcs.GetCachedObject(keyFilename);

            var list = (Dictionary<string, string>)ConfigObject;

            return list.GetValueOrDefault(key);

        }
        public static bool GetConfigValueBool(string keyFilename, string key)
        {
            object ConfigObject = CodeLogic_Funcs.GetCachedObject(keyFilename);

            var list = (Dictionary<string, string>)ConfigObject;

            return bool.Parse(list.GetValueOrDefault(key));

        }
        public static int GetConfigValueInt(string keyFilename, string key)
        {
            object ConfigObject = CodeLogic_Funcs.GetCachedObject(keyFilename);

            var list = (Dictionary<string, string>)ConfigObject;

            return int.Parse(list.GetValueOrDefault(key));

        }
    }
}