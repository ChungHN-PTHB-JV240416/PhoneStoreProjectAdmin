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

        // === SỬA ĐỔI: DÙNG LẠI NavbarItem TỪ CSDL ===
        // Thay vì NavbarItemViewModel, chúng ta dùng luôn Model gốc
        // để có thể truy cập ParentId
        public List<NavbarItem> NavbarItems { get; set; }

        public List<Category> Categories { get; set; }
        public bool ShowSearchBar { get; set; }
    }
}