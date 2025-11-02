using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PhoneStore_New.Models.ViewModels
{
    public class OrderItemViewModel
    {
        // === Dữ liệu đơn hàng hiển thị trên bảng ===

        public int OrderId { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime OrderDate { get; set; } // Giả định là non-nullable hoặc đã xử lý trong Controller

        public decimal TotalAmount { get; set; } // Giả định là non-nullable hoặc đã xử lý trong Controller

        public string Status { get; set; }

        // === Thuộc tính tính toán và định dạng ===

        /// <summary>
        /// Định dạng tổng tiền thành chuỗi tiền tệ.
        /// </summary>
        public string FormattedTotalAmount => string.Format("{0:N0} VNĐ", TotalAmount);

        /// <summary>
        /// Xác định xem người dùng có thể hủy đơn hàng này không (chỉ khi trạng thái là 'pending').
        /// </summary>
        public bool CanCancel => Status.Equals("pending", StringComparison.OrdinalIgnoreCase);
    }
}