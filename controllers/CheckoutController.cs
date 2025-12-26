using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;

namespace PhoneStore_New.Controllers
{
    public class CheckoutController : Controller
    {
        private List<CartItem> GetCart()
        {
            var cart = Session["Cart"] as List<CartItem>;
            return cart ?? new List<CartItem>();
        }

        private int GetCurrentUserId()
        {
            if (Session["UserId"] != null) return Convert.ToInt32(Session["UserId"]);

            // Fallback: Cố gắng lấy ID từ User.Identity nếu Session bị mất
            if (User.Identity.IsAuthenticated && !string.IsNullOrEmpty(User.Identity.Name))
            {
                if (int.TryParse(User.Identity.Name, out int uid))
                {
                    Session["UserId"] = uid;
                    return uid;
                }
                using (var db = new PhoneStoreDBEntities())
                {
                    var u = db.Users.FirstOrDefault(x => x.Username == User.Identity.Name);
                    if (u != null) return u.UserId;
                }
            }
            return 0;
        }

        // GET: /Checkout/Index
        public ActionResult Index(string message)
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index", "Cart");

            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Index", "Login", new { returnUrl = "/Checkout/Index" });

            using (var db = new PhoneStoreDBEntities())
            {
                var user = db.Users.Find(userId);
                var qrUrl = db.Settings.FirstOrDefault(s => s.SettingKey == "qr_code_url")?.SettingValue;

                decimal subtotal = cart.Sum(i => i.Subtotal);
                string appliedVoucherCode = Session["VoucherCode"] as string;
                decimal discountAmount = (decimal)(Session["DiscountAmount"] ?? 0m);
                decimal totalAmount = subtotal - discountAmount;
                if (totalAmount < 0) totalAmount = 0;

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

        // POST: /Checkout/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(CheckoutViewModel model, HttpPostedFileBase bill_image)
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index", "Home");

            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Index", "Login", new { returnUrl = "/Checkout/Index" });

            using (var db = new PhoneStoreDBEntities())
            {
                decimal subtotal = cart.Sum(i => i.Subtotal);
                string appliedVoucherCode = Session["VoucherCode"] as string;
                decimal discountAmount = (decimal)(Session["DiscountAmount"] ?? 0m);
                decimal totalAmount = subtotal - discountAmount;
                if (totalAmount < 0) totalAmount = 0;

                // Cập nhật lại model để hiển thị nếu có lỗi validate
                model.CartItems = cart;
                model.TotalAmount = totalAmount;
                model.SubtotalAmount = subtotal;
                model.DiscountAmount = discountAmount;
                model.QrCodeUrl = db.Settings.FirstOrDefault(s => s.SettingKey == "qr_code_url")?.SettingValue;

                if (!ModelState.IsValid) return View(model);

                // 1. Kiểm tra tồn kho lần cuối
                foreach (var item in cart)
                {
                    var productInDb = db.Products.AsNoTracking().FirstOrDefault(p => p.ProductId == item.ProductId);
                    if (productInDb == null || productInDb.StockQuantity < item.Quantity)
                    {
                        TempData["Message"] = $"Sản phẩm '{item.Name}' đã hết hàng hoặc không đủ số lượng.";
                        return RedirectToAction("Index", "Cart");
                    }
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 2. Xử lý ảnh bill (nếu chuyển khoản)
                        string billImageUrl = null;
                        if (model.PaymentMethod == "transfer" && bill_image != null && bill_image.ContentLength > 0)
                        {
                            var fileName = Guid.NewGuid() + Path.GetExtension(bill_image.FileName);
                            var path = Path.Combine(Server.MapPath("~/uploads/bills/"), fileName);
                            var folder = Server.MapPath("~/uploads/bills/");
                            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                            bill_image.SaveAs(path);
                            billImageUrl = "/uploads/bills/" + fileName;
                        }

                        // 3. Tạo đơn hàng
                        var newOrder = new Order
                        {
                            UserId = userId,
                            TotalAmount = totalAmount,
                            ShippingAddress = model.ShippingAddress,
                            PaymentMethod = model.PaymentMethod,
                            BillImageUrl = billImageUrl,
                            OrderDate = DateTime.Now,
                            Status = "pending", // Mặc định là chờ xử lý
                            voucher_code = appliedVoucherCode ?? "",
                            discount_amount = discountAmount
                        };
                        db.Orders.Add(newOrder);
                        db.SaveChanges(); // Lưu để lấy OrderId

                        // 4. Lưu chi tiết đơn hàng & Trừ kho
                        foreach (var item in cart)
                        {
                            db.OrderItems.Add(new OrderItem
                            {
                                OrderId = newOrder.OrderId,
                                ProductId = item.ProductId,
                                Quantity = item.Quantity,
                                PriceAtOrder = item.Price
                            });
                            // Trừ tồn kho trực tiếp bằng SQL cho nhanh
                            db.Database.ExecuteSqlCommand("UPDATE Products SET StockQuantity = StockQuantity - @p0 WHERE ProductId = @p1", item.Quantity, item.ProductId);
                        }

                        // 5. Cập nhật lượt dùng Voucher
                        if (!string.IsNullOrEmpty(appliedVoucherCode))
                        {
                            var v = db.Vouchers.FirstOrDefault(vc => vc.Code == appliedVoucherCode);
                            if (v != null) { v.UsageCount += 1; db.Entry(v).State = EntityState.Modified; }
                        }

                        // 6. Xóa giỏ hàng cũ trong Database (nếu có)
                        var dbCartItems = db.Carts.Where(c => c.UserId == userId).ToList();
                        if (dbCartItems.Any()) db.Carts.RemoveRange(dbCartItems);

                        db.SaveChanges();
                        transaction.Commit();

                        // ========================================================
                        // QUAN TRỌNG: LOGIC XỬ LÝ SESSION GIỎ HÀNG
                        // ========================================================

                        // Nếu là VNPAY: CHƯA XÓA SESSION GIỎ HÀNG VỘI (Để lỡ thanh toán lỗi còn quay lại được)
                        if (model.PaymentMethod == "vnpay")
                        {
                            return RedirectToAction("PayWithVnpay", "Order", new { id = newOrder.OrderId });
                        }

                        // Nếu là COD hoặc Chuyển khoản: XÓA LUÔN
                        Session.Remove("Cart");
                        Session.Remove("VoucherCode");
                        Session.Remove("DiscountAmount");

                        TempData["Message"] = $"Đặt hàng thành công! Mã đơn: #{newOrder.OrderId}";
                        return RedirectToAction("Index", "Home");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        TempData["Message"] = "Lỗi xử lý đơn hàng: " + ex.Message;
                        return View(model);
                    }
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApplyVoucher(string voucherCode)
        {
            var cart = GetCart();
            if (!cart.Any()) return Json(new { success = false, message = "Giỏ hàng trống." });
            decimal subtotal = cart.Sum(i => i.Subtotal);
            string message = ""; decimal discountAmount = 0; bool success = false;

            using (var db = new PhoneStoreDBEntities())
            {
                if (string.IsNullOrWhiteSpace(voucherCode)) return Json(new { success = false, message = "Vui lòng nhập mã." });
                var voucher = db.Vouchers.FirstOrDefault(v => v.Code == voucherCode);
                var userId = GetCurrentUserId();
                var user = db.Users.Find(userId);
                bool isVip = "vip".Equals(user?.user_type, StringComparison.OrdinalIgnoreCase);

                if (voucher == null || !voucher.IsActive) message = "Mã không hợp lệ.";
                else if (voucher.ExpiryDate.HasValue && voucher.ExpiryDate < DateTime.Today) message = "Mã hết hạn.";
                else if (voucher.UsageCount >= voucher.UsageLimit) message = "Mã hết lượt.";
                else if (voucher.VipOnly && !isVip) message = "Mã chỉ dành cho VIP.";
                else
                {
                    success = true; message = "Áp dụng thành công!";
                    discountAmount = "percentage".Equals(voucher.DiscountType, StringComparison.OrdinalIgnoreCase)
                        ? subtotal * (voucher.DiscountValue / 100m) : voucher.DiscountValue;
                    if (discountAmount > subtotal) discountAmount = subtotal;
                    Session["VoucherCode"] = voucher.Code; Session["DiscountAmount"] = discountAmount;
                }
            }
            return Json(new { success, message, formattedDiscount = $"{discountAmount:N0} VNĐ", formattedTotal = $"{subtotal - discountAmount:N0} VNĐ" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveVoucher()
        {
            Session.Remove("VoucherCode"); Session.Remove("DiscountAmount");
            return Json(new { success = true, message = "Đã xóa mã.", formattedDiscount = "0 VNĐ", formattedTotal = $"{GetCart().Sum(i => i.Subtotal):N0} VNĐ" });
        }
    }
}