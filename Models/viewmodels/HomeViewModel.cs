using System.Collections.Generic;
using System.Web.Mvc; // <--- BẮT BUỘC CÓ ĐỂ DÙNG SelectList
using PhoneStore_New.Models;

namespace PhoneStore_New.Models.ViewModels
{
    public class HomeSection
    {
        public int SectionId { get; set; }
        public string Title { get; set; }
        public int LayoutType { get; set; }
        public string ViewAllUrl { get; set; }
        public List<ProductCardViewModel> Products { get; set; }
    }

    public class HomeViewModel
    {
        public List<Banner> Banners { get; set; }

        // Sidebar danh mục
        public List<NavbarItem> SidebarItems { get; set; }

        // Các Section sản phẩm
        public List<HomeSection> Sections { get; set; }

        // Cấu hình giao diện
        public int ProductsPerRow { get; set; }

        // --- CẬP NHẬT MỚI: Dùng Dropdown Khoảng giá Dynamic ---
        public int? SelectedPriceRangeId { get; set; } // ID khoảng giá được chọn
        public SelectList PriceRangeList { get; set; } // Danh sách đổ vào Dropdown
    }
}