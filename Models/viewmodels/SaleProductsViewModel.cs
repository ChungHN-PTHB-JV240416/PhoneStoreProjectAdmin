using System.Collections.Generic;

namespace PhoneStore_New.Models.ViewModels
{
    public class SaleProductsViewModel
    {
        /// <summary>
        /// Số cột hiển thị sản phẩm (được lấy từ Settings).
        /// </summary>
        public int ProductsPerRow { get; set; }

        /// <summary>
        /// Danh sách sản phẩm được nhóm theo tên Danh mục (đã được lọc sale).
        /// </summary>
        public Dictionary<string, List<ProductCardViewModel>> ProductsByCategory { get; set; }
    }
}