using System.ComponentModel.DataAnnotations;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class NavbarItemViewModel
    {
        public int ItemId { get; set; }

        [Required(ErrorMessage = "Tên mục không được để trống")]
        [Display(Name = "Tên mục")]
        public string ItemText { get; set; }

        [Display(Name = "Đường dẫn (URL)")]
        public string ItemUrl { get; set; }

        [Display(Name = "Thứ tự")]
        public int ItemOrder { get; set; }

        [Display(Name = "Mục cha")]
        public int? ParentId { get; set; } // <-- THÊM THUỘC TÍNH NÀY
    }
}