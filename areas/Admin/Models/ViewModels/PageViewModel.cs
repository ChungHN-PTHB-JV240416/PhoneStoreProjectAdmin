using System.ComponentModel.DataAnnotations;
using System.Web.Mvc; // Cần cho [AllowHtml]

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class PageViewModel
    {
        public int PageId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Tiêu đề trang")]
        [Display(Name = "Tiêu đề trang")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Đường dẫn (slug)")]
        [Display(Name = "Đường dẫn (ví dụ: ve-chung-toi)")]
        public string Slug { get; set; } // Đây là URL

        [AllowHtml] // Cho phép nhập HTML (quan trọng cho trình soạn thảo)
        [Display(Name = "Nội dung trang")]
        public string Content { get; set; }

        [Display(Name = "Hiển thị (Publish)")]
        public bool IsPublished { get; set; }
    }
}