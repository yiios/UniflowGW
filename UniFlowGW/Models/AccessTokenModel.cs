﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UniFlowGW.Models
{
	public class AccessTokenModel
	{

		public int errcode { get; set; }
		public string errmsg { get; set; }
		public string access_token { get; set; }
		public string refresh_token { get; set; }
		public int expires_in { get; set; }
		public string openid { get; set; }
		public string scope { get; set; }


	}
}
