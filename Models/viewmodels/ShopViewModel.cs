using System.Collections.Generic;
using System.Web.Mvc;
using PagedList;
using PhoneStore_New.Models;

namespace PhoneStore_New.Models.ViewModels
{
    public class ShopViewModel
    {
        // Danh sách sản phẩm (Phân trang)
        public IPagedList<ProductCardViewModel> Products { get; set; }

        // --- KHẮC PHỤC LỖI CS0117: Thêm thuộc tính này ---
        // Danh sách danh mục hiển thị ở Sidebar (Cột trái)
        public List<NavbarItem> FilterCategories { get; set; }
        // ------------------------------------------------

        // Dữ liệu cho Dropdown (Thanh tìm kiếm)
        public SelectList CategoryList { get; set; }
        public SelectList PriceRangeList { get; set; }

        // Trạng thái lọc hiện tại
        public string Search { get; set; }
        public int? CategoryId { get; set; }
        public int? PriceRangeId { get; set; }
        public string Sort { get; set; }
    }

    // Class giả lập khoảng giá
    public class PriceRangeItem
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
    }
}