using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace UniFlowGW.Util
{
	public class RequestUtil
	{

		public static async Task<string> HttpGetAsync(string url)
		{

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync();
			Stream resStream = response.GetResponseStream();
			return await new StreamReader(resStream).ReadToEndAsync();
		}

        public static string HttpGet(string url)
        {

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream resStream = response.GetResponseStream();
            return new StreamReader(resStream).ReadToEnd();
        }

        public static async Task<string> HttpGetAsync(string url, Dictionary<string, string> headers)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
            HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync();
            Stream resStream = response.GetResponseStream();
            return await new StreamReader(resStream).ReadToEndAsync();
        }

        public static string HttpGet(string url, Dictionary<string, string> headers)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			foreach (var header in headers)
			{
				request.Headers.Add(header.Key, header.Value);
			}
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Stream resStream = response.GetResponseStream();
			return new StreamReader(resStream).ReadToEnd();
		}
	}
}
