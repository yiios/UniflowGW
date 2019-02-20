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
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UniFlowGW.Models;
using UniFlowGW.Util;

namespace UniFlowGW.Controllers
{
    public static class Error
    {
        public enum Codes
        {
            OK = 0,
            LoginRequired = 100,
            UserNotFound = 101,
            InvalidUserId = 102,
            InvalidUserData = 103,
            DuplicateUser = 104,
            BindNotFound = 109,
            PrinterNotFound = 201,
            PrinterNotConfigured = 202,
            EncryptError = 701,
            DecryptError = 702,
            InvalidData = 801,
            LoadObjectError = 802,
            Exception = 900,
            ExternalError = 901,
        }

        public static Dictionary<Codes, string> Messages { get; }
        = new Dictionary<Codes, string>()
        {
            [Codes.OK] = "成功",
            //[Codes.InvalidData] = "无效数据: [{0}]: {1}",
            //[Codes.Exception] = "未知错误"
        };
        public static string AsMessage(this Codes code, params object[] args)
        {
            if (Messages.ContainsKey(code))
                return string.Format(Messages[code], args);
            return code.ToString() + ":" + string.Join(",", args);
        }
        public static string AsString(this Codes code)
            => ((int)code).ToString();
        public static Codes AsCode(this string codestr)
            => Enum.Parse<Codes>(codestr);
    }

#if !DEBUG_DUMMY
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

