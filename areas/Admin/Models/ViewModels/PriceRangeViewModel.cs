// Areas/Admin/Models/ViewModels/PriceRangeViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class PriceRangeViewModel
    {
        // THÊM THUỘC TÍNH BỊ LỖI (CS0117)
        public int RangeId { get; set; }

        [Required]
        public string RangeLabel { get; set; }

        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }

        // THÊM THUỘC TÍNH BỊ LỖI (CS1061)
        public int RangeOrder { get; set; }
    }
}