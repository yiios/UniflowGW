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
            serviceProvider.GetService()
			_ctx = ctx;
			_logger = logger;
		}

        public ActionResult Index()
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
	}
}
