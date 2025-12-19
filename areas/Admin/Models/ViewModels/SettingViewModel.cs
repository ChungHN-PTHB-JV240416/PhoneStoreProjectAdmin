using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Để dùng Display, DisplayFormat
using System.Web;
using System.Web.Mvc;
using PhoneStore_New.Models.ViewModels; // Để nhận diện PriceRangeViewModel nếu nó nằm ở namespace khác

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class SettingViewModel
    {
        // === 1. CÀI ĐẶT CHUNG ===
        [Display(Name = "Lời chào (Header)")]
        public string WelcomeText { get; set; }

        [Display(Name = "Số sản phẩm mỗi dòng")]
        public int ProductsPerRow { get; set; }

        [Display(Name = "Hiển thị thanh tìm kiếm")]
        public bool ShowSearchBar { get; set; }

        // === 2. CÀI ĐẶT FOOTER ===
        [Display(Name = "Văn bản Footer")]
        public string FooterText { get; set; }

        [Display(Name = "Địa chỉ")]
        public string FooterAddress { get; set; }

        [Display(Name = "Số điện thoại")]
        public string FooterPhone { get; set; }

        // === 3. QUẢN LÝ FILE UPLOAD (Logo, Background, QR) ===
        public HttpPostedFileBase LogoFile { get; set; }
        public string CurrentLogoUrl { get; set; }

        public HttpPostedFileBase BackgroundFile { get; set; }
        public string CurrentBackgroundUrl { get; set; }

        public HttpPostedFileBase QrCodeFile { get; set; }
        public string CurrentQrCodeUrl { get; set; }

        // === 4. CẤU HÌNH FLASH SALE (QUAN TRỌNG) ===
        [Display(Name = "Bật tính năng Flash Sale")]
        public bool FlashSaleIsActive { get; set; } // Bật/Tắt thủ công

        [Display(Name = "Thời gian bắt đầu")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? FlashSaleStartTime { get; set; } // Giờ bắt đầu

        [Display(Name = "Thời gian kết thúc")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? FlashSaleEndTime { get; set; }   // Giờ kết thúc

        // === 5. CÁC DANH SÁCH HỖ TRỢ VIEW (DROPDOWN & TABLE) ===

        // Danh sách Navbar để hiển thị bảng quản lý
        public List<NavbarItemViewModel> NavbarItems { get; set; }

        // Danh sách Khoảng giá
        public List<PriceRangeViewModel> PriceRanges { get; set; }

        // Danh sách các mục có thể làm cha (để đổ vào Dropdown Menu)
        public List<SelectListItem> ParentNavbarItems { get; set; }

        // Danh sách các Trang nội dung (Pages) để liên kết nhanh
        public List<SelectListItem> PageLinks { get; set; }

        // Thông báo hệ thống
        public string Message { get; set; }
    }
}