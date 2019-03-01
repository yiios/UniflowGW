using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UniFlowGW.Models;
using UniFlowGW.Services;
using UniFlowGW.Util;
using UniFlowGW.ViewModels;

namespace UniFlowGW.Controllers
{
    public class AdminController : Controller
    {

        readonly DatabaseContext _ctx;
        readonly ILogger<AdminController> _logger;
        private readonly LicenseCheckService licenseCheckService;
        readonly SettingService settings;

        public AdminController(SettingService settings,
            DatabaseContext ctx, IServiceProvider serviceProvider,
            ILogger<AdminController> logger,
            LicenseCheckService licenseCheckService)
        {
            this.settings = settings;
            _ctx = ctx;
            _logger = logger;
            this.licenseCheckService = licenseCheckService;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public ActionResult Index()
        {
            if (!HttpContext.Session.HasAdminLogin())
                return RedirectToAction("Login");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl = null)
        {

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {

            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _ctx.Admins.Where(t => t.Login == model.UserName.Trim()).FirstOrDefaultAsync();
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "用户名或密码不正确.");
                    return View(model);
                }
                string salt = settings["Security:Salt"];
                string encrypted = GetHashPassword(model.Password.Trim(), salt);
                if (!user.PasswordHash.Equals(encrypted))
                {
                    ModelState.AddModelError(string.Empty, "用户名或密码不正确.");
                    return View(model);
                }

                HttpContext.Session.SetAdminLoginUser(user.Login);
                _logger.LogInformation("登录成功！");

                if (string.IsNullOrEmpty(returnUrl))
                    return RedirectToAction("Index");
                return Redirect(returnUrl);
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            if (!HttpContext.Session.HasAdminLogin())
                return RedirectToAction("Login", new { returnUrl = Url.Action("Settings") });


            var login = HttpContext.Session.GetAdminLoginUser();
            var user = await _ctx.Admins.Where(t => t.Login == login).FirstOrDefaultAsync();
            if (user == null)
            {
                throw new ApplicationException("用户异常！");
            }
            var model = new ChangePasswordViewModel { StatusMessage = StatusMessage };
            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!HttpContext.Session.HasAdminLogin())
                return RedirectToAction("Login", new { returnUrl = Url.Action("Settings") });

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var login = HttpContext.Session.GetAdminLoginUser();
            var user = await _ctx.Admins.Where(t => t.Login == login).FirstOrDefaultAsync();
            if (user == null)
            {
                throw new ApplicationException("用户异常！");
            }
            string salt = settings["Security:Salt"];
            string encrypted = GetHashPassword(model.OldPassword.Trim(), salt);
            if (!user.PasswordHash.Equals(encrypted))
            {
                ModelState.AddModelError(string.Empty, "当前密码不正确.");
                return View(model);
            }
            if (!model.NewPassword.Equals(model.ConfirmPassword.Trim()))
            {
                ModelState.AddModelError(string.Empty, "确认密码不正确.");
                return View(model);
            }
            string newEncryptedPwd = GetHashPassword(model.NewPassword.Trim(), salt);
            user.PasswordHash = newEncryptedPwd;
            await _ctx.SaveChangesAsync();

            _logger.LogInformation("密码修改成功");
            StatusMessage = "密码修改成功！";

            return RedirectToAction(nameof(ChangePassword));
        }

        private string GetHashPassword(string password, string salt)
        {
            string content = password + salt;
            string encrypted;
            using (var md5 = MD5.Create())
            {
                encrypted = BitConverter.ToString(
                    md5.ComputeHash(Encoding.UTF8.GetBytes(content)))
                    .Replace("-", string.Empty);
            }
            return encrypted;
        }

