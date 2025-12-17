using System.ComponentModel.DataAnnotations;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class ProductCollectionViewModel
    {
        public int CollectionId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên Bộ sưu tập")]
        [Display(Name = "Tên Bộ sưu tập")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đường dẫn (slug)")]
        [Display(Name = "Đường dẫn (ví dụ: dong-ho-ban-chay)")]
        public string Handle { get; set; } // Đây là URL

        [Display(Name = "Hiển thị (Publish)")]
        public bool IsPublished { get; set; }
    }
}