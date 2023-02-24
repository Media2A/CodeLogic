using System;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        public string SimpleWebRequest(string url)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            WebHeaderCollection header = response.Headers;

            var encoding = ASCIIEncoding.ASCII;
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
                return reader.ReadToEnd();
            }

        }
    }
}