        public ActionResult Settings()
        {
            if (!HttpContext.Session.HasAdminLogin())
                return RedirectToAction("Login", new { returnUrl = Url.Action("Settings") });

            var model = new SettingsViewModel();
            model.LoadFrom(settings);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Settings(SettingsViewModel model)
        {
            if (!HttpContext.Session.HasAdminLogin())
                return RedirectToAction("Login", new { returnUrl = Url.Action("Settings") });

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.StoreTo(settings);

            return View();
        }

        public ActionResult LicenseState()
        {
            if (!HttpContext.Session.HasAdminLogin())
                return RedirectToAction("Login", new { returnUrl = Url.Action("LicenseState") });

            return View();
        }


        [HttpGet]
        public async Task<IActionResult> LicenseRegister()
        {

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LicenseRegister(string key)
        {
            var res = await licenseCheckService.RegisterLicenseKeyAsync(key);
            if (res.State == Licensing.LicenseRegisterState.InvalidKey)
            {
                ModelState.AddModelError(string.Empty, "授权码无效！");
                return View();
            }
            else if (res.State == Licensing.LicenseRegisterState.AlreadyUsed)
            {
                ModelState.AddModelError(string.Empty, "授权码已经被使用！");
                return View();
            }
            else if (res.State == Licensing.LicenseRegisterState.AlreadyLicensed)
            {
                ModelState.AddModelError(string.Empty, "授权码已经被授权！");
                return View();
            }
            else if (res.State == Licensing.LicenseRegisterState.ServiceError || res.State == Licensing.LicenseRegisterState.StateError)
            {
                ModelState.AddModelError(string.Empty, "服务端异常");
                return View();
            }
            else if (res.State == Licensing.LicenseRegisterState.OK)
            {
                return RedirectToAction("LicenseState");
            }
            return View();
        }


        public ActionResult PrintRecord(string s, string d, string q, string cq, int? p)
        {
            if (!HttpContext.Session.HasAdminLogin())
                return RedirectToAction("Login", new { returnUrl = Url.Action("PrintRecord") });

            var sort = string.IsNullOrEmpty(s) ? nameof(PrintTask.Time) : s;
            var direction = string.IsNullOrEmpty(d) ?
                (sort == nameof(PrintTask.Time) ? "D" : "A") : d;
            int pageIndex = p ?? 1;
            string query = cq;
            if (!string.IsNullOrEmpty(q))
            {
                pageIndex = 1;
                query = q;
            }

            ViewBag.Query = query;
            ViewBag.Sort = sort;
            ViewBag.Direction = direction;

            var querable = from t in _ctx.PrintTasks select t;
            if (!string.IsNullOrEmpty(query))
                querable = querable.Where(t => t.UserID.Contains(query));
            switch (sort + direction)
            {
                case nameof(PrintTask.Time) + "A":
                    querable = querable.OrderBy(t => t.Time);
                    ViewBag.MarkTime = "▲";
                    ViewBag.NextDirectionTime = "D";
                    break;
                case nameof(PrintTask.Time) + "D":
                    querable = querable.OrderByDescending(t => t.Time);
                    ViewBag.MarkTime = "▼";
                    ViewBag.NextDirectionTime = "A";
                    break;
                case nameof(PrintTask.Status) + "A":
                    querable = querable.OrderBy(t => t.Status);
                    ViewBag.MarkStatus = "▲";
                    ViewBag.NextDirectionStatus = "D";
                    break;
                case nameof(PrintTask.Status) + "D":
                    querable = querable.OrderByDescending(t => t.Status);
                    ViewBag.MarkStatus = "▼";
                    ViewBag.NextDirectionStatus = "D";
                    break;
                case nameof(PrintTask.UserID) + "A":
                    querable = querable.OrderBy(t => t.UserID);
                    ViewBag.MarkUserID = "▲";
                    ViewBag.NextDirectionUserID = "D";
                    break;
                case nameof(PrintTask.UserID) + "D":
                    querable = querable.OrderByDescending(t => t.UserID);
                    ViewBag.MarkUserID = "▼";
                    ViewBag.NextDirectionUserID = "A";
                    break;
                default: break;
            }

            var total = _ctx.PrintTasks.Count();
            var count = querable.Count();
            int pageSize = 20;
            int pageCount = (int)Math.Ceiling(count / (double)pageSize);

            ViewBag.Total = total;
            ViewBag.Count = count;
            ViewBag.PageSize = pageSize;
            ViewBag.PageCount = pageCount;
            ViewBag.PageIndex = pageIndex;
            ViewBag.HasPrevPage = pageIndex > 1;
            ViewBag.HasNextPage = pageIndex < pageCount;

            return View(querable.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList());
        }

        public ActionResult Account(string s, string d, string q, string cq, int? p)
        {
            if (!HttpContext.Session.HasAdminLogin())
                return RedirectToAction("Login", new { returnUrl = Url.Action("Account") });

            var sort = string.IsNullOrEmpty(s) ? nameof(BindUser.BindTime) : s;
            var direction = string.IsNullOrEmpty(d) ?
                (sort == nameof(BindUser.BindTime) ? "D" : "A") : d;
            int pageIndex = p ?? 1;
            string query = cq;
            if (!string.IsNullOrEmpty(q))
            {
                pageIndex = 1;
                query = q;
            }

            ViewBag.Query = query;
            ViewBag.Sort = sort;
            ViewBag.Direction = direction;

            var querable = from t in _ctx.BindUsers.Include(t => t.ExternBindings) select t;
            if (!string.IsNullOrEmpty(query))
                querable = querable.Where(t => t.UserLogin.Contains(query));
            switch (sort + direction)
            {
                case nameof(BindUser.BindTime) + "A":
                    querable = querable.OrderBy(t => t.BindTime);
                    ViewBag.MarkTime = "▲";
                    ViewBag.NextDirectionTime = "D";
                    break;
                case nameof(BindUser.BindTime) + "D":
                    querable = querable.OrderByDescending(t => t.BindTime);
                    ViewBag.MarkTime = "▼";
                    ViewBag.NextDirectionTime = "A";
                    break;
                case nameof(BindUser.UserLogin) + "A":
                    querable = querable.OrderBy(t => t.UserLogin);
                    ViewBag.MarkUserID = "▲";
                    ViewBag.NextDirectionUserID = "D";
                    break;
                case nameof(BindUser.UserLogin) + "D":
                    querable = querable.OrderByDescending(t => t.UserLogin);
                    ViewBag.MarkUserID = "▼";
                    ViewBag.NextDirectionUserID = "A";
                    break;
                default: break;
            }

            var total = _ctx.BindUsers.Count();
            var count = querable.Count();
            int pageSize = 20;
            int pageCount = (int)Math.Ceiling(count / (double)pageSize);

            ViewBag.Total = total;
            ViewBag.Count = count;
            ViewBag.PageSize = pageSize;
            ViewBag.PageCount = pageCount;
            ViewBag.PageIndex = pageIndex;
            ViewBag.HasPrevPage = pageIndex > 1;
            ViewBag.HasNextPage = pageIndex < pageCount;

            return View(querable.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList());
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
