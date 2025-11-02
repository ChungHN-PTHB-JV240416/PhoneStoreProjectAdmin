// Trong SupplierListViewModel.cs

using System.Collections.Generic;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class SupplierListViewModel
    {
        public List<SupplierViewModel> Suppliers { get; set; }
        public string Message { get; set; }

        // THÊM THUỘC TÍNH NÀY ĐỂ KHẮC PHỤC LỖI CS1061
        public string SearchQuery { get; set; }
    }
}