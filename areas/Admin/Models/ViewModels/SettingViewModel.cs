using System.Collections.Generic;
using System.Web;
using System.Web.Mvc; // <-- THÊM DÒNG NÀY (RẤT QUAN TRỌNG)

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class SettingViewModel
    {
        // Settings chung
        public string WelcomeText { get; set; }
        public int ProductsPerRow { get; set; }
        public bool ShowSearchBar { get; set; }

        // Footer Info
        public string FooterText { get; set; }
        public string FooterAddress { get; set; }
        public string FooterPhone { get; set; }

        // Quản lý file upload
        public HttpPostedFileBase LogoFile { get; set; }
        public string CurrentLogoUrl { get; set; }
        public HttpPostedFileBase BackgroundFile { get; set; }
        public string CurrentBackgroundUrl { get; set; }
        public HttpPostedFileBase QrCodeFile { get; set; }
        public string CurrentQrCodeUrl { get; set; }

        // Quản lý Navbar và Price Ranges
        public List<NavbarItemViewModel> NavbarItems { get; set; }
        public List<PriceRangeViewModel> PriceRanges { get; set; }

        public string Message { get; set; }

        // === BẮT ĐẦU THÊM MỚI CHO MENU ĐA CẤP ===

        /// <summary>
        /// Danh sách các mục có thể làm cha (để đổ vào Dropdown)
        /// </summary>
        public List<SelectListItem> ParentNavbarItems { get; set; }

        /// <summary>
        /// Danh sách các Trang (Pages) có thể liên kết đến (để đổ vào Dropdown)
        /// </summary>
        public List<SelectListItem> PageLinks { get; set; }

        // === KẾT THÚC THÊM MỚI ===
    }
}