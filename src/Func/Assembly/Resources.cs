using Microsoft.AspNetCore.Http;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        public static Stream GetImageFromResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceStream(resourceName);
        }
    }
}