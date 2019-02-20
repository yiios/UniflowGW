using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UniFlowGW.Models;
using UniFlowGW.Util;
using UniFlowGW.ViewModels;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace UniFlowGW.Controllers
{
    public class AdminController : Controller
    {

        readonly DatabaseContext _ctx;
        readonly ILogger<AdminController> _logger;
        public IConfiguration Configuration { get; }

        public AdminController(IConfiguration configuration,
            DatabaseContext ctx, IServiceProvider serviceProvider,
            ILogger<AdminController> logger)
        {
            Configuration = configuration;
            _ctx = ctx;
            _logger = logger;
        }

        public ActionResult Index()
        {
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
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                /*
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    //return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
                */
            }

            // If we got this far, something failed, redisplay form
            return RedirectToAction("Index");
        }


        public async Task<IActionResult> Logout()
        {
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            /*
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword)
            {
                return RedirectToAction(nameof(SetPassword));
            }

           
            */
            var model = new ChangePasswordViewModel { StatusMessage = "" };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            /*
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                AddErrors(changePasswordResult);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User changed their password successfully.");
            StatusMessage = "Your password has been changed.";

            return RedirectToAction(nameof(ChangePassword));

          */
            return View();
        }

        public ActionResult Settings()
        {
            return View();
        }

        public ActionResult LicenseState()
        {
            return View();
        }

        public ActionResult PrintRecord(string s, string d, string q, string cq, int? p)
        {
            var sort = string.IsNullOrEmpty(s) ? nameof(PrintTask.Time) : s;
            var direction = string.IsNullOrEmpty(d) ?
                (sort == nameof(PrintTask.Time) ? "D" : "A") : d;
            int pageIndex = p ?? 1;
            string query = cq;
            if (!string.IsNullOrEmpty(q))
                pageIndex = 1;
            else
                query = q;

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
            var sort = string.IsNullOrEmpty(s) ? nameof(BindUser.BindTime) : s;
            var direction = string.IsNullOrEmpty(d) ?
                (sort == nameof(BindUser.BindTime) ? "D" : "A") : d;
            int pageIndex = p ?? 1;
            string query = cq;
            if (!string.IsNullOrEmpty(q))
                pageIndex = 1;
            else
                query = q;

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
                    ViewBag.MarkStatus = "▲";
                    ViewBag.NextDirectionStatus = "D";
                    break;
                case nameof(BindUser.UserLogin) + "D":
                    querable = querable.OrderByDescending(t => t.UserLogin);
                    ViewBag.MarkStatus = "▼";
                    ViewBag.NextDirectionStatus = "D";
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
