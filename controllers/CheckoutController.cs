using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity; // Cần thêm để dùng .State
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;
using PhoneStore_New;

[Authorize]
public class CheckoutController : Controller
{
    private List<CartItem> GetCart()
    {
        return Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
    }

    // GET: /Checkout/Index
    public ActionResult Index(string message)
    {
        var cart = GetCart();
        if (!cart.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        using (var db = new PhoneStoreDBEntities()) // Đảm bảo đúng tên DbContext
        {
            var userId = int.Parse(IdentityExtensions.GetUserId(User.Identity));
            var user = db.Users.Find(userId);
            var qrUrl = db.Settings.FirstOrDefault(s => s.SettingKey == "qr_code_url")?.SettingValue;

            decimal subtotal = cart.Sum(i => i.Subtotal);
            string appliedVoucherCode = Session["VoucherCode"] as string;
            decimal discountAmount = (decimal)(Session["DiscountAmount"] ?? 0m);
            decimal totalAmount = subtotal - discountAmount;

            var viewModel = new CheckoutViewModel
            {
                FirstName = user?.FirstName,
                LastName = user?.LastName,
                PhoneNumber = user?.PhoneNumber,
                ShippingAddress = user?.Address,
                CartItems = cart,
                QrCodeUrl = qrUrl,
                Message = (TempData["Message"] as string) ?? message,
                SubtotalAmount = subtotal,
                VoucherCode = appliedVoucherCode,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount
            };

            return View(viewModel);
        }
    }

    // POST: /Checkout/Index (Xử lý Đặt hàng)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Index(CheckoutViewModel model, HttpPostedFileBase bill_image)
    {
        var cart = GetCart();

        using (var db = new PhoneStoreDBEntities()) // Đảm bảo đúng tên DbContext
        {
            decimal subtotal = cart.Sum(i => i.Subtotal);
            string appliedVoucherCode = Session["VoucherCode"] as string;
            decimal discountAmount = (decimal)(Session["DiscountAmount"] ?? 0m);
            decimal totalAmount = subtotal - discountAmount;

            model.CartItems = cart;
            model.TotalAmount = totalAmount;

            if (!ModelState.IsValid)
            {
                model.QrCodeUrl = db.Settings.FirstOrDefault(s => s.SettingKey == "qr_code_url")?.SettingValue;
                model.SubtotalAmount = subtotal;
                model.DiscountAmount = discountAmount;
                return View(model);
            }

            foreach (var item in cart)
            {
                var productInDb = db.Products.AsNoTracking().FirstOrDefault(p => p.ProductId == item.ProductId);
                if (productInDb == null)
                {
                    TempData["Message"] = $"Sản phẩm '{item.Name}' không còn tồn tại.";
                    return RedirectToAction("Index", "Cart");
                }
                if (productInDb.StockQuantity < item.Quantity)
                {
                    TempData["Message"] = $"Sản phẩm '{item.Name}' không đủ số lượng.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    string billImageUrl = null;
                    if (model.PaymentMethod == "transfer" && bill_image != null && bill_image.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(bill_image.FileName);
                        var path = Path.Combine(Server.MapPath("~/uploads/bills/"), fileName);
                        bill_image.SaveAs(path);
                        billImageUrl = "~/uploads/bills/" + fileName;
                    }

                    var newOrder = new Order
                    {
                        UserId = int.Parse(IdentityExtensions.GetUserId(User.Identity)),
                        TotalAmount = totalAmount,
                        ShippingAddress = model.ShippingAddress,
                        PaymentMethod = model.PaymentMethod,
                        BillImageUrl = billImageUrl,
                        OrderDate = DateTime.Now,
                        Status = "pending",
                        voucher_code = appliedVoucherCode ?? "",
                        discount_amount = discountAmount
                    };
                    db.Orders.Add(newOrder);

                    foreach (var item in cart)
                    {
                        db.OrderItems.Add(new OrderItem
                        {
                            Order = newOrder,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            PriceAtOrder = item.Price
                        });
                    }

                    db.SaveChanges(); // Lưu Order và OrderItems

                    foreach (var item in cart)
                    {
                        db.Database.ExecuteSqlCommand(
                            "UPDATE Products SET StockQuantity = StockQuantity - @p0 WHERE ProductId = @p1",
                            item.Quantity, item.ProductId);
                    }

                    if (!string.IsNullOrEmpty(appliedVoucherCode))
                    {
                        var voucherUsed = db.Vouchers.FirstOrDefault(v => v.Code == appliedVoucherCode);
                        if (voucherUsed != null)
                        {
                            voucherUsed.UsageCount += 1;
                            db.Entry(voucherUsed).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }

                    transaction.Commit();

                    Session.Remove("Cart");
                    Session.Remove("VoucherCode");
                    Session.Remove("DiscountAmount");

                    TempData["Message"] = $"Đơn hàng của bạn đã được đặt thành công! Mã đơn hàng: {newOrder.OrderId}";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Message"] = "Có lỗi xảy ra khi xử lý đơn hàng: " + ex.Message + (ex.InnerException != null ? " -> Chi tiết: " + ex.InnerException.Message : "");
                    return RedirectToAction("Index");
                }
            }
        }
    }

    // POST: /Checkout/ApplyVoucher
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult ApplyVoucher(string voucherCode)
    {
        var cart = GetCart();
        decimal subtotal = cart.Sum(i => i.Subtotal);
        string message = "";
        decimal discountAmount = 0;
        bool success = false;

        using (var db = new PhoneStoreDBEntities()) // Đảm bảo đúng tên DbContext
        {
            if (string.IsNullOrWhiteSpace(voucherCode))
            {
                message = "Vui lòng nhập mã voucher.";
            }
            else
            {
                var voucher = db.Vouchers.FirstOrDefault(v => v.Code.Equals(voucherCode, StringComparison.OrdinalIgnoreCase));
                var user = db.Users.Find(int.Parse(IdentityExtensions.GetUserId(User.Identity)));
                bool isVip = "vip".Equals(user?.user_type, StringComparison.OrdinalIgnoreCase);

                if (voucher == null)
                {
                    message = "Mã voucher không tồn tại.";
                }
                else if (!voucher.IsActive)
                {
                    message = "Mã voucher đã bị vô hiệu hóa.";
                }
                else if (voucher.ExpiryDate.HasValue && voucher.ExpiryDate.Value < DateTime.Today)
                {
                    message = "Mã voucher đã hết hạn.";
                }
                else if (voucher.UsageCount >= voucher.UsageLimit)
                {
                    message = "Mã voucher đã hết lượt sử dụng.";
                }
                else if (voucher.VipOnly && !isVip)
                {
                    message = "Mã voucher này chỉ dành cho thành viên VIP.";
                }
                else
                {
                    success = true;
                    message = "Áp dụng voucher thành công!";

                    // === SỬA LỖI LOGIC QUAN TRỌNG TẠI ĐÂY ===
                    // Chúng ta phải so sánh với 'percentage' (đúng như trong CSDL)
                    // và sử dụng StringComparison.OrdinalIgnoreCase để đảm bảo nó luôn đúng
                    if ("percentage".Equals(voucher.DiscountType, StringComparison.OrdinalIgnoreCase))
                    {
                        // Đây là logic giảm %
                        discountAmount = subtotal * (voucher.DiscountValue / 100m);
                    }
                    else if ("fixed_amount".Equals(voucher.DiscountType, StringComparison.OrdinalIgnoreCase))
                    {
                        // Đây là logic giảm tiền mặt
                        discountAmount = voucher.DiscountValue;
                    }
                    // === KẾT THÚC SỬA LỖI ===

                    if (discountAmount > subtotal)
                    {
                        discountAmount = subtotal;
                    }

                    Session["VoucherCode"] = voucher.Code;
                    Session["DiscountAmount"] = discountAmount;
                }
            }
        } // DbContext được giải phóng

        return Json(new
        {
            success = success,
            message = message,
            discountAmount = discountAmount,
            formattedDiscount = string.Format("{0:N0} VNĐ", discountAmount),
            totalAmount = subtotal - discountAmount,
            formattedTotal = string.Format("{0:N0} VNĐ", subtotal - discountAmount)
        });
    }

    // POST: /Checkout/RemoveVoucher
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult RemoveVoucher()
    {
        Session.Remove("VoucherCode");
        Session.Remove("DiscountAmount");

        var cart = GetCart();
        decimal subtotal = cart.Sum(i => i.Subtotal);

        return Json(new
        {
            success = true,
            message = "Đã xóa voucher.",
            formattedDiscount = "0 VNĐ",
            totalAmount = subtotal,
            formattedTotal = string.Format("{0:N0} VNĐ", subtotal)
        });
    }

    // Không cần Dispose() nữa vì chúng ta đã dùng `using` ở mọi nơi
}