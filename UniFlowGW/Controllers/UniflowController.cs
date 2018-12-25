using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UniFlowGW.Models;
using UniFlowGW.Util;

namespace UniFlowGW.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UniflowController : ControllerBase
	{
		readonly DatabaseContext _ctx;
		readonly ILogger<UniflowController> _logger;
		IConfiguration Configuration { get; }

		public UniflowController(IConfiguration configuration,
			DatabaseContext ctx,
			ILogger<UniflowController> logger)
		{
			Configuration = configuration;
			_ctx = ctx;
			_logger = logger;
		}

        [HttpGet]
		public async Task<ActionResult<BindStatusResponse>> CheckUser(LoginPasswordRequest req)
		{
            if (!ModelState.IsValid)
                return new BindStatusResponse
                {
                    Code = ""
                };

            string baseurl = Configuration["UniflowService:Url"];
            string key = Configuration["UniflowService:EncryptKey"];
            string login = EncrpyUntil.Encrypt(req.Login, key);
            string password = EncrpyUntil.Encrypt(req.Password, key);

            string url = $"{baseurl}/WECHAT/CHECKUSER/{login}/{password}";
            _logger.LogTrace("Get " + url);
            var result = await RequestUtil.HttpGetAsync(url);
            _logger.LogTrace("Response: " + result);

            var xdoc = XElement.Parse(result);
            var status = xdoc.Element("Status").Value;
            var bindId = xdoc.Element("UserRef").Value;
            var response = new BindStatusResponse
            {
                Code = xdoc.Element("ErrorCode").Value,
                Message = xdoc.Element("ErrorDesc").Value,
                Status = status,
                BindId = bindId,
            };

            //if (status != "0")
            //    return response;

            //var bind = _ctx.BindUsers.Find(bindId);
            //if (bind == null)
            //{
            //    _ctx.BindUsers.Add(new BindUser
            //    {
            //        BindUserId = bindId,
            //        UserLogin = req.Login,
            //        BindTime = DateTime.Now,
            //    });
            //    await _ctx.SaveChangesAsync();
            //}
            //else if (bind.UserLogin != req.Login)
            //{
            //    _logger.LogWarning("Login changed: " + req.Login + " " + JsonHelper.SerializeObject(bind));
            //    bind.UserLogin = req.Login;
            //    await _ctx.SaveChangesAsync();
            //}
            return response;
        }

        [HttpGet]
        public ActionResult<BindStatusResponse> CheckBind(ExternalIdRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!Enum.TryParse<ExternAccountType>(req.Type, out var type))
                return BadRequest("Type not supported: " + req.Type);

            var bind = _ctx.ExternBindings
                .Where(b => b.Type == type && b.ExternalId == req.ExternalId)
                .FirstOrDefault();
            return new BindStatusResponse
            {
                Code = "0",
                Message = "",
                BindId = bind?.BindUserId ?? "",
                Status = bind == null ? "1" : "0",
            };
        }

        [HttpGet]
        public async Task<ActionResult<BindStatusResponse>> Bind(BindExternalIdRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string baseurl = Configuration["UniflowService:Url"];
            string key = Configuration["UniflowService:EncryptKey"];
            string login = EncrpyUntil.Encrypt(req.Login, key);
            string password = EncrpyUntil.Encrypt(req.Password, key);

            string url = $"{baseurl}/WECHAT/CHECKUSER/{login}/{password}";
            _logger.LogTrace("Get " + url);
            var result = await RequestUtil.HttpGetAsync(url);
            _logger.LogTrace("Response: " + result);

            var xdoc = XElement.Parse(result);
            var status = xdoc.Element("Status").Value;
            var bindId = xdoc.Element("UserRef").Value;
            var response = new BindStatusResponse
            {
                Code = xdoc.Element("ErrorCode").Value,
                Message = xdoc.Element("ErrorDesc").Value,
                Status = status,
                BindId = bindId,
            };

            //if (status != "0")
            //    return response;

            //var bind = _ctx.BindUsers.Find(bindId);
            //if (bind == null)
            //{
            //    _ctx.BindUsers.Add(new BindUser
            //    {
            //        BindUserId = bindId,
            //        UserLogin = req.Login,
            //        BindTime = DateTime.Now,
            //    });
            //    await _ctx.SaveChangesAsync();
            //}
            //else if (bind.UserLogin != req.Login)
            //{
            //    _logger.LogWarning("Login changed: " + req.Login + " " + JsonHelper.SerializeObject(bind));
            //    bind.UserLogin = req.Login;
            //    await _ctx.SaveChangesAsync();
            //}
            return response;
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

    public class LoginPasswordRequest
    {
        [Required]
        public string Login { get; set; }
        [Required]
        public string Password { get; set; }
    }

    public class ExternalIdRequest
    {
        [Required]
        public string ExternalId { get; set; }
        [Required]
        public string Type { get; set; }
    }

    public class BindExternalIdRequest
    {
        [Required]
        public string BindId { get; set; }
        [Required]
        public string ExternalId { get; set; }
        [Required]
        public string Type { get; set; }
    }

    public class StatusResponse : BaseResponseBody
    {
        public string Status { get; set; }
    }
    public class BindStatusResponse : BaseResponseBody
    {
        public string Status { get; set; }
        public string BindId { get; set; }
    }
}