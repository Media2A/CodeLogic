using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        // Url
        public static string GetSession(HttpContext context, string SessionKey)
        {
                var SessionContent = context.Session.GetString(SessionKey);
                return SessionContent;

        }
        public static void SetSession(HttpContext context, string SessionKey, string SessionValue)
        {
            context.Session.SetString(SessionKey, SessionValue);
        }
        public static string GetSessionID(HttpContext context)
        {
            return context.Session.Id.ToString();
        }
    }
}
