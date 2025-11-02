using System.Collections.Generic;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class BestsellerReportViewModel
    {
        /// <summary>
        /// Danh sách các sản phẩm bán chạy nhất (Top 10).
        /// </summary>
        public List<BestsellerItemViewModel> Bestsellers { get; set; }
    }
}