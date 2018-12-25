using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UniFlowGW.Models;
using UniFlowGW.Util;

namespace UniFlowGW.Controllers
{
	[Route("GWService/")]
	[ApiController]
	public class APIController : ControllerBase
	{
		readonly DatabaseContext _ctx;
		readonly ILogger<APIController> _logger;
		Dictionary<string, string> sessionIdOpenIdMap = new Dictionary<string, string>();


		public APIController(IConfiguration configuration,
			DatabaseContext ctx,
			ILogger<APIController> logger)
		{
			Configuration = configuration;
			_ctx = ctx;
			_logger = logger;
		}
		public IConfiguration Configuration { get; }


		[Route("ThirdLogin")]
		public ThirdLoginResponseBody ThirdLogin(string code)
		{
			StringBuilder url = new StringBuilder();
			url.Append(Configuration["wxapp.sessionHost"]).Append("?appid=").Append(Configuration["wxapp.appId"]).Append("&secret=").Append(Configuration["wxapp.secret"]).Append("&js_code=")
					.Append(code).Append("&grant_type=authorization_code");
			var res = RequestUtil.HttpGet(url.ToString());
			var thirdLoginRes = JsonHelper.DeserializeJsonToObject<WxThirdLoginResponseBody>(res);
			if (thirdLoginRes.Code == "0")
			{
				return new ThirdLoginResponseBody { Code = "100" };
			}
			string sessionId = Guid.NewGuid().ToString();
			sessionIdOpenIdMap.Add(sessionId, thirdLoginRes.openid);
			return new ThirdLoginResponseBody { sessionId = sessionId };
		}


		[Route("SessionCheck")]
		public SessionCheckResponseBody SessionCheck(string sessionId)
		{
			int result = 0;
			if (sessionIdOpenIdMap.ContainsKey(sessionId))
				result = 1;
			return new SessionCheckResponseBody { result = result };
		}



		[Route("Unlock")]
		public UnlockResponseBody Unlock(UnlockRequestBody unlockModel)
		{
			string encryptKey = Configuration["UniflowEncryptKey"];
			string unlockCommand = EncrpyUntil.Decrypt(encryptKey, unlockModel.data);
			//Http://UNIFLOW-SERVER:8080/uniFLOWRESTService/@XTR03183@12182018124231@10.11.226.146
			string[] param = unlockCommand.Split("@");
			var sessionId = unlockModel.sessionId;
			if (!sessionIdOpenIdMap.ContainsKey(sessionId))
			{
				return new UnlockResponseBody { printerName = param[1], Code = "100" };
			}
			string openId = sessionIdOpenIdMap[sessionId];
			string unlockURL = "http://10.11.226.200:8080/uniFLOWRESTService/WECHAT/UNLOCK/e97683c1962e7216784cf92d9QiEIlNGUKaPL5KmxUKGnyUuK-Mtyt86/XTR03183";
			//string unlockURL = string.Format("{0}WECHAT/UNLOCK/{1}/{2}", param[0], openId, param[1]);
			var res = RequestUtil.HttpGet(unlockURL);
			_logger.LogInformation("Unlock Command:" + res);
			return new UnlockResponseBody { printerName = param[1], Code = "0" };
		}

		[Route("BindUser")]
		public BindUserResponseBody BindUser(BindUserRequestBody bindModel)
		{
			string checkUserURL = "http://10.11.226.200:8080/uniFLOWRESTService/WECHAT/CHECKUSER/e97683c1962e7216784cf92dCg9qCn3QIfI/e97683c1962e7216784cf92d-Ku6rqCxOFd9jj73HlXmA24a721bHGsL";
			//string checkUserURL = "";
			var checkUserResult = RequestUtil.HttpGet(checkUserURL);
			string bindUserURL = "http://10.11.226.200:8080/uniFLOWRESTService/WECHAT/BINDUSER/{7B5CFF4A-D398-4F1D-9607-2FC521742514}/e97683c1962e7216784cf92d9QiEIlNGUKbj13yMpL50ruDfIgS6kYla";
			//string bindUserURL = "";
			var bindUserResult = RequestUtil.HttpGet(bindUserURL);
			return new BindUserResponseBody { Code = "0" };

		}

	}


	public class BaseResponseBody
	{
		public string Code { get; set; }
		public string Message { get; set; }
	}

	public class WxThirdLoginResponseBody : BaseResponseBody
	{
		public string openid { get; set; }
		public string session_key { get; set; }

	}

	public class ThirdLoginResponseBody : BaseResponseBody
	{
		public string sessionId { get; set; }
	}

	public class SessionCheckResponseBody : BaseResponseBody
	{
		public int result { get; set; }
	}

	public class UnlockRequestBody
	{
		public string data { get; set; }
		public string sessionId { get; set; }
		public string type { get; set; }

	}


	public class UnlockResponseBody : BaseResponseBody
	{
		public string printerName { get; set; }
		public string printerStatus { get; set; }

	}

	public class BindUserRequestBody
	{
		public string token { get; set; }
		public string type { get; set; }
		public string name { get; set; }
		public string password { get; set; }
	}

	public class BindUserResponseBody : BaseResponseBody
	{

	}
}