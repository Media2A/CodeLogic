using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        // Url
        public static string GetPath(HttpContext context)
        {
            var Domain = context.Request.Path.Value.ToString();
            return Domain;
        }
        public static string GetDisplayUrl(HttpContext context)
        {
            var QueryString = context.Request.GetDisplayUrl();
            return QueryString;
        }
        public static string GetFullEncodedUrl(HttpContext context)
        {
            var QueryString = context.Request.GetEncodedUrl();
            return QueryString;
        }
        public static string GetRawUrl(HttpContext httpContext)
        {
            return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}{httpContext.Request.QueryString}";
        }

        // Querystring

        public static string GetFullQueryString(HttpContext context)
        {
            var QueryString = context.Request.QueryString.ToString();
            return QueryString;
        }

        // Url path split


        public static string SplitUrlString(string rawUrl, int intSplitID)
        {
            int index = rawUrl.IndexOf("?");
            if (index > 0)
            {
                rawUrl = rawUrl.Substring(0, index);
            }

            if (rawUrl.StartsWith("/"))
            {
                string[] strUrlSplit = rawUrl.Split('/');
                int splitIndex = strUrlSplit.Length - 1;

                if (splitIndex >= intSplitID)
                {
                    return strUrlSplit[intSplitID];
                }
                else
                {
                    return "";
                }
            }
            return "root";
        }

    }
}
