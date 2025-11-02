using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PhoneStore_New.Models.ViewModels // Đảm bảo đúng namespace
{
    public class CheckoutViewModel
    {
        // === 1. THÔNG TIN NGƯỜI DÙNG VÀ GIAO HÀNG ===
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống.")]
        [Display(Name = "Địa chỉ nhận hàng")]
        public string ShippingAddress { get; set; }

        // === 2. THÔNG TIN THANH TOÁN ===
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; } // 'cod' hoặc 'transfer'

        public string QrCodeUrl { get; set; }

        // === 3. DỮ LIỆU TỪ CONTROLLER ===
        public List<CartItem> CartItems { get; set; }
        public string Message { get; set; } // Thông báo lỗi hoặc thành công

        // === 4. SỬA ĐỔI: THÊM LOGIC VOUCHER ===

        /// <summary>
        /// Mã voucher mà người dùng nhập vào ô.
        /// </summary>
        public string VoucherCode { get; set; }

        /// <summary>
        /// Số tiền được giảm (được tính toán và lưu trong Session).
        /// </summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Tổng tiền ban đầu (chưa giảm giá).
        /// </summary>
        public decimal SubtotalAmount { get; set; }

        /// <summary>
        /// Tổng tiền cuối cùng (sau khi đã trừ voucher).
        /// </summary>
        public decimal TotalAmount { get; set; }

        // Các thuộc tính định dạng tiền
        public string FormattedSubtotalAmount => string.Format("{0:N0} VNĐ", SubtotalAmount);
        public string FormattedDiscountAmount => string.Format("{0:N0} VNĐ", DiscountAmount);
        public string FormattedTotalAmount => string.Format("{0:N0} VNĐ", TotalAmount);
    }
}