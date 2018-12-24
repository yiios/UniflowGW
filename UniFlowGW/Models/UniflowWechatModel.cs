using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace UniFlowGW.Models
{
	[DataContract]
	public class UniflowWechatModel
	{

		[DataMember]
		public string UserRef
		{
			get;
			set;
		}

		[DataMember]
		public string Request
		{
			get;
			set;
		}

		[DataMember]
		public DateTime RequestTime
		{
			get;
			set;
		}

		[DataMember]
		public int Status
		{
			get;
			set;
		}

		[DataMember]
		public string Desc
		{
			get;
			set;
		}

		[DataMember]
		public int ErrorCode
		{
			get;
			set;
		}

		[DataMember]
		public string ErrorDesc
		{
			get;
			set;
		}

		public UniflowWechatModel(string sRequestInfo)
		{
			UserRef = "";
			Request = sRequestInfo;
			RequestTime = DateTime.UtcNow;
			Status = 0;
			Desc = "";
			ErrorCode = 0;
			ErrorDesc = "";
		}
	}
}