        [HttpPost("checkuser")]
        public async Task<ActionResult<BindStatusResponse>> CheckUser(LoginPasswordRequest req)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.First(m => m.Value.Errors.Count > 0);
                return new BindStatusResponse
                {
                    Code = Error.Codes.InvalidData.AsString(),
                    Message = Error.Codes.InvalidData.AsMessage(
                        error.Key, error.Value.Errors[0].ErrorMessage),
                };
            }

            try
            {
                string baseurl = Configuration["UniflowService:Url"];
                string key = Configuration["UniflowService:EncryptKey"];
                string salt = EncryptUtil.CreateCryptographicallySecureGuid();

                string login = EncryptUtil.Encrypt(req.Login, key, salt);
                string password = EncryptUtil.Encrypt(req.Password, key, salt);

                string url = $"{baseurl}/WECHAT/CHECKUSER/{login}/{password}";
                _logger.LogTrace("Get " + url);
                var result = await RequestUtil.HttpGetAsync(url);
                _logger.LogTrace("Response: " + result);

                var xdoc = XElement.Parse(result);
                var ns = xdoc.GetDefaultNamespace();
                var status = xdoc.Element(ns.GetName("Status")).Value;
                var bindId = xdoc.Element(ns.GetName("UserRef")).Value;
                var code = xdoc.Element(ns.GetName("ErrorCode")).Value;
                var message = status == "0" ? Error.Codes.OK.AsMessage() :
                    code.AsCode().AsMessage(xdoc.Element(ns.GetName("ErrorDesc")).Value);

                var response = new BindStatusResponse
                {
                    Code = code,
                    Message = message,
                    BindId = bindId,
                    LdapLoginId = req.Login,
                };

                if (status != "0")
                    return response;

                var bind = _ctx.BindUsers.Find(bindId);
                if (bind == null)
                {
                    _ctx.BindUsers.Add(new BindUser
                    {
                        BindUserId = bindId,
                        UserLogin = req.Login,
                    });
                    await _ctx.SaveChangesAsync();
                }
                else if (bind.UserLogin != req.Login)
                {
                    _logger.LogWarning("Login changed: " + req.Login + " " + JsonHelper.SerializeObject(bind));
                    bind.UserLogin = req.Login;
                    await _ctx.SaveChangesAsync();
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error");
                return new BindStatusResponse
                {
                    Code = Error.Codes.Exception.AsString(),
                    Message = Error.Codes.Exception.AsMessage(
                        ex.Message),
                };
            }
        }

        [HttpPost("checkbind")]
        public ActionResult<BindStatusResponse> CheckBind(ExternalIdRequest req)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.First(m => m.Value.Errors.Count > 0);
                return new BindStatusResponse
                {
                    Code = Error.Codes.InvalidData.AsString(),
                    Message = Error.Codes.InvalidData.AsMessage(
                        error.Key, error.Value.Errors[0].ErrorMessage),
                };
            }

            try
            {
                var bind = _ctx.ExternBindings
                    .Include(b => b.BindUser)
                    .Where(b => b.Type == req.Type && b.ExternalId == req.ExternalId)
                    .FirstOrDefault();
                var code = bind == null ? Error.Codes.BindNotFound : Error.Codes.OK;
                return new BindStatusResponse
                {
                    Code = code.AsString(),
                    Message = code.AsMessage(),
                    BindId = bind?.BindUserId ?? null,
                    LdapLoginId = bind?.BindUser.UserLogin ?? null,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error");
                return new BindStatusResponse
                {
                    Code = Error.Codes.Exception.AsString(),
                    Message = Error.Codes.Exception.AsMessage(
                        ex.Message),
                };
            }
        }

        [HttpPost("bind")]
        public async Task<ActionResult<StatusResponse>> Bind(BindExternalIdRequest req)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.First(m => m.Value.Errors.Count > 0);
                return new StatusResponse
                {
                    Code = Error.Codes.InvalidData.AsString(),
                    Message = Error.Codes.InvalidData.AsMessage(
                        error.Key, error.Value.Errors[0].ErrorMessage),
                };
            }

            try
            {
                var bind = await _ctx.BindUsers.FindAsync(req.BindId);
                if (bind == null)
                {
                    var code = Error.Codes.BindNotFound;
                    return new StatusResponse
                    {
                        Code = code.AsString(),
                        Message = code.AsMessage(),
                    };
                }

                if (!bind.IsBinded)
                {
                    string baseurl = Configuration["UniflowService:Url"];
                    string key = Configuration["UniflowService:EncryptKey"];

                    string openid = EncryptUtil.Encrypt(bind.BindUserId, key);

                    string url = $"{baseurl}/WECHAT/BINDUSER/{req.BindId}/{openid}";
                    _logger.LogTrace("Get " + url);
                    var result = await RequestUtil.HttpGetAsync(url);
                    _logger.LogTrace("Response: " + result);

                    var xdoc = XElement.Parse(result);
                    var ns = xdoc.GetDefaultNamespace();
                    var status = xdoc.Element(ns.GetName("Status")).Value;
                    var code = xdoc.Element(ns.GetName("ErrorCode")).Value;
                    var message = status == "0" ? Error.Codes.OK.AsMessage() :
                        code.AsCode().AsMessage(xdoc.Element(ns.GetName("ErrorDesc")).Value);

                    if (status != "0")
                    {
                        return new StatusResponse
                        {
                            Code = code,
                            Message = message,
                        };
                    }

                    bind.BindTime = DateTime.Now;
                    bind.IsBinded = true;
                    await _ctx.SaveChangesAsync();
                }

                var externBind = _ctx.ExternBindings
                    .Where(b => b.BindUserId == bind.BindUserId && b.Type == req.Type && b.ExternalId == req.ExternalId)
                    .FirstOrDefault();

                if (externBind == null)
                {
                    _ctx.ExternBindings.Add(new ExternBinding
                    {
                        BindUserId = bind.BindUserId,
                        Type = req.Type,
                        ExternalId = req.ExternalId,
                        BindTime = DateTime.Now,
                    });
                    await _ctx.SaveChangesAsync();
                }

                return new StatusResponse
                {
                    Code = Error.Codes.OK.AsString(),
                    Message = Error.Codes.OK.AsMessage(),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error");
                return new StatusResponse
                {
                    Code = Error.Codes.Exception.AsString(),
                    Message = Error.Codes.Exception.AsMessage(
                        ex.Message),
                };
            }
        }

        [HttpPost("unlock")]
        public async Task<ActionResult<StatusResponse>> Unlock(UnlockRequest req)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.First(m => m.Value.Errors.Count > 0);
                return new StatusResponse
                {
                    Code = Error.Codes.InvalidData.AsString(),
                    Message = Error.Codes.InvalidData.AsMessage(
                        error.Key, error.Value.Errors[0].ErrorMessage),
                };
            }

            try
            {
                var bind = await _ctx.BindUsers.FindAsync(req.BindId);
                if (bind == null || bind.BindTime == null)
                {
                    return new StatusResponse
                    {
                        Code = Error.Codes.BindNotFound.AsString(),
                        Message = Error.Codes.BindNotFound.AsMessage(),
                    };
                }

                string baseurl = Configuration["UniflowService:Url"];
                string key = Configuration["UniflowService:EncryptKey"];

                string openid = EncryptUtil.Encrypt(bind.BindUserId, key);
                string serial = req.Serial;

                string url = $"{baseurl}/WECHAT/UNLOCK/{openid}/{serial}";
                _logger.LogTrace("Get " + url);
                var result = await RequestUtil.HttpGetAsync(url);
                _logger.LogTrace("Response: " + result);

                var xdoc = XElement.Parse(result);
                var ns = xdoc.GetDefaultNamespace();
                var status = xdoc.Element(ns.GetName("Status")).Value;
                var code = xdoc.Element(ns.GetName("ErrorCode")).Value;
                var message = status == "0" ? Error.Codes.OK.AsMessage() :
                    code.AsCode().AsMessage(xdoc.Element(ns.GetName("ErrorDesc")).Value);

                return new StatusResponse
                {
                    Code = code,
                    Message = message,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error");
                return new StatusResponse
                {
                    Code = Error.Codes.Exception.AsString(),
                    Message = Error.Codes.Exception.AsMessage(
                        ex.Message),
                };
            }
        }

        [HttpPost("externalunlock")]
        public async Task<ActionResult<StatusResponse>> ExternalUnlock(ExternalIdUnlockRequest req)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.First(m => m.Value.Errors.Count > 0);
                return new StatusResponse
                {
                    Code = Error.Codes.InvalidData.AsString(),
                    Message = Error.Codes.InvalidData.AsMessage(
                        error.Key, error.Value.Errors[0].ErrorMessage),
                };
            }

            try
            {
                var bind = await _ctx.ExternBindings.FirstAsync(t => t.ExternalId == req.ExternalId && t.Type == req.Type);
                if (bind == null || bind.BindTime == null)
                {
                    return new StatusResponse
                    {
                        Code = Error.Codes.BindNotFound.AsString(),
                        Message = Error.Codes.BindNotFound.AsMessage(),
                    };
                }

                string baseurl = Configuration["UniflowService:Url"];
                string key = Configuration["UniflowService:EncryptKey"];

                string openid = EncryptUtil.Encrypt(bind.BindUserId, key);
                string serial = req.Serial;

                string url = $"{baseurl}/WECHAT/UNLOCK/{openid}/{serial}";
                _logger.LogTrace("Get " + url);
                var result = await RequestUtil.HttpGetAsync(url);
                _logger.LogTrace("Response: " + result);

                var xdoc = XElement.Parse(result);
                var ns = xdoc.GetDefaultNamespace();
                var status = xdoc.Element(ns.GetName("Status")).Value;
                var code = xdoc.Element(ns.GetName("ErrorCode")).Value;
                var message = status == "0" ? Error.Codes.OK.AsMessage() :
                    code.AsCode().AsMessage(xdoc.Element(ns.GetName("ErrorDesc")).Value);

                return new StatusResponse
                {
                    Code = code,
                    Message = message,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error");
                return new StatusResponse
                {
                    Code = Error.Codes.Exception.AsString(),
                    Message = Error.Codes.Exception.AsMessage(
                        ex.Message),
                };
            }
        }

    }




