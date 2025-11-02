using System.Collections.Generic;

namespace PhoneStore_New.Models.ViewModels
{
    public class OrderHistoryViewModel
    {
        /// <summary>
        /// Danh sách các đơn hàng của người dùng, sử dụng OrderItemViewModel để hiển thị.
        /// </summary>
        public List<OrderHistoryItemViewModel> Orders { get; set; }
        /// <summary>
        /// Thông báo (thành công/lỗi) sau khi hủy đơn hàng.
        /// </summary>  
        public string Message { get; set; }
    }
}