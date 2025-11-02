using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Collections.Generic;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class UserViewModel
    {
        public int UserId { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Tên.")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Họ.")]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        public string Role { get; set; }

        [Display(Name = "Loại Tài khoản")]
        public string UserType { get; set; }

        [Display(Name = "VIP Hết hạn")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? VipExpiryDate { get; set; }

        public List<SelectListItem> Roles { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "user", Text = "User" },
            new SelectListItem { Value = "admin", Text = "Admin" }
        };

        public List<SelectListItem> UserTypes { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "regular", Text = "Thường" },
            new SelectListItem { Value = "vip", Text = "VIP" }
        };
    }
}