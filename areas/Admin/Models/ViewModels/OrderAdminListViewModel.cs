using System.Collections.Generic;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class OrderAdminListViewModel
    {
        /// <summary>
        /// Danh sách các đơn hàng để hiển thị trên bảng.
        /// Sử dụng OrderAdminViewModel để chứa thông tin chi tiết từng đơn.
        /// </summary>
        public List<OrderAdminViewModel> Orders { get; set; }

        /// <summary>
        /// Thông báo (thành công/lỗi) sau khi cập nhật trạng thái đơn hàng.
        /// </summary>
        public string Message { get; set; }
    }
}