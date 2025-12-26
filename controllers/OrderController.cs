using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;
using System.Configuration;

namespace PhoneStore_New.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        private int GetCurrentUserId()
        {
            if (Session["UserId"] != null) return Convert.ToInt32(Session["UserId"]);

            if (User.Identity.IsAuthenticated)
            {
                int uid;
                if (int.TryParse(User.Identity.Name, out uid)) return uid;
                var user = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
                if (user != null) return user.UserId;
            }
            return 0;
        }

        // 1. Lịch sử đơn hàng
        public ActionResult History(string message)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Index", "Login");

            var orders = db.Orders
                            .Where(o => o.UserId == userId)
                            .OrderByDescending(o => o.OrderDate)
                            .Select(o => new OrderHistoryItemViewModel
                            {
                                OrderId = o.OrderId,
                                OrderDate = o.OrderDate,
                                TotalAmount = o.TotalAmount,
                                Status = o.Status
                            })
                            .ToList();
            var viewModel = new OrderHistoryViewModel { Orders = orders, Message = (TempData["Message"] as string) ?? message };
            return View(viewModel);
        }

        // 2. Chi tiết đơn hàng
        public ActionResult Detail(int? id)
        {
            if (id == null)
            {
                TempData["Message"] = "Lỗi: ID đơn hàng không hợp lệ.";
                return RedirectToAction("History");
            }

            var userId = GetCurrentUserId();
            var order = db.Orders
                          .Include(o => o.OrderItems.Select(oi => oi.Product))
                          .FirstOrDefault(o => o.OrderId == id.Value && o.UserId == userId);

            if (order == null) return HttpNotFound();

            var viewModel = new OrderDetailViewModel
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                ShippingAddress = order.ShippingAddress,
                PaymentMethod = order.PaymentMethod,
                Items = order.OrderItems.Select(oi => new OrderItemDetailViewModel
                {
                    ProductName = oi.Product?.Name ?? "[Sản phẩm đã bị xóa]",
                    Quantity = oi.Quantity,
                    PriceAtOrder = oi.PriceAtOrder
                }).ToList()
            };

            return View(viewModel);
        }

        // 3. Hủy đơn hàng
        public ActionResult Cancel(int id)
        {
            var userId = GetCurrentUserId();
            var order = db.Orders.FirstOrDefault(o => o.OrderId == id && o.UserId == userId);

            if (order != null && order.Status == "pending")
            {
                // Hoàn lại tồn kho
                foreach (var item in order.OrderItems)
                {
                    var product = db.Products.Find(item.ProductId);
                    if (product != null) product.StockQuantity += item.Quantity;
                }

                order.Status = "cancelled";
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Message"] = $"Đơn hàng #{id} đã được hủy thành công.";
            }
            else
            {
                TempData["Message"] = "Không thể hủy đơn hàng này.";
            }
            return RedirectToAction("History");
        }

        // =========================================================================
        // 4. CHỨC NĂNG THANH TOÁN VNPAY & CẬP NHẬT THANH TOÁN
        // =========================================================================

        [HttpPost]
        public ActionResult UpdatePaymentMethod(int orderId, string paymentMethod)
        {
            var userId = GetCurrentUserId();
            var order = db.Orders.FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null || order.Status != "pending")
            {
                TempData["Message"] = "Không thể cập nhật đơn hàng này.";
                return RedirectToAction("Detail", new { id = orderId });
            }

            // 1. Cập nhật phương thức thanh toán mới
            order.PaymentMethod = paymentMethod;
            db.SaveChanges();

            // 2. Điều hướng xử lý tiếp theo
            if (paymentMethod == "vnpay")
            {
                // Nếu chọn VNPAY -> Chuyển sang trang thanh toán (Giữ nguyên Session Cart phòng khi lỗi)
                return RedirectToAction("PayWithVnpay", new { id = orderId });
            }
            else
            {
                // Nếu chọn COD -> Xóa Session giỏ hàng ngay lập tức (Vì coi như đã chốt đơn thành công)
                Session.Remove("Cart");
                Session.Remove("VoucherCode");
                Session.Remove("DiscountAmount");

                TempData["Message"] = "Đã chuyển sang thanh toán tiền mặt (COD). Đơn hàng đã được xác nhận.";
                return RedirectToAction("Detail", new { id = orderId });
            }
        }

        public ActionResult PayWithVnpay(int id)
        {
            var userId = GetCurrentUserId();
            var order = db.Orders.FirstOrDefault(o => o.OrderId == id && o.UserId == userId);

            if (order == null || order.Status == "paid" || order.Status == "cancelled")
                return RedirectToAction("Detail", new { id = id });

            string vnp_Url = ConfigurationManager.AppSettings["VnpayUrl"];
            string vnp_TmnCode = ConfigurationManager.AppSettings["VnpayTmnCode"];
            string vnp_HashSecret = ConfigurationManager.AppSettings["VnpayHashSecret"];
            string vnp_Returnurl = ConfigurationManager.AppSettings["VnpayReturnUrl"];

            if (string.IsNullOrEmpty(vnp_TmnCode) || string.IsNullOrEmpty(vnp_HashSecret))
            {
                TempData["Message"] = "Lỗi cấu hình VNPAY.";
                return RedirectToAction("Detail", new { id = id });
            }

            VnPayLibrary vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);

            long amount = (long)(order.TotalAmount * 100);
            vnpay.AddRequestData("vnp_Amount", amount.ToString());

            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");

            vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1");

            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + order.OrderId);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", order.OrderId.ToString());

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return Redirect(paymentUrl);
        }

        [AllowAnonymous]
        public ActionResult PaymentCallback()
        {
            if (Request.QueryString.Count > 0)
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["VnpayHashSecret"];
                var vnpayData = Request.QueryString;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (string s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s, vnpayData[s]);
                    }
                }

                string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];
                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

                if (checkSignature)
                {
                    string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                    string vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
                    int orderId = int.Parse(vnp_TxnRef);

                    if (vnp_ResponseCode == "00")
                    {
                        var order = db.Orders.Find(orderId);
                        if (order != null)
                        {
                            order.Status = "paid";
                            db.SaveChanges();

                            Session.Remove("Cart");
                            Session.Remove("VoucherCode");
                            Session.Remove("DiscountAmount");

                            TempData["Message"] = "Thanh toán VNPAY thành công!";
                        }
                    }
                    else
                    {
                        TempData["Message"] = "Thanh toán thất bại. Mã lỗi: " + vnp_ResponseCode;
                    }
                    return RedirectToAction("Detail", "Order", new { id = orderId });
                }
                else
                {
                    TempData["Message"] = "Lỗi bảo mật: Chữ ký VNPAY không hợp lệ.";
                }
            }
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}