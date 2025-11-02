using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;
using System.Collections.Generic;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class ProductAdminEditViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
        [Display(Name = "Tên Sản phẩm")]
        public string Name { get; set; }

        [Display(Name = "Mô tả")]
        [AllowHtml]
        public string Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá.")]
        [Range(0.01, 1000000000.00, ErrorMessage = "Giá không hợp lệ.")]
        [Display(Name = "Giá Bán")]
        public decimal Price { get; set; }

        // === SỬA ĐỔI: THÊM 2 TRƯỜNG MỚI ===
        [Display(Name = "Giá Nhập")]
        [Range(0.00, 1000000000.00, ErrorMessage = "Giá nhập không hợp lệ.")]
        public decimal? PurchasePrice { get; set; }

        [Display(Name = "Giá VIP")]
        [Range(0.00, 1000000000.00, ErrorMessage = "Giá VIP không hợp lệ.")]
        public decimal? vip_price { get; set; }
        // === KẾT THÚC SỬA ĐỔI ===

        [Required(ErrorMessage = "Vui lòng nhập số lượng.")]
        [Display(Name = "Số lượng tồn kho")]
        public int StockQuantity { get; set; }

        [Display(Name = "Danh mục")]
        public int? CategoryId { get; set; }

        [Display(Name = "Giảm giá (%)")]
        [Range(0, 100, ErrorMessage = "Giảm giá phải từ 0 đến 100.")]
        public int DiscountPercentage { get; set; }

        [Display(Name = "Hình ảnh mới")]
        public HttpPostedFileBase ImageFile { get; set; }

        public string CurrentImageUrl { get; set; }
    }
}