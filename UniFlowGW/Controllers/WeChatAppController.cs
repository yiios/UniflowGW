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
using Newtonsoft.Json;
using UniFlowGW.Models;
using UniFlowGW.Util;

namespace UniFlowGW.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class WeChatAppController : ControllerBase
	{
		readonly DatabaseContext _ctx;
		readonly ILogger<WeChatAppController> _logger;
		UniflowController _uniflow;

		public WeChatAppController(IConfiguration configuration,
			DatabaseContext ctx,
			ILogger<WeChatAppController> logger,
			UniflowController uniflow)
		{
			Configuration = configuration;
			_ctx = ctx;
			_logger = logger;
			_uniflow = uniflow;
		}
		public IConfiguration Configuration { get; }

		[HttpGet("login")]
		public async Task<ActionResult<StatusResponse>> Login(string code)
		{
			var url = string.Format(
				Configuration["WxApp:UrlPattern"],
				Configuration["WxApp:AppId"],
				Configuration["WxApp:Secret"],
				code
				);

			var res = await RequestUtil.HttpGetAsync(url.ToString());
			var response = JsonConvert.DeserializeAnonymousType(res, new
			{
				errcode = 0,
				errmsg = "",
				openid = "",
			});
			//dynamic response = JsonConvert.DeserializeObject(res);
			if (response.errcode != 0)
			{
				return new StatusResponse
				{
					Code = Error.Codes.ExternalError.AsString(),
					Message = Error.Codes.ExternalError.AsMessage(
						response.errcode, response.errmsg),
				};
			}

			var openId = response.openid as string;
			HttpContext.Session.SetExternId(openId, "WeChatAppOpenID");
			_logger.LogInformation(string.Format("[WeChatAppController] [Login] OpenId:{0}", openId));
			var checkResult = _uniflow.CheckBind(
				new ExternalIdRequest { ExternalId = openId, Type = "WeChatAppOpenID" });
			_logger.LogInformation(string.Format("[WeChatAppController] [Login] [CheckBind] Code:{0}", checkResult.Value.Code));
			if (checkResult.Value.Code == "0")
			{
				var bindId = checkResult.Value.BindId;
				HttpContext.Session.SetBindId(bindId);
				HttpContext.Session.SetLdapLoginId(checkResult.Value.LdapLoginId);
			}

			return new StatusResponse
			{
				Code = checkResult.Value.Code,
				Message = checkResult.Value.Message,
			};
		}

		[HttpPost("bind")]
		public async Task<ActionResult<StatusResponse>> Bind(LoginPasswordRequest req)
		{
			var (openId, type) = HttpContext.Session.GetExternId();
			_logger.LogInformation(string.Format("[WeChatAppController] [Bind] OpenId:{0},Type:{1}", openId, type));
			if (string.IsNullOrEmpty(openId))
			{
				return new StatusResponse
				{
					Code = Error.Codes.LoginRequired.AsString(),
					Message = Error.Codes.LoginRequired.AsMessage(),
				};
			}

			var checkResult = await _uniflow.CheckUser(req);
			_logger.LogInformation(string.Format("[WeChatAppController] [Bind] [CheckUser] Code:{0}", checkResult.Value.Code));
			if (checkResult.Value.Code != "0")
			{
				return new StatusResponse
				{
					Code = checkResult.Value.Code,
					Message = checkResult.Value.Message,
				};
			}

			var bindId = checkResult.Value.BindId;
			HttpContext.Session.SetBindId(bindId);

			var bindResult = await _uniflow.Bind(
				new BindExternalIdRequest
				{
					ExternalId = openId,
					Type = "WeChatAppOpenID",
					BindId = bindId,
				});

			return new StatusResponse
			{
				Code = bindResult.Value.Code,
				Message = bindResult.Value.Message,
			};
		}

		[HttpGet("unlock")]
		public async Task<ActionResult<UnlockResponse>> Unlock(string data)
		{
			var (openId, type) = HttpContext.Session.GetExternId();
			_logger.LogInformation(string.Format("[WeChatAppController] [Unlock] OpenId:{0},Type:{1}", openId, type));
			if (string.IsNullOrEmpty(openId))
			{
				return new UnlockResponse
				{
					Code = Error.Codes.LoginRequired.AsString(),
					Message = Error.Codes.LoginRequired.AsMessage(),
				};
			}

			var bindId = HttpContext.Session.GetBindId();
			_logger.LogInformation(string.Format("[WeChatAppController] [Unlock] bindId:{0}", bindId));
			if (string.IsNullOrEmpty(bindId))
			{
				var checkResult = _uniflow.CheckBind(
					new ExternalIdRequest { ExternalId = openId, Type = "WeChatAppOpenID" });
				_logger.LogInformation(string.Format("[WeChatAppController] [Unlock] [CheckBind] Code:{0}", checkResult.Value.Code));
				if (checkResult.Value.Code == "0")
				{
					bindId = checkResult.Value.BindId;
					HttpContext.Session.SetBindId(bindId);
				}

				return new UnlockResponse
				{
					Code = checkResult.Value.Code,
					Message = checkResult.Value.Message,
				};
			}

			string key = Configuration["UniflowService:EncryptKey"];
			try
			{
				data = EncryptUtil.Decrypt(key, data);
			}
			catch (Exception ex)
			{
				return new UnlockResponse
				{
					Code = Error.Codes.DecryptError.AsString(),
					Message = Error.Codes.DecryptError.AsMessage(ex.Message),
				};
			}
			var parts = data.Split('@');
			if (parts.Length < 4 ||
				!Uri.IsWellFormedUriString(parts[0], UriKind.Absolute) ||
				string.IsNullOrEmpty(parts[1]))
			{
				return new UnlockResponse
				{
					Code = Error.Codes.InvalidData.AsString(),
					Message = Error.Codes.InvalidData.AsMessage(
						"invalid BarcodeData format"),
				};
			}
			var serial = parts[1];

			var result = await _uniflow.Unlock(new UnlockRequest { BindId = bindId, Serial = serial });
			_logger.LogInformation(string.Format("[WeChatAppController] [Unlock] Code:{0}", result.Value.Code));
			if (result.Value.Code == "0")
			{

				return new UnlockResponse
				{
					PrinterName = serial,
					PrinterStatus = "1",
					Code = result.Value.Code,
					Message = result.Value.Message,
				};
			}
			else
			{
				return new UnlockResponse
				{
					Code = result.Value.Code,
					Message = result.Value.Message,
				};
			}
		}
	}


	public class BaseResponseBody
	{
		public string Code { get; set; }
		public string Message { get; set; }
	}


	public class ThirdLoginResponseBody : BaseResponseBody
	{
		public string SessionId { get; set; }
	}



	public class UnlockResponse : StatusResponse
	{
		public string PrinterName { get; set; }
		public string PrinterStatus { get; set; }
	}

}