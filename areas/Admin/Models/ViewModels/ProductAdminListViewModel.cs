using System.Collections.Generic;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class ProductAdminListViewModel
    {
        public List<ProductAdminViewModel> Products { get; set; }
        public string SearchQuery { get; set; }
        public string Message { get; set; }
    }
}