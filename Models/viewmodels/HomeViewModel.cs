using System.Collections.Generic;
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

        // --- THÊM MỚI: Để map với form lọc ở View ---
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }
}