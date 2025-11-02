using System.Collections.Generic;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class SalesReportViewModel
    {
        public List<SalesReportItemViewModel> ReportItems { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
    }
}