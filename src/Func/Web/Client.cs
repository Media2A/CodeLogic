using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        // Url
        public static string GetClientIP(HttpContext context)
        {
            var remoteIpAddress = context.Connection.RemoteIpAddress.ToString();
            return remoteIpAddress;
        }
        public static string GetClientXforwardIP(HttpContext context)
        {
            var xForwardIP = context.Request.Headers["X-Forwarded-For"].ToString();
            return xForwardIP;
        }
        public static bool IsLocalIPRange(string ipAddress)
        {
            int[] ipParts = ipAddress.Split(new String[] { "." }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => int.Parse(s)).ToArray();
            if (ipParts[0] == 10 ||
                (ipParts[0] == 192 && ipParts[1] == 168) ||
                (ipParts[0] == 172 && (ipParts[1] >= 16 && ipParts[1] <= 31)))
            {
                return true;
            }

            return false;
        }
        public static bool IsWebCrawler(HttpContext context)
        {
            bool crawlerCheck = Regex.IsMatch(context.Request.Headers["User-Agent"].ToString(), @"bot|crawler|baiduspider|80legs|ia_archiver|voyager|curl|wget|yahoo! slurp|mediapartners-google", RegexOptions.IgnoreCase);

            return false;
        }
        public static string GetClientLanguage(HttpContext context)
        {
            var clientLanguage = context.Request.Headers["Accept-Language"].ToString().Split(";").FirstOrDefault()?.Split(",").FirstOrDefault();

            if(clientLanguage != null)
            {
                return clientLanguage.Substring(0,2);
            }
            else
            {
                return "invalid";
            }
            
        }
    }
}
