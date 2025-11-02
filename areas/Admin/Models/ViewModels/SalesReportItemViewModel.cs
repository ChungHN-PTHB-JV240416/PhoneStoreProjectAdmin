using System;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class SalesReportItemViewModel
    {
        public DateTime OrderDay { get; set; }
        public decimal DailyRevenue { get; set; }
        public decimal DailyProfit { get; set; }

        public string FormattedRevenue => string.Format("{0:N0} VNĐ", DailyRevenue);
        public string FormattedProfit => string.Format("{0:N0} VNĐ", DailyProfit);
        public string FormattedDay => OrderDay.ToString("dd/MM/yyyy");
    }
}