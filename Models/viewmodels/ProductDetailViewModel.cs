using System.Collections.Generic;

namespace PhoneStore_New.Models.ViewModels // Đảm bảo đúng namespace
{
    public class ProductDetailViewModel
    {
        public ProductCardViewModel Product { get; set; }
        public decimal AverageRating { get; set; }
        public List<ReviewViewModel> Reviews { get; set; }
        public string Message { get; set; }

        // === BẮT ĐẦU SỬA ĐỔI ===
        // Thêm các thuộc tính này để Controller tính toán và gửi cho View
        // (Chúng ta thêm vào đây thay vì lồng trong ProductCardViewModel
        // để giữ cho logic ViewModel được rõ ràng)

        /// <summary>
        /// Giá cuối cùng sẽ được hiển thị cho người dùng.
        /// </summary>
        public decimal FinalPrice { get; set; }

        /// <summary>
        /// Giá gốc (để so sánh và gạch đi nếu cần).
        /// </summary>
        public decimal OriginalPrice { get; set; }

        /// <summary>
        /// Cờ báo cho View biết đây có phải là giá VIP không.
        /// </summary>
        public bool IsVipPrice { get; set; }

        // Định dạng giá
        public string FormattedFinalPrice => string.Format("{0:N0} VNĐ", FinalPrice);
        public string FormattedOriginalPrice => string.Format("{0:N0} VNĐ", OriginalPrice);
        // === KẾT THÚC SỬA ĐỔI ===
    }
}