using System.Collections.Generic;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class CategoryListViewModel
    {
        /// <summary>
        /// Danh sách các danh mục để hiển thị trên bảng.
        /// </summary>
        public List<CategoryViewModel> Categories { get; set; }

        /// <summary>
        /// Chuỗi tìm kiếm hiện tại (dùng để giữ lại giá trị trên ô tìm kiếm).
        /// </summary>
        public string SearchQuery { get; set; }

        /// <summary>
        /// Thông báo (thành công/lỗi) để hiển thị.
        /// </summary>
        public string Message { get; set; }
    }
}