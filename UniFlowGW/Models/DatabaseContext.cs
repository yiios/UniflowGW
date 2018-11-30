﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UniFlowGW.ViewModels;

namespace UniFlowGW.Models
{
    public enum PrintTaskStatus
    {
        [Display(Name = "已提交")] Committed,
        [Display(Name = "处理中")] Processing,
        [Display(Name = "失败")] Failed
    }
	public class PrintTask
	{
		public int PrintTaskId { get; set; }

        [Display(Name = "文档")]
		public string Document { get; set; }
        [Display(Name = "用户 ID")]
		public string UserID { get; set; }
        [Display(Name = "详细信息")]
		public string Detail { get; set; }

        private PrintViewModel pmodel;
		[NotMapped]
		public PrintViewModel PrintModel
		{
			get {
                if (pmodel == null && !string.IsNullOrEmpty(Detail))
                    pmodel = JsonConvert.DeserializeObject<PrintViewModel>(Detail);
                return pmodel;
            }
			set { Detail = JsonConvert.SerializeObject(value); pmodel = value; }
		}

        [Display(Name = "时间")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}")]
		public DateTime Time { get; set; }
        [Display(Name = "状态")]
		public PrintTaskStatus Status { get; set; }
        [Display(Name = "错误消息")]
		public string Message { get; set; }
		public bool QueuedTask { get; set; }
	}
	public class Admin
	{
		public int AdminId { get; set; }
		public string Login { get; set; }
		public string PasswordHash { get; set; }
	}

	public class WeChatUser
	{
		public int Id { get; set; }
		public string openId { get; set; }
		public string userId { get; set; }
		public DateTime bindDate { get; set; }

	}
	public class DatabaseContext : DbContext
	{
		public DbSet<PrintTask> PrintTasks { get; set; }
		public DbSet<Admin> Admins { get; set; }
		public DbSet<WeChatUser> WeChatUsers { get; set; }

		public DatabaseContext(DbContextOptions<DatabaseContext> options)
			: base(options)
		{ }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			builder.Entity<PrintTask>().HasIndex(pt => pt.UserID);
			builder.Entity<PrintTask>().HasIndex(pt => pt.Time);
		}
	}
}
