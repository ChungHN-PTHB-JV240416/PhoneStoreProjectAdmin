using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models; // Đảm bảo đúng namespace
using PhoneStore_New.Models.ViewModels; // Đảm bảo đúng namespace

namespace PhoneStore_New.Controllers // Đảm bảo đúng namespace
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities(); // Đảm bảo đúng tên DbContext

        public ActionResult History(string message)
        {
            var userId = int.Parse(IdentityExtensions.GetUserId(User.Identity));
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

        // === SỬA ĐỔI QUAN TRỌNG: Đổi "int id" thành "int? id" ===
        public ActionResult Detail(int? id)
        {
            // 1. Kiểm tra xem ID có bị null hay không
            if (id == null)
            {
                // Nếu không có ID, không thể xem chi tiết, quay về trang lịch sử
                TempData["Message"] = "Lỗi: ID đơn hàng không hợp lệ.";
                return RedirectToAction("History");
            }

            // 2. Nếu có ID, tiếp tục xử lý như bình thường
            var userId = int.Parse(IdentityExtensions.GetUserId(User.Identity));
            var order = db.Orders
                          .Include(o => o.OrderItems.Select(oi => oi.Product))
                          // 3. Dùng id.Value vì id bây giờ là một nullable int
                          .FirstOrDefault(o => o.OrderId == id.Value && o.UserId == userId);

            if (order == null)
            {
                return HttpNotFound();
            }

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

        public ActionResult Cancel(int id)
        {
            var userId = int.Parse(IdentityExtensions.GetUserId(User.Identity));
            var order = db.Orders.FirstOrDefault(o => o.OrderId == id && o.UserId == userId);
            if (order != null && order.Status == "pending")
            {
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}