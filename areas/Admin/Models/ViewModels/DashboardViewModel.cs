using System.Collections.Generic;
using PhoneStore_New.Models; // Cần thiết để gọi Model Product

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int NewOrdersCount { get; set; }
        public decimal DailyRevenue { get; set; }
        public int NewUsersCount { get; set; }
        public List<Product> LowStockProducts { get; set; }
    }
}