using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        public static Dictionary<string, string> ConvertObjectToDictionary(object arg)
        {
            return arg.GetType().GetProperties().ToDictionary(property => property.Name, property => property.GetValue(arg).ToString());
        }
        public static byte[] ObjectToByteArray(object obj)
        {
            byte[] imageBytes = (byte[])obj;

            return imageBytes;
        }
    }
}
