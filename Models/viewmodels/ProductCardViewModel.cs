using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhoneStore_New.Models.ViewModels // Nhớ đảm bảo đúng namespace của project mới
{
    public class ProductCardViewModel
    {
        public int ProductId { get; set; }
        public int SoldQuantity { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public int StockQuantity { get; set; }

        public decimal OriginalPrice { get; set; }
        public int DiscountPercentage { get; set; }

        // === BẮT ĐẦU SỬA ĐỔI ===
        // Thêm 2 thuộc tính mới để Controller tính toán và gửi cho View

        /// <summary>
        /// Giá cuối cùng sẽ được hiển thị cho người dùng (đã tính toán VIP hoặc khuyến mãi).
        /// </summary>
        public decimal FinalPrice { get; set; }

        /// <summary>
        /// Một lá cờ để báo cho View biết đây có phải là giá dành riêng cho VIP không.
        /// </summary>
        public bool IsVipPrice { get; set; }
        // === KẾT THÚC SỬA ĐỔI ===


        // Giữ lại các thuộc tính tính toán cũ để có thể so sánh
        public decimal SalePrice => OriginalPrice * (1m - DiscountPercentage / 100m);
        public string FormattedOriginalPrice => string.Format("{0:N0} VNĐ", OriginalPrice);
        public string FormattedSalePrice => string.Format("{0:N0} VNĐ", SalePrice);

        // Thêm thuộc tính định dạng cho giá cuối cùng
        public string FormattedFinalPrice => string.Format("{0:N0} VNĐ", FinalPrice);
        
    }
}