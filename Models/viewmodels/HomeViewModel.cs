using System.Collections.Generic;
using PhoneStore_New.Models; 

namespace PhoneStore_New.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Banner> Banners { get; set; }

        public int ProductsPerRow { get; set; }

        // === SỬA ĐỔI QUAN TRỌNG: Đổi từ List sang Dictionary ===
        /// <summary>
        /// Danh sách sản phẩm đã được gom nhóm theo Tên Thương hiệu (Category Name).
        /// Key: Tên thương hiệu (ví dụ: "iPhone")
        /// Value: Danh sách các sản phẩm (ProductCardViewModel) của thương hiệu đó.
        /// </summary>
        public Dictionary<string, List<ProductCardViewModel>> ProductsByBrand { get; set; }
        
        // === Chúng ta không cần List<Product> cũ nữa ===
        // public List<ProductCardViewModel> Products { get; set; } 
    }
}