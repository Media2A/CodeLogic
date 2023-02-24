using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {

        public static bool TryParseJSON(string json)
        {
            JObject jObject;
            try
            {
                jObject = JObject.Parse(json);
                return true;
            }
            catch
            {
                jObject = null;
                return false;
            }
        }

        public static object DeserializeObjectJson(string jsondata)
        {
            return JsonConvert.DeserializeObject(jsondata);
        }
        public static IDictionary<string, string> DeserializeJsonDictionary(string jsondata)
        {
            Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsondata);
            return dict;
        }
        
        public static string SerializeObjectJson(object jsonObject)
        {
            return JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
        }
        public static string GetJsonValue(string jsonObject, string key)
        {
            JObject obj = JObject.Parse(jsonObject);
            return (string)obj[key].ToString();
        }
    }
}
