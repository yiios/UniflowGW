using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UniFlowGW.Models;
using UniFlowGW.Util;
using UniFlowGW.ViewModels;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace UniFlowGW.Controllers
{
	public class UserController : Controller
	{

		readonly DatabaseContext _ctx;
		readonly ILogger<UserController> _logger;
		public IConfiguration Configuration { get; }

		public UserController(IConfiguration configuration,
			DatabaseContext ctx,
			ILogger<UserController> logger)
		{
			Configuration = configuration;
			_ctx = ctx;
			_logger = logger;
		}

		[Route("Test1")]
		public IActionResult Test()
		{
			_logger.LogInformation("test");
			var sn = "XTR03183";
			var timestamp = Math.Floor((double)DateTime.Now.Ticks / 1000).ToString();
			var nonce = "123456";
			var secretNo = "101d7a8c8d6db9842a493b40642107a4";
			var type = "register";
			var paramArray = new List<string> { sn, secretNo, timestamp, nonce, type };
			paramArray.Sort();

			//var sign = sha1(paramsStr);



			return View();
		}

	}
}
