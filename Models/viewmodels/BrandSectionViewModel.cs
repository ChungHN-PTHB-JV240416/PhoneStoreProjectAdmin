using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhoneStore_New.Models.ViewModels
{
    public class BrandSectionViewModel
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; }

        // Danh sách sản phẩm thuộc thương hiệu này
        public List<ProductCardViewModel> Products { get; set; }
    }
}