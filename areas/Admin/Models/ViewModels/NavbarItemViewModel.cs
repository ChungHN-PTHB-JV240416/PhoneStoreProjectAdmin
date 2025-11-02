using System.ComponentModel.DataAnnotations;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class NavbarItemViewModel
    {
        public int ItemId { get; set; }
        [Required]
        public string ItemText { get; set; }
        public string ItemUrl { get; set; }
        public int ItemOrder { get; set; }
    }
}