// SupplierViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class SupplierViewModel
    {
        public int SupplierId { get; set; }
        [Required(ErrorMessage = "Tên nhà cung cấp không được để trống.")]
        public string Name { get; set; }
        public string ContactPerson { get; set; }
        [Phone]
        public string Phone { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        public string Address { get; set; }
    }
}

