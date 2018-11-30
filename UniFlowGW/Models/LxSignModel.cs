using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UniFlowGW.Models
{
	public class LxSignModel
	{
		public string code { get; set; }
		public string msg { get; set; }
		public Data data { get; set; }
		public string schema { get; set; }

	}

	public class Data
	{
		public string lxAccount { get; set; }
		public string times { get; set; }
	}
}
