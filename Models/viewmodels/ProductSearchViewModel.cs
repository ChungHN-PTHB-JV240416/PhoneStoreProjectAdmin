using System.Collections.Generic;
using System.Web.Mvc;
using PagedList; // <-- THÊM DÒNG NÀY

namespace PhoneStore_New.Models.ViewModels
{
    public class ProductSearchViewModel
    {
        public string Keyword { get; set; }

        public List<SelectListItem> Categories { get; set; }
        public List<SelectListItem> PriceRanges { get; set; }

        public int? SelectedCategoryId { get; set; }
        public int? SelectedPriceRangeId { get; set; }

        // === SỬA ĐỔI QUAN TRỌNG: ĐỔI TỪ List SANG IPagedList ===
        /// <summary>
        /// Kết quả tìm kiếm (đã được phân trang).
        /// </summary>
        public IPagedList<ProductCardViewModel> Results { get; set; }
        // === KẾT THÚC SỬA ĐỔI ===

        public ProductSearchViewModel()
        {
            // Khởi tạo rỗng để tránh lỗi
            Results = new PagedList<ProductCardViewModel>(null, 1, 1);
            Categories = new List<SelectListItem>();
            PriceRanges = new List<SelectListItem>();
        }
    }
}