// Trong file: Areas/Admin/Models/ViewModels/UserListViewModel.cs

using System.Collections.Generic;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class UserListViewModel
    {
        public List<UserViewModel> Users { get; set; }
        public string SearchQuery { get; set; }
        public string Message { get; set; }

        // === THÊM CONSTRUCTOR NÀY VÀO ===
        public UserListViewModel()
        {
            // Khởi tạo danh sách để đảm bảo nó không bao giờ null
            Users = new List<UserViewModel>();
        }
    }
}