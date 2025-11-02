using System;
using System.ComponentModel.DataAnnotations;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class VoucherViewModel
    {
        public int VoucherId { get; set; }

        [Required(ErrorMessage = "Mã code không được để trống")]
        [Display(Name = "Mã Code (ví dụ: SALE100K)")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại giảm giá")]
        [Display(Name = "Loại giảm giá")]
        public string DiscountType { get; set; } // "fixed" (tiền mặt) hoặc "percentage" (phần trăm)

        [Required(ErrorMessage = "Giá trị không được để trống")]
        [Display(Name = "Giá trị giảm")]
        public decimal DiscountValue { get; set; }

        [Display(Name = "Ngày hết hạn (bỏ trống nếu không hết hạn)")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Required(ErrorMessage = "Giới hạn sử dụng không được để trống")]
        [Display(Name = "Giới hạn lượt dùng")]
        public int UsageLimit { get; set; }

        [Display(Name = "Số lượt đã dùng")]
        public int UsageCount { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; }

        [Display(Name = "Chỉ dành cho VIP")]
        public bool VipOnly { get; set; }

        // === Thuộc tính tiện ích ===
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Today;
        public bool IsUsedUp => UsageCount >= UsageLimit;
        public string Status
        {
            get
            {
                if (!IsActive) return "Đã tắt";
                if (IsExpired) return "Đã hết hạn";
                if (IsUsedUp) return "Đã hết lượt";
                return "Hoạt động";
            }
        }
    }
}