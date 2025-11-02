using System.Collections.Generic;
using PhoneStore_New.Models;

namespace PhoneStore_New.Models.ViewModels
{
    public class HeaderViewModel
    {
        public string LogoUrl { get; set; }
        public string WelcomeText { get; set; }
        public string UserFirstName { get; set; }
        public string UserAvatarUrl { get; set; }
        public bool IsLoggedIn { get; set; }
        public List<NavbarItemViewModel> NavbarItems { get; set; }
        public List<Category> Categories { get; set; }

        // === THÊM DÒNG MỚI NÀY VÀO ===
        /// <summary>
        /// Cài đặt (lấy từ CSDL) để quyết định có hiện thanh tìm kiếm hay không.
        /// </summary>
        public bool ShowSearchBar { get; set; }
    }
}