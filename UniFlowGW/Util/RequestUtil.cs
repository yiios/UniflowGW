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

		public static String HttpGet(string url)
		{

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Stream resStream = response.GetResponseStream();
			return new StreamReader(resStream).ReadToEnd();
		}

		public static String HttpGet(string url, Dictionary<string, string> headers)
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
