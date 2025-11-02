using System.ComponentModel.DataAnnotations;
using System.Web; // <-- THÊM DÒNG NÀY

namespace PhoneStore_New.Models.ViewModels
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } // Chỉ hiển thị, không sửa

        [Required(ErrorMessage = "Tên không được để trống.")]
        [Display(Name = "Tên")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Họ không được để trống.")]
        [Display(Name = "Họ")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        public string Message { get; set; }

        // === BẮT ĐẦU THÊM MỚI ===

        [Display(Name = "Ảnh đại diện hiện tại")]
        public string AvatarUrl { get; set; }

        [Display(Name = "Thay ảnh đại diện mới")]
        public HttpPostedFileBase ImageFile { get; set; } // Dùng để nhận file upload

        // === KẾT THÚC THÊM MỚI ===
    }
}