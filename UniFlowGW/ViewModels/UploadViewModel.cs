using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace UniFlowGW.ViewModels
{
    public class UploadViewModel
    {
        [Required]
        [Display(Name ="ÓÃ»§ ID")]
        public string UserID { get; set; }
        [Required]
        [Display(Name = "ÎÄµµ")]
        public IFormFile Document { get; set; }
    }
}