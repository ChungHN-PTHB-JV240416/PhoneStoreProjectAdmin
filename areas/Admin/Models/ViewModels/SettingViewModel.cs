using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class SettingViewModel
    {
        // Settings chung
        public string WelcomeText { get; set; }
        public int ProductsPerRow { get; set; }

        // === THÊM MỚI TẠI ĐÂY ===
        [Display(Name = "Hiển thị Thanh Tìm kiếm")]
        public bool ShowSearchBar { get; set; }
        // === KẾT THÚC THÊM MỚI ===

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
    }
}