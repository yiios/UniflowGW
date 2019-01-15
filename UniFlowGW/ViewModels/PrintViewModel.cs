using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace UniFlowGW.ViewModels
{
    public enum ColorMode { [Display(Name = "黑白")] BW, [Display(Name = "彩色")] Color, }
    public enum PaperSize { A4 = 1, A3 = 2, }
    public enum Orientation { [Display(Name = "纵向")] Portrait, [Display(Name = "横向")] Landscape, }
    public enum PaperMode {
        [Display(Name = "单面")] Simplex,
        [Display(Name = "长边双面")] LongEdge,
        [Display(Name = "短边双面")] ShortEdge,
    }
    public class PrintViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        [Required]
        [Display(Name = "文档")]
        public IFormFile Document { get; set; }

        [Display(Name = "份数")]
        public int Copies { get; set; } = 1;
        [Display(Name = "颜色模式")]
        public ColorMode ColorMode { get; set; } = ColorMode.BW;
        [Display(Name = "页面尺寸")]
        public PaperSize PaperSize { get; set; } = PaperSize.A4;
        [Display(Name = "方向")]
        public Orientation Orientation { get; set; } = Orientation.Portrait;
        [Display(Name = "装订模式")]
        public PaperMode PaperMode { get; set; } = PaperMode.Simplex;
    }

    public class PrintTaskDetail
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        [Display(Name = "文件路径")]
        public string Path { get; set; }
        [Display(Name = "文档")]
        public string Document { get; set; }
        [Display(Name = "用户 ID")]
        public string UserID { get; set; }
        [Display(Name = "份数")]
        public int Copies { get; set; } = 1;
        [Display(Name = "颜色模式")]
        public ColorMode ColorMode { get; set; } = ColorMode.BW;
        [Display(Name = "页面尺寸")]
        public PaperSize PaperSize { get; set; } = PaperSize.A4;
        [Display(Name = "方向")]
        public Orientation Orientation { get; set; } = Orientation.Portrait;
        [Display(Name = "装订模式")]
        public PaperMode PaperMode { get; set; } = PaperMode.Simplex;
    }
}