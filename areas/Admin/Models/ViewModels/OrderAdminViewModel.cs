using System;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class OrderAdminViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string PaymentMethod { get; set; }
        public string CustomerName { get; set; }
        public string Username { get; set; }

        public string FormattedTotalAmount => string.Format("{0:N0} VNĐ", TotalAmount);
    }
}