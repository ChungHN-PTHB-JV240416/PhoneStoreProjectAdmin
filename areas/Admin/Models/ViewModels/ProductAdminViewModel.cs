using System.ComponentModel.DataAnnotations;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class ProductAdminViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int DiscountPercentage { get; set; }
        public string ImageUrl { get; set; }
        public string FormattedPrice => string.Format("{0:N0} VNĐ", Price);
    }
}