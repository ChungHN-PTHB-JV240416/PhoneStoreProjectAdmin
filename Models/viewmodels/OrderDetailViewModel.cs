using System;
using System.Collections.Generic;

namespace PhoneStore_New.Models.ViewModels
{
    public class OrderDetailViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; }
        public List<OrderItemDetailViewModel> Items { get; set; }

        public OrderDetailViewModel()
        {
            Items = new List<OrderItemDetailViewModel>();
        }
    }

    public class OrderItemDetailViewModel
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAtOrder { get; set; }
        public decimal Subtotal => Quantity * PriceAtOrder;
    }
}