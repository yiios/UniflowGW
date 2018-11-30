using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UniFlowGW.Models
{
	public class CorpModel
	{

		public int errcode { get; set; }
		public string errmsg { get; set; }
		public string CorpId { get; set; }
		public string UserId { get; set; }
		public string DeviceId { get; set; }
		public string user_ticket { get; set; }
		public int expires_in { get; set; }
		public string OpenId { get; set; }

	}
}