#endif
#if DEBUG_DUMMY
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

        [HttpPost("checkuser")]
        public async Task<ActionResult<BindStatusResponse>> CheckUser(LoginPasswordRequest req)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.First(m => m.Value.Errors.Count > 0);
                return new BindStatusResponse
                {
                    Code = Error.Codes.InvalidData.AsString(),
                    Message = Error.Codes.InvalidData.AsMessage(
                        error.Key, error.Value.Errors[0].ErrorMessage),
                };
            }

            try
            {
                string baseurl = Configuration["UniflowService:Url"];
                string key = Configuration["UniflowService:EncryptKey"];
                string salt = EncryptUtil.CreateCryptographicallySecureGuid();

                string login = EncryptUtil.Encrypt(req.Login, key, salt);
                string password = EncryptUtil.Encrypt(req.Password, key, salt);

                var bindId = req.Login;
                var code = "0";
                var message = "OK";

                var response = new BindStatusResponse
                {
                    Code = code,
                    Message = message,
                    BindId = bindId,
                    LdapLoginId = req.Login,
                };

                var bind = _ctx.BindUsers.Find(bindId);
                if (bind == null)
                {
                    _ctx.BindUsers.Add(new BindUser
                    {
                        BindUserId = bindId,
                        UserLogin = req.Login,
                    });
                    await _ctx.SaveChangesAsync();
                }
                else if (bind.UserLogin != req.Login)
                {
                    _logger.LogWarning("Login changed: " + req.Login + " " + JsonHelper.SerializeObject(bind));
                    bind.UserLogin = req.Login;
                    await _ctx.SaveChangesAsync();
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error");
                return new BindStatusResponse
                {
                    Code = Error.Codes.Exception.AsString(),
                    Message = Error.Codes.Exception.AsMessage(
                        ex.Message),
                };
            }
        }

        [HttpPost("checkbind")]
        public ActionResult<BindStatusResponse> CheckBind(ExternalIdRequest req)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.First(m => m.Value.Errors.Count > 0);
                return new BindStatusResponse
                {
                    Code = Error.Codes.InvalidData.AsString(),
                    Message = Error.Codes.InvalidData.AsMessage(
                        error.Key, error.Value.Errors[0].ErrorMessage),
                };
            }

            try
            {
                var bind = _ctx.ExternBindings
                    .Where(b => b.Type == req.Type && b.ExternalId == req.ExternalId)
                    .FirstOrDefault();
                var code = bind == null ? Error.Codes.BindNotFound : Error.Codes.OK;
                return new BindStatusResponse
                {
                    Code = code.AsString(),
                    Message = code.AsMessage(),
                    BindId = bind?.BindUserId ?? null,
                    LdapLoginId = bind?.BindUser.UserLogin ?? null,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error");
                return new BindStatusResponse
                {
                    Code = Error.Codes.Exception.AsString(),
                    Message = Error.Codes.Exception.AsMessage(
                        ex.Message),
                };
            }
        }

        [HttpPost("bind")]
        public async Task<ActionResult<StatusResponse>> Bind(BindExternalIdRequest req)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.First(m => m.Value.Errors.Count > 0);
                return new StatusResponse
                {
                    Code = Error.Codes.InvalidData.AsString(),
                    Message = Error.Codes.InvalidData.AsMessage(
                        error.Key, error.Value.Errors[0].ErrorMessage),
                };
            }

            try
            {
                var bind = await _ctx.BindUsers.FindAsync(req.BindId);
                if (bind == null)
                {
                    var code = Error.Codes.BindNotFound;
                    return new StatusResponse
                    {
                        Code = code.AsString(),
                        Message = code.AsMessage(),
                    };
                }

                if (!bind.IsBinded)
                {
                    bind.BindTime = DateTime.Now;
                    bind.IsBinded = true;
                    await _ctx.SaveChangesAsync();
                }

                var externBind = _ctx.ExternBindings
                    .Where(b => b.BindUserId == bind.BindUserId && b.Type == req.Type && b.ExternalId == req.ExternalId)
                    .FirstOrDefault();

                if (externBind == null)
                {
                    _ctx.ExternBindings.Add(new ExternBinding
                    {
                        BindUserId = bind.BindUserId,
                        Type = req.Type,
                        ExternalId = req.ExternalId,
                        BindTime = DateTime.Now,
                    });
                    await _ctx.SaveChangesAsync();
                }

                return new StatusResponse
                {
                    Code = Error.Codes.OK.AsString(),
                    Message = Error.Codes.OK.AsMessage(),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error");
                return new StatusResponse
                {
                    Code = Error.Codes.Exception.AsString(),
                    Message = Error.Codes.Exception.AsMessage(
                        ex.Message),
                };
            }
        }

        [HttpPost("unlock")]
        public async Task<ActionResult<StatusResponse>> Unlock(UnlockRequest req)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.First(m => m.Value.Errors.Count > 0);
                return new StatusResponse
                {
                    Code = Error.Codes.InvalidData.AsString(),
                    Message = Error.Codes.InvalidData.AsMessage(
                        error.Key, error.Value.Errors[0].ErrorMessage),
                };
            }

            try
            {
                var bind = await _ctx.BindUsers.FindAsync(req.BindId);
                if (bind == null || bind.BindTime == null)
                {
                    return new StatusResponse
                    {
                        Code = Error.Codes.BindNotFound.AsString(),
                        Message = Error.Codes.BindNotFound.AsMessage(),
                    };
                }

                return new StatusResponse
                {
                    Code = "0",
                    Message = "OK",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error");
                return new StatusResponse
                {
                    Code = Error.Codes.Exception.AsString(),
                    Message = Error.Codes.Exception.AsMessage(
                        ex.Message),
                };
            }
        }

    }
#endif

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

    public class UnlockRequest
    {
        [Required]
        public string BindId { get; set; }
        [Required]
        public string Serial { get; set; }
    }
    public class ExternalIdUnlockRequest
    {
        [Required]
        public string ExternalId { get; set; }
        [Required]
        public string Type { get; set; }
        [Required]
        public string Serial { get; set; }
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
    }
    public class BindStatusResponse : BaseResponseBody
    {
        public string BindId { get; set; }
        public string LdapLoginId { get; set; }
    }
}