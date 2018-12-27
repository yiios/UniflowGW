using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using UniFlowGW.Models;
using UniFlowGW.ViewModels;
using UniFlowGW.Util;
using System.Web;
using Microsoft.Extensions.Logging;
using UniFlowGW.Services;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace UniFlowGW.Controllers
{
	public class HomeController : Controller
	{
		readonly DatabaseContext _ctx;
		readonly ILogger<HomeController> _logger;
		readonly IBackgroundTaskQueue queue;
		UniflowController _uniflow;

		public HomeController(IConfiguration configuration,
			DatabaseContext ctx,
			IBackgroundTaskQueue queue,
			ILogger<HomeController> logger, UniflowController uniflow)
		{
			Configuration = configuration;
			_ctx = ctx;
			this.queue = queue;
			_logger = logger;
			_uniflow = uniflow;
		}
		public IConfiguration Configuration { get; }

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult About()
		{
			ViewData["Message"] = "Your application description page.";

			return View();
		}

		public IActionResult Contact()
		{
			ViewData["Message"] = "Your contact page.";

			return View();
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[Route("Input")]
		public IActionResult Input()
		{
			return View();
		}

		[Route("Test")]
		public IActionResult Test()
		{
			_logger.LogInformation("test");
			var isValid = LDAPUtil.ValidateUser(Configuration["LDAPDomain"], "gaohongxing", "cib@123");

			string wxTokenJson = "{ \"access_token\":\"15_JzT2YREAlsa7JqvgFSXGUZYNHXAPSbVFENbGgWkwpOP_wTGYa7it7y5Mh95nHVUZjQB - A7VtAU2UE6YMageTOw5zxF7_evsw - pEyiA7dcpo\",\"expires_in\":7200,\"refresh_token\":\"15_5rqyI5DCrduNB2t2stCLNXjCJavV0omoR--rlJi9KoMT3_QIggIgyIQX4QhV2vqcD7_EA_zhbsC7CQidcF5FYdj1RlrGK-dh41QRst2ST14\",\"openid\":\"otprL0SNKPkZlwS8BrPfEuK6eZ-Q\",\"scope\":\"snsapi_base\"}";
			var wxToken = JsonHelper.DeserializeJsonToObject<AccessTokenModel>(wxTokenJson);

			var tokenJson = "{\"errcode\":0,\"errmsg\":\"\",\"access_token\": \"accesstoken000001\",\"expires_in\": 7200 }";
			var token = JsonHelper.DeserializeJsonToObject<AccessTokenModel>(tokenJson);
			var cropJson = "{ \"UserId\":\"USERID\",\"DeviceId\":\"DEVICEID\", \"errcode\": 0,\"errmsg\": \"ok\"}";
			var crop = JsonHelper.DeserializeJsonToObject<CorpModel>(cropJson);

			var signJson = "{ \"code\":\"0\",\"msg\":\"success\", \"data\": {\"lxaccount\": \"guo\",\"version\":\"1.0\"}}";
			var sign = JsonHelper.DeserializeJsonToObject<LxSignModel>(signJson);
			return View();
		}


		[Route("OAuth")]
		public IActionResult OAuth()
		{
			string oAuthURL = string.Format("{0}?appid={1}&redirect_uri={2}&response_type=code&scope=snsapi_base&agentid={3}&state=#wechat_redirect",
				Constant.OAuthURL, Configuration["QywxAppId"], Configuration["QywxCallBackURL"], Configuration["AgentId"]);
			_logger.LogInformation("Oauth-URL:" + oAuthURL);
			return Redirect(oAuthURL);
		}

		[Route("QywxCallback")]
		public IActionResult QywxCallback(string code)
		{
			string getAccessTokenURL = string.Format("{0}?corpid={1}&corpsecret={2}", Constant.TokenURL, Configuration["QywxAppId"], Configuration["QywxAppSecret"]);
			_logger.LogInformation("TokenURL:" + getAccessTokenURL);
			var resGetToken = RequestUtil.HttpGet(getAccessTokenURL);
			_logger.LogInformation("TokenJson:" + resGetToken);
			string accessToken = JsonHelper.DeserializeJsonToObject<AccessTokenModel>(resGetToken).access_token;
			string userinfoURL = string.Format("{0}?access_token={1}&code={2}", Constant.UserinfoURL, accessToken, code);
			_logger.LogInformation("UserInfoURL:" + userinfoURL);
			var resUserInfo = RequestUtil.HttpGet(userinfoURL);
			_logger.LogInformation("UserInfoJson:" + resUserInfo);
			var corpModel = JsonHelper.DeserializeJsonToObject<CorpModel>(resUserInfo);
			_logger.LogInformation(string.Format("Corp:{0}-{1}", corpModel.UserId, corpModel.errmsg));
			string userId = "guest";
			if (!string.IsNullOrEmpty(corpModel.UserId))
			{
				userId = corpModel.UserId;
			}
			return View("Upload", new UploadViewModel { UserID = userId });
		}


		[Route("wxOAuth")]
		public IActionResult wxOAuth()
		{
			string oAuthURL = string.Format("{0}?appid={1}&redirect_uri={2}&response_type=code&scope=snsapi_base&state=#wechat_redirect",
				Constant.OAuthURL, Configuration["WxAppId"], Configuration["WxCallBackURL"]);
			_logger.LogInformation("Oauth-URL:" + oAuthURL);
			return Redirect(oAuthURL);
		}


		[Route("WxCallback")]
		public IActionResult WxCallback(string code)
		{
			string userId = "guest";
			try
			{
				string getAccessTokenURL = string.Format(Constant.WxAccessTokenURL, Configuration["WxAppId"], Configuration["WxAppSecret"], code);
				_logger.LogInformation("TokenURL:" + getAccessTokenURL);
				var resGetToken = RequestUtil.HttpGet(getAccessTokenURL);
				_logger.LogInformation("TokenJson:" + resGetToken);
				var tokenModel = JsonHelper.DeserializeJsonToObject<AccessTokenModel>(resGetToken);
				string accessToken = tokenModel.access_token;
				string openId = tokenModel.openid;
				_logger.LogInformation(string.Format("OpenId:{0}", openId));
				var checkResult = _uniflow.CheckBind(
				new ExternalIdRequest { ExternalId = openId, Type = "WeChatOpenID" });
				_logger.LogInformation("[WxCallBack] CheckBind Code:" + checkResult.Value.Code);
				if (checkResult.Value.Code == "0")
				{
					var bindId = checkResult.Value.BindId;
					_logger.LogInformation("[WxCallBack] CheckBind bindId:" + bindId);
					HttpContext.Session.SetString("WX_BindId", bindId);
					userId = _ctx.BindUsers.Where(b => b.BindUserId == bindId).FirstOrDefault().UserLogin;
				}
				else
				{
					return View("Bind", new UserViewModel { openId = openId });

				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message, ex);
			}

			return View("Upload", new UploadViewModel { UserID = userId });
		}

		[HttpGet("Upload")]
		public IActionResult Upload()
		{
			var uid = Request.Query["u"];
			return View(new UploadViewModel() { UserID = uid });
		}

		[HttpGet("lxupload")]
		public IActionResult LXUpload(string sign)
		{
			string account = "guest";
			try
			{
				string validSignURL = string.Format(Configuration["LxValidSignURL"], HttpUtility.UrlEncode(sign));
				var headers = new Dictionary<string, string>()
				{
					["X-LONGCHAT-AppKey"] = Configuration["LxAppKey"],
				};
				var accountJson = RequestUtil.HttpGet(validSignURL, headers);
				_logger.LogInformation("AccountJson:" + accountJson);
				var signModel = JsonHelper.DeserializeJsonToObject<LxSignModel>(accountJson);

				if (signModel != null && signModel.data != null)
				{
					account = signModel.data.lxAccount;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
			}
			return View("Upload", new UploadViewModel { UserID = account });
		}

		[HttpGet("lxupload")]
		public IActionResult LXUpload(string filepath, string sign)
		{
			string account = "guest";
			try
			{
				string validSignURL = string.Format(Configuration["LxValidSignURL"], HttpUtility.UrlEncode(sign));
				var headers = new Dictionary<string, string>()
				{
					["X-LONGCHAT-AppKey"] = Configuration["LxAppKey"],
				};
				var accountJson = RequestUtil.HttpGet(validSignURL, headers);
				_logger.LogInformation("AccountJson:" + accountJson);
				var signModel = JsonHelper.DeserializeJsonToObject<LxSignModel>(accountJson);

				if (signModel != null && signModel.data != null)
				{
					account = signModel.data.lxAccount;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
			}
			return View("Upload", new UploadViewModel { UserID = account });
		}

		[HttpPost("Upload")]
		public IActionResult Upload(UploadViewModel upload)
		{
			if (upload.Document == null)
				return View("Error", new ErrorViewModel { Message = "Document not provided." });
			string uid = upload.UserID;
			if (string.IsNullOrWhiteSpace(uid))
				return View("Error", new ErrorViewModel { Message = "User ID not provided." });

			var filename = upload.Document.FileName;
			var ext = Path.GetExtension(filename);
			var temp = Path.GetTempFileName() + ext;
			_logger.LogInformation("Upload File Path:" + temp);
			using (var outstream = System.IO.File.OpenWrite(temp))
				upload.Document.CopyTo(outstream);

			TempData["Path"] = temp;
			TempData["UserID"] = upload.UserID;
			TempData["Document"] = Path.GetFileName(filename);

			return RedirectToAction("Print");
		}



		[Route("Print")]
		public IActionResult Print()
		{
			var allowed = (Configuration["ConvertibleFileTypes"] + ";" +
				Configuration["ImageFileTypes"] + ";" +
				Configuration["DirectHandledFileTypes"]).ToLower().Split(';');

			string path = TempData["Path"]?.ToString() ?? Request.Query["p"];
			if (string.IsNullOrWhiteSpace(path))
				return View("Error", new ErrorViewModel { Message = "Document not provided." });
			string document = TempData["Document"]?.ToString() ?? path;
			string uid = TempData["UserID"]?.ToString() ?? Request.Query["u"];
			if (string.IsNullOrWhiteSpace(uid))
				return View("Error", new ErrorViewModel { Message = "User ID not provided." });
			if (!allowed.Any(ext => document.ToLower().EndsWith(ext)))
				return View("Error", new ErrorViewModel { Message = "Document type not supported." });
			return View(new PrintViewModel() { Path = path, Document = document, UserID = uid });
		}


		[HttpPost]
		[Route("Result")]
		public IActionResult Result(PrintViewModel model)
		{
			string path = model.Path;
			if (string.IsNullOrWhiteSpace(path))
				return View("Error", new ErrorViewModel { Message = "Document not provided." });
			string document = model.Document;
			if (string.IsNullOrWhiteSpace(document))
				document = path;
			string uid = model.UserID;
			if (string.IsNullOrWhiteSpace(uid))
				return View("Error", new ErrorViewModel { Message = "User ID not provided." });

			var convertExts = Configuration["ConvertibleFileTypes"].ToLower().Split(';');
			var imageExts = Configuration["ImageFileTypes"].ToLower().Split(';');
			var directExts = Configuration["DirectHandledFileTypes"].ToLower().Split(';');

			var temp = Path.GetTempFileName();
			var tempdoc = temp;

			var dext = directExts.FirstOrDefault(ext => document.ToLower().EndsWith(ext));
			var cvtext = convertExts.FirstOrDefault(ext => document.ToLower().EndsWith(ext));
			var imgext = imageExts.FirstOrDefault(ext => document.ToLower().EndsWith(ext));
			var template = Template.tickettempPdf;

			var task = new PrintTask
			{
				PrintModel = model,
				Document = document,
				Status = PrintTaskStatus.Processing,
				Time = DateTime.Now,
				UserID = model.UserID,
			};

			try
			{
				if (dext != null)
				{
					tempdoc += dext;
					System.IO.File.Copy(path, tempdoc);
				}
				else if (cvtext != null)
				{
					//tempdoc += ".pdf";
					//if (!RunConvert(path, tempdoc) || !System.IO.File.Exists(tempdoc))
					//	return View("Error", new ErrorViewModel { Message = "Failed to convert document." });
					task.QueuedTask = true;
				}
				else if (imgext != null)
				{
					tempdoc += ".jpg";
					if ((imgext == ".jpg" || imgext == ".jpeg") && model.ColorMode == ColorMode.Color)
						System.IO.File.Copy(path, tempdoc);
					else if (!RunImageConvert(path, tempdoc, model.ColorMode == ColorMode.BW) ||
						!System.IO.File.Exists(tempdoc))
					{
						//return View("Error", new ErrorViewModel { Message = "Failed to convert document." });
						task.Status = PrintTaskStatus.Failed;
						task.Message = "Failed to convert document.";
					}
					template = Template.tickettempImage;
				}
				else
				{
					//return View("Error", new ErrorViewModel { Message = "Document type not supported." });
					task.Status = PrintTaskStatus.Failed;
					task.Message = "Document type not supported.";
				}

				if (task.Status == PrintTaskStatus.Failed)
					return View("Error", new ErrorViewModel { Message = task.Message });

				if (!task.QueuedTask)
				{
					MoveToOutput(model, uid, tempdoc, template);
					task.Status = PrintTaskStatus.Committed;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
				task.Status = PrintTaskStatus.Failed;
				task.Message = "Internal error";
				throw;
			}
			finally
			{
				_ctx.PrintTasks.Add(task);
				_ctx.SaveChanges();
			}

			if (task.QueuedTask) // wake up the queue
				queue.QueueBackgroundWorkItem(RunConvertInQueue);

			return View(model);
			//return View("Upload", new UploadViewModel { UserID = model.UserID });
		}

		private void MoveToOutput(PrintViewModel model, string uid, string tempdoc, string template)
		{
			Guid guid = Guid.NewGuid();
			var outdoc = guid.ToString() + Path.GetExtension(tempdoc);
			var ticket = template
				.Replace("$USERID$", uid)
				.Replace("$PATH$", outdoc)
				.Replace("$FILENAME$", Path.GetFileName(model.Document))
				.Replace("$COPIES$", model.Copies.ToString())
				.Replace("$COLORMODE$", model.ColorMode.ToString())
				.Replace("$PAPERSIZE$", ((int)model.PaperSize).ToString())
				.Replace("$DUPLEX$", model.PaperMode.ToString());
			var tempxml = tempdoc + ".xml";
			System.IO.File.WriteAllText(tempxml, ticket);

			var targetPaths = Configuration["TaskTargetPath"];
			foreach (var targetPath in targetPaths.Split(';'))
			{
				var targetdoc = Path.Combine(targetPath.Trim(), outdoc);
				var targetxml = Path.Combine(targetPath.Trim(), guid.ToString() + ".xml");
				System.IO.File.Copy(tempdoc, targetdoc);
				System.IO.File.Copy(tempxml, targetxml);
			}

			System.IO.File.Delete(tempdoc);
			System.IO.File.Delete(tempxml);
		}

		private Task RunConvertInQueue(DatabaseContext ctx, CancellationToken token)
		{
			var tasks = from task in ctx.PrintTasks
						where task.Status == PrintTaskStatus.Processing && task.QueuedTask
						orderby task.Time ascending
						select task;
			foreach (var task in tasks)
			{
				var temp = Path.GetTempFileName();
				var tempdoc = temp + ".pdf";
				try
				{
					if (!RunConvert(task.PrintModel.Path, tempdoc) || !System.IO.File.Exists(tempdoc))
					{
						task.Status = PrintTaskStatus.Failed;
						task.Message = "Failed to convert document.";
					}
					else
					{
						MoveToOutput(task.PrintModel, task.UserID, tempdoc, Template.tickettempPdf);
						task.Status = PrintTaskStatus.Committed;
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex.Message);
					task.Status = PrintTaskStatus.Failed;
					task.Message = "Internal error";
					//throw;
				}
				finally
				{
					System.IO.File.Delete(temp);
					ctx.SaveChanges();
				}
			}
			return Task.CompletedTask;
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}

		bool RunConvert(string source, string target)
		{
			var processInfo = new ProcessStartInfo
			{
				FileName = Configuration["PdfConverter"],
				Arguments = $"\"{source}\" \"{target}\"",
				CreateNoWindow = false,
			};

			try
			{
				using (var process = Process.Start(processInfo))
				{
					process.WaitForExit();
					var result = process.ExitCode;
					return result == 0;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message, ex);
				return false;
			}
		}

		bool RunImageConvert(string source, string target, bool grayscale)
		{
			try
			{
				var processInfo = new ProcessStartInfo
				{
					FileName = Configuration["ImageConverter"],
					Arguments = $"\"{source}\" \"{target}\" jpg {(grayscale ? "grayscale" : "color")}",
					CreateNoWindow = false,
				};


				using (var process = Process.Start(processInfo))
				{
					process.WaitForExit();
					var result = process.ExitCode;
					return result == 0;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message, ex);
				return false;
			}
		}

		[HttpGet("bind")]
		public IActionResult Bind()
		{
			return View();
		}

		[HttpPost("bind")]
		public IActionResult Bind(UserViewModel model)
		{
			var req = new LoginPasswordRequest() { Login = model.userName, Password = model.password };
			var checkResult = _uniflow.CheckUser(req);
			if (checkResult.Result.Value.Code != "0")
			{
				ModelState.AddModelError("errorMsg", "用户名或密码错误，绑定失败!");
				return View();
			}

			var bindId = checkResult.Result.Value.BindId;
			HttpContext.Session.SetString("BindId", bindId);

			var bindResult = _uniflow.Bind(
				new BindExternalIdRequest
				{
					ExternalId = model.openId,
					Type = "WeChatOpenID",
					BindId = bindId,
				});

			if (bindResult.Result.Value.Code == "0")
			{
				return RedirectToAction("wxOAuth", "Home");
			}
			ModelState.AddModelError("errorMsg", bindResult.Result.Value.Message);
			return View();

		}

		[HttpPost("unBind")]
		public IActionResult UnBind()
		{
			string openId = HttpContext.Session.GetString("openId");
			var findUser = _ctx.ExternBindings.Where(s => s.ExternalId == openId).SingleOrDefault<ExternBinding>();
			if (null != findUser)
			{
				findUser.BindUserId = openId;
				findUser.BindTime = DateTime.Now;
				_ctx.ExternBindings.Remove(findUser);
				_logger.LogInformation(string.Format("Remove WechatUser:{0}-{1}", findUser.BindUserId, findUser.ExternalId));
			}
			_ctx.SaveChanges();
			return RedirectToAction("wxOAuth", "Home");
		}

		[HttpGet("unlock")]
		public IActionResult UnLock()
		{
			var unlockURL = Configuration["Test.Unlock.URL"];

			RequestUtil.HttpGet(unlockURL);

			return View();
		}



	}
}
