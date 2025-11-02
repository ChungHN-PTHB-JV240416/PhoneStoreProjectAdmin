using System;
using System.ComponentModel.DataAnnotations;
using System.Web; // Cần cho HttpPostedFileBase

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class BannerViewModel
    {
        public int BannerId { get; set; }

        [Display(Name = "Ảnh Banner Mới")]
        public HttpPostedFileBase ImageFile { get; set; } // Dùng để nhận file upload

        [Display(Name = "Ảnh hiện tại")]
        public string ImageUrl { get; set; } // Dùng để hiển thị ảnh cũ

        [Display(Name = "Đường link (URL) khi click vào")]
        public string LinkUrl { get; set; }

        [Display(Name = "Tiêu đề (hiển thị khi di chuột)")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thứ tự hiển thị")]
        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Kích hoạt (Hiển thị)")]
        public bool IsActive { get; set; }
    }
}