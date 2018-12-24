using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace UniFlowGW.ViewModels
{
    public enum ColorMode { [Display(Name = "�ڰ�")] BW, [Display(Name = "��ɫ")] Color, }
    public enum PaperSize { A4 = 1, A3 = 2, }
    public enum Orientation { [Display(Name = "����")] Portrait, [Display(Name = "����")] Landscape, }
    public enum PaperMode {
        [Display(Name = "����")] Simplex,
        [Display(Name = "˫�泤�߷�ת")] LongEdge,
        [Display(Name = "˫��̱߷�ת")] ShortEdge,
    }
    public class PrintViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        [Display(Name = "�ļ�·��")]
        public string Path { get; set; }
        [Display(Name = "�ĵ�")]
        public string Document { get; set; }
        [Display(Name = "�û� ID")]
        public string UserID { get; set; }
        [Display(Name = "����")]
        public int Copies { get; set; } = 1;
        [Display(Name = "��ɫģʽ")]
        public ColorMode ColorMode { get; set; } = ColorMode.BW;
        [Display(Name = "ҳ��ߴ�")]
        public PaperSize PaperSize { get; set; } = PaperSize.A4;
        [Display(Name = "����")]
        public Orientation Orientation { get; set; } = Orientation.Portrait;
        [Display(Name = "װ��ģʽ")]
        public PaperMode PaperMode { get; set; } = PaperMode.Simplex;
    }
}