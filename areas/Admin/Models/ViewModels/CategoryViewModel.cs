using System.ComponentModel.DataAnnotations;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống.")]
        [StringLength(50, ErrorMessage = "Tên danh mục không được vượt quá 50 ký tự.")]
        [Display(Name = "Tên Danh mục")]
        public string Name { get; set; }
    }
}