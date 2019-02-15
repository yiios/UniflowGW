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
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Net;

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
            var bindId = HttpContext.Session.GetBindId();
            _logger.LogInformation(string.Format("[HomeController] [Index] bindId:{0}", bindId));
            var fileUploadSwitch = Configuration["ModuleSwitch:FileUpload"];
            if (!fileUploadSwitch.Equals("On"))
            {
                return View("Error", new ErrorViewModel { Message = "请扫描打印机二维码。" });
            }
            if (string.IsNullOrEmpty(bindId))
            {
                //目前只有龙信的打印服务不需要绑定LDAP帐号。
                if (bool.TryParse(Configuration["NoLogin"], out bool noLogin) && noLogin)
                    return View("Error", new ErrorViewModel { Message = "会话已过期，请重新进入。" });

                return RedirectToAction("Login", new { backto = WebUtility.UrlEncode(Url.Action()) });
            }
            return View("Print", new PrintViewModel());

        }

        public IActionResult History()
        {
            var bindId = HttpContext.Session.GetBindId();
            _logger.LogInformation(string.Format("[HomeController] [History] bindId:{0}", bindId));
            var fileUploadSwitch = Configuration["ModuleSwitch:FileUpload"];
            if (!fileUploadSwitch.Equals("On"))
            {
                return View("Error", new ErrorViewModel { Message = "请扫描打印机二维码。" });
            }
            if (string.IsNullOrEmpty(bindId))
            {
                if (bool.TryParse(Configuration["NoLogin"], out bool noLogin) && noLogin)
                    return View("Error", new ErrorViewModel { Message = "会话已过期，请重新进入。" });

                return RedirectToAction("Login", new { backto = WebUtility.UrlEncode(Url.Action()) });
            }

            return View();
        }

        [HttpPost]
        public IActionResult Result(PrintViewModel model)
        {
            var bindId = HttpContext.Session.GetBindId();
            _logger.LogInformation(string.Format("[HomeController] [Result] bindId:{0}", bindId));
            if (string.IsNullOrEmpty(bindId))
            {
                if (bool.TryParse(Configuration["NoLogin"], out bool noLogin) && noLogin)
                    return View("Error", new ErrorViewModel { Message = "会话已过期，请重新进入。" });

                return RedirectToAction("Login", new { backto = WebUtility.UrlEncode(Url.Action("Index")) });
            }

            if (!ModelState.IsValid)
                return View("Error", new ErrorViewModel
                {
                    Message = ModelState.First(m => m.Value.Errors.Count > 0).Value.Errors[0].ErrorMessage
                });

            var document = model.Document.FileName;
            var ext = Path.GetExtension(document).ToLower();

            var allowed = (Configuration["ConvertibleFileTypes"] + ";" +
                Configuration["ImageFileTypes"] + ";" +
                Configuration["DirectHandledFileTypes"]).ToLower().Split(';');

            if (!allowed.Contains(ext.ToLower()))
                return View("Error", new ErrorViewModel { Message = "Document type not supported." });

            var uploadPath = Path.GetTempFileName() + ext;
            _logger.LogInformation("Upload File Path:" + uploadPath);
            using (var outstream = System.IO.File.OpenWrite(uploadPath))
                model.Document.CopyTo(outstream);

            var temp = Path.GetTempFileName();
            var tempdoc = uploadPath;

            var convertExts = Configuration["ConvertibleFileTypes"].ToLower().Split(';');
            var imageExts = Configuration["ImageFileTypes"].ToLower().Split(';');
            var directExts = Configuration["DirectHandledFileTypes"].ToLower().Split(';');

            var isDirect = directExts.Contains(ext);
            var isConvert = convertExts.Contains(ext);
            var isImage = imageExts.Contains(ext);
            var template = Template.ticketPdf;

            var loginId = HttpContext.Session.GetLdapLoginId();

            var task = new PrintTask
            {
                PrintModel = new PrintTaskDetail
                {
                    Path = uploadPath,
                    RequestId = model.RequestId,
                    Document = document,
                    Copies = model.Copies,
                    Orientation = model.Orientation,
                    ColorMode = model.ColorMode,
                    PaperMode = model.PaperMode,
                    PaperSize = model.PaperSize,
                },
                Document = document,
                Status = PrintTaskStatus.Processing,
                Time = DateTime.Now,
                UserID = loginId,
            };

            try
            {
                if (isDirect)
                {
                    tempdoc += ext;
                    System.IO.File.Copy(uploadPath, tempdoc);
                }
                else if (isConvert)
                {
                    task.QueuedTask = true;
                }
                else if (isImage)
                {
                    tempdoc += ".jpg";
                    if ((ext == ".jpg" || ext == ".jpeg") && model.ColorMode == ColorMode.Color)
                        System.IO.File.Copy(uploadPath, tempdoc);
                    else if (!RunImageConvert(uploadPath, tempdoc, model.ColorMode == ColorMode.BW) ||
                        !System.IO.File.Exists(tempdoc))
                    {
                        task.Status = PrintTaskStatus.Failed;
                        task.Message = "Failed to convert document.";
                    }
                    template = Template.tickettempImage;
                }
                else
                {
                    task.Status = PrintTaskStatus.Failed;
                    task.Message = "Document type not supported.";
                }

                if (task.Status == PrintTaskStatus.Failed)
                    return View("Error", new ErrorViewModel { Message = task.Message });

                if (!task.QueuedTask)
                {
                    MoveToOutput(task.PrintModel, loginId, tempdoc, template);
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

            return View(task.PrintModel);
        }

        [HttpGet]
        public IActionResult Login(string backto)
        {
            if (bool.TryParse(Configuration["NoLogin"], out bool noLogin) && noLogin)
                return NotFound();

            var ua = Request.Headers["User-Agent"].ToString();
            _logger.LogInformation(string.Format("[HomeController] [Login] userAgent:{0}, backto{1}", ua, backto));

            var enable = bool.TryParse(Configuration["WeChat:Enable"], out var value) && value;
            if (enable && Regex.IsMatch(ua, "MicroMessenger", RegexOptions.IgnoreCase)) // wechat
            {
                var isWxwork = Regex.IsMatch(ua, "wxwork", RegexOptions.IgnoreCase);
                var wxworkOauthUrl = string.Format(
                    Configuration["WeChat:OAuth2UrlPattern"],
                    isWxwork ? Configuration["WeChat:WxWork:AppId"] : Configuration["WeChat:Wx:AppId"],
                    WebUtility.UrlEncode(Url.Action("OAuth2Callback", "Home", new { backto }, Request.Scheme)) // calback
                                                                                                               // state
                    );
                _logger.LogInformation("[HomeController] [Login] Oauth-URL:" + wxworkOauthUrl);
                return Redirect(wxworkOauthUrl);
            }

            return View("Bind");
        }

        [HttpPost]
        public IActionResult Login(UserViewModel model, string backto)
        {
            if (bool.TryParse(Configuration["NoLogin"], out bool noLogin) && noLogin)
                return NotFound();

            var req = new LoginPasswordRequest() { Login = model.userName, Password = model.password };
            var checkResult = _uniflow.CheckUser(req);
            if (checkResult.Result.Value.Code != "0")
            {
                ModelState.AddModelError("errorMsg", "用户名或密码错误!");
                return View("Bind", model);
            }

            var bindId = checkResult.Result.Value.BindId;
            var externId = model.userName;
            var type = "LDAPLogin";
            HttpContext.Session.SetExternId(externId, type);

            var bindResult = _uniflow.Bind(
                new BindExternalIdRequest
                {
                    ExternalId = externId,
                    Type = type,
                    BindId = bindId,
                });
            _logger.LogInformation("[HomeController] [Login] [Bind] Code:" + bindResult.Result.Value.Code);
            if (bindResult.Result.Value.Code != "0")
            {
                ModelState.AddModelError("errorMsg", bindResult.Result.Value.Message);
                return View("Bind", model);
            }

            HttpContext.Session.SetBindId(bindId);
            HttpContext.Session.SetLdapLoginId(model.userName);

            if (!string.IsNullOrEmpty(backto))
                return Redirect(WebUtility.UrlDecode(backto));
            return RedirectToAction("Index");
        }

        public IActionResult QR(string data)
        {
            _logger.LogInformation("[HomeController] [Login] [QR] data:" + data);
            if (!string.IsNullOrEmpty(data))
            {
                string key = Configuration["UniflowService:EncryptKey"];
                try
                {
                    data = EncryptUtil.Decrypt(key, data);
                    _logger.LogInformation("[HomeController] [Login] [QR] Decrypt data:" + data);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Failed to decode qrcode: " + ex.Message);
                    data = null;
                }
                if (data != null)
                {

                    var parts = data.Split('@');
                    if (parts.Length < 4 ||
                        !Uri.IsWellFormedUriString(parts[0], UriKind.Absolute) ||
                        string.IsNullOrEmpty(parts[1]))
                    {
                        _logger.LogInformation("invalid BarcodeData format: " + data);
                    }
                    else
                    {
                        HttpContext.Session.SetCurrentPrinterSN(parts[1]);
                    }
                }
            }
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Unlock(string data)
        {
            _logger.LogInformation(string.Format("[HomeController] [Unlock] data:{0}", data));
            var bindId = HttpContext.Session.GetBindId();
            if (string.IsNullOrEmpty(bindId))
            {
                if (bool.TryParse(Configuration["NoLogin"], out bool noLogin) && noLogin)
                    return View("Error", new ErrorViewModel { Message = "会话已过期，请重新进入。" });

                return RedirectToAction("Login", new { backto = WebUtility.UrlEncode(Url.Action("Unlock", new { data })) });
            }
            if (bindId.IsNoLoginBind())
            {
                return View("Error", new ErrorViewModel { Message = "暂不支持打印机扫码开机。" });
            }

            string key = Configuration["UniflowService:EncryptKey"];

            var printerSN = HttpContext.Session.GetCurrentPrinterSN();
            if (!string.IsNullOrEmpty(data))
            {
                try
                {
                    data = EncryptUtil.Decrypt(key, data);
                }
                catch (Exception)
                {
                    return View("Error", new ErrorViewModel { Message = "二维码数据无效" });
                }
                var parts = data.Split('@');
                if (parts.Length < 4 ||
                    !Uri.IsWellFormedUriString(parts[0], UriKind.Absolute) ||
                    string.IsNullOrEmpty(parts[1]))
                {
                    return View("Error", new ErrorViewModel { Message = "二维码数据无效" });
                }
                printerSN = parts[1];
                HttpContext.Session.SetCurrentPrinterSN(printerSN);
            }

            if (string.IsNullOrEmpty(printerSN))
                return View("Error", new ErrorViewModel { Message = "没有当前打印机，请扫描打印机二维码。" });

            var result = await _uniflow.Unlock(new UnlockRequest { BindId = bindId, Serial = printerSN });
            _logger.LogInformation(string.Format("[HomeController] [Unlock] result:{0},sn:", result.Value.Code, printerSN));
            ViewBag.Result = result.Value.Code == "0";
            return View();
        }

        public IActionResult OAuth2Callback(string code, string backto, string state)
        {
            var ua = Request.Headers["User-Agent"].ToString();

            string externId = "", type = "";
            if (Regex.IsMatch(ua, "MicroMessenger", RegexOptions.IgnoreCase)) // wechat
            {
                var isWxWork = Regex.IsMatch(ua, "wxwork", RegexOptions.IgnoreCase);
                if (isWxWork)
                    (externId, type) = WxWorkCallback(code);
                else
                    (externId, type) = WxCallback(code);
            }
            else
            {
                _logger.LogInformation("Not supported oauth provider.");
                return View("Error", new ErrorViewModel { Message = "Not supported oauth provider." });
            }

            HttpContext.Session.SetExternId(externId, type);

            _logger.LogInformation($"OAuth2 result: {externId} ({type})");
            var checkResult = _uniflow.CheckBind(
                new ExternalIdRequest { ExternalId = externId, Type = type });
            _logger.LogInformation("CheckBind: " + JsonConvert.SerializeObject(checkResult.Value));

            if (checkResult.Value.Code != "0")
            {
                return RedirectToAction("Bind", new { backto });
            }

            var bindId = checkResult.Value.BindId;
            HttpContext.Session.SetBindId(bindId);
            HttpContext.Session.SetLdapLoginId(checkResult.Value.LdapLoginId);

            if (!string.IsNullOrEmpty(backto))
                return Redirect(WebUtility.UrlDecode(backto));
            return RedirectToAction("Index");
        }

        (string externId, string type) WxCallback(string code)
        {
            _logger.LogInformation("[HomeController] [Login] [WxCallback] code:" + code);
            string getAccessTokenURL = string.Format(
                Configuration["WeChat:Wx:GetTokenUrlPattern"],
                Configuration["WeChat:Wx:AppId"],
                Configuration["WeChat:Wx:Secret"],
                code);

            var resGetToken = RequestUtil.HttpGet(getAccessTokenURL);
            var model = JsonHelper.DeserializeJsonToObject<AccessTokenModel>(resGetToken);

            return (model.openid, "WeChatOpenId");
        }

        (string externId, string type) WxWorkCallback(string code)
        {
            _logger.LogInformation("[HomeController] [Login] [WxWorkCallback] code:" + code);
            string getAccessTokenURL = string.Format(
                Configuration["WeChat:WxWork:GetTokenUrlPattern"],
                Configuration["WeChat:WxWork:AppId"],
                Configuration["WeChat:WxWork:Secret"]);

            var resGetToken = RequestUtil.HttpGet(getAccessTokenURL);
            string accessToken = JsonHelper.DeserializeJsonToObject<AccessTokenModel>(resGetToken).access_token;

            string userinfoURL = string.Format(
                Configuration["WeChat:WxWork:GetUserInfoUrlPattern"],
                accessToken,
                code);
            var resUserInfo = RequestUtil.HttpGet(userinfoURL);
            var corpModel = JsonHelper.DeserializeJsonToObject<CorpModel>(resUserInfo);

            string userId = "guest";
            string type = "WxWorkUserID";
            if (!string.IsNullOrEmpty(corpModel.UserId))
            {
                userId = corpModel.UserId;
            }
            else if (!string.IsNullOrEmpty(corpModel.OpenId))
            {
                userId = corpModel.OpenId;
                type = "WxWorkOpenID";
            }
            return (userId, type);
        }

        [HttpGet("bind")]
        public IActionResult Bind(string backto)
        {
            if (bool.TryParse(Configuration["NoLogin"], out bool noLogin) && noLogin)
                return NotFound();

            return View();
        }

        [HttpPost("bind")]
        public IActionResult Bind(UserViewModel model, string backto)
        {
            if (bool.TryParse(Configuration["NoLogin"], out bool noLogin) && noLogin)
                return NotFound();

            var (externId, type) = HttpContext.Session.GetExternId();
            if (string.IsNullOrEmpty(externId))
            {
                return RedirectToAction("Login");
            }

            var req = new LoginPasswordRequest() { Login = model.userName, Password = model.password };
            var checkResult = _uniflow.CheckUser(req);
            if (checkResult.Result.Value.Code != "0")
            {
                ModelState.AddModelError("errorMsg", "用户名或密码错误!");
                return View(model);
            }

            var bindId = checkResult.Result.Value.BindId;

            var bindResult = _uniflow.Bind(
                new BindExternalIdRequest
                {
                    ExternalId = externId,
                    Type = type,
                    BindId = bindId,
                });

            if (bindResult.Result.Value.Code != "0")
            {
                ModelState.AddModelError("errorMsg", bindResult.Result.Value.Message);
                return View(model);
            }

            HttpContext.Session.SetBindId(bindId);
            HttpContext.Session.SetLdapLoginId(model.userName);

            if (!string.IsNullOrEmpty(backto))
                return Redirect(WebUtility.UrlDecode(backto));
            return RedirectToAction("Index");
        }

        [HttpPost("unBind")]
        public IActionResult UnBind()
        {
            if (bool.TryParse(Configuration["NoLogin"], out bool noLogin) && noLogin)
                return NotFound();

            var (externId, type) = HttpContext.Session.GetExternId();
            var findUser = _ctx.ExternBindings.Where(s => s.ExternalId == externId && s.Type == type).SingleOrDefault<ExternBinding>();
            if (null != findUser)
            {
                _ctx.ExternBindings.Remove(findUser);
                _logger.LogInformation(string.Format("Remove WechatUser:{0}-{1}", findUser.BindUserId, findUser.ExternalId));
            }
            _ctx.SaveChanges();
            return RedirectToAction("Index");
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

        [HttpGet("lxupload")]
        public IActionResult LXLogin(string sign)
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
                return View("Error", new ErrorViewModel { Message = "身份验证失败。" });
            }
            HttpContext.Session.SetLdapLoginId(account);
            HttpContext.Session.SetBindId(SessionKeys.NoLoginBindIdValue);

            return RedirectToAction("Index");
        }

        private void MoveToOutput(PrintTaskDetail model, string uid, string tempdoc, string template)
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

            var targetPaths = Configuration["UniflowService:TaskTargetPath"];
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
                    if (!RunConvert(task.PrintModel.Path, tempdoc,
                            task.PrintModel.Orientation == Orientation.Landscape) ||
                        !System.IO.File.Exists(tempdoc))
                    {
                        task.Status = PrintTaskStatus.Failed;
                        task.Message = "Failed to convert document.";
                    }
                    else
                    {
                        MoveToOutput(task.PrintModel, task.UserID, tempdoc, Template.ticketPdf);
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

        bool RunConvert(string source, string target, bool landscape)
        {
            var landscapearg = landscape ? "-landscape" : "-portrait";
            var processInfo = new ProcessStartInfo
            {
                FileName = Configuration["PdfConverter"],
                Arguments = $"\"{source}\" \"{target}\" {landscapearg}",
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

        public IActionResult TestAccessShare()
        {

            return View();
        }
    }
}
