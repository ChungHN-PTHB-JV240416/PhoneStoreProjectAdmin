using System.Collections.Generic;

namespace PhoneStore_New.Models.ViewModels
{
    public class OrderViewModel
    {
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string ReceiverAddress { get; set; }
        public string Note { get; set; }
        public List<CartItem> CartItems { get; set; }
        public decimal TotalAmount { get; set; }
    }
}