using System.ComponentModel.DataAnnotations;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class PriceRangeViewModel
    {
        public int RangeId { get; set; }

        [Required(ErrorMessage = "Nhãn không được để trống")]
        [Display(Name = "Tên nhãn (ví dụ: Dưới 1 triệu)")]
        public string RangeLabel { get; set; }

        [Required]
        [Display(Name = "Giá tối thiểu")]
        public decimal MinPrice { get; set; }

        [Required]
        [Display(Name = "Giá tối đa")]
        public decimal MaxPrice { get; set; }

        [Display(Name = "Thứ tự")]
        public int RangeOrder { get; set; }
    }
}