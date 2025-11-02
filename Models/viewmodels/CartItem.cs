using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhoneStore_New.Models.ViewModels
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public int Quantity { get; set; }

        /// <summary>
        /// Thuộc tính tính toán tổng tiền cho mặt hàng này.
        /// </summary>
        public decimal Subtotal => Price * Quantity;
    }
}