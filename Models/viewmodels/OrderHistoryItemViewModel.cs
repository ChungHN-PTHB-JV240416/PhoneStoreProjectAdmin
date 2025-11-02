using System;

namespace PhoneStore_New.Models.ViewModels
{
    public class OrderHistoryItemViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }

        // Thuộc tính tiện ích để định dạng tiền tệ
        public string FormattedTotalAmount => TotalAmount.ToString("N0") + " VNĐ";

        // Thuộc tính tiện ích để kiểm tra xem có thể hủy đơn không
        public bool CanCancel => Status == "pending";
    }
}