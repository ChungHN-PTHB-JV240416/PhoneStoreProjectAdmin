using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using PhoneStore_New.Areas.Admin.Models.ViewModels;
using PhoneStore_New.Models;
using System;
using PhoneStore_New.Models.ViewModels;

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class OrderAdminController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // GET: Admin/Order/Index
        public ActionResult Index(string message)
        {
            var orders = db.Orders
                            .Include(o => o.User)
                            .OrderByDescending(o => o.OrderDate)
                            .Select(o => new OrderAdminViewModel
                            {
                                OrderId = o.OrderId,
                                OrderDate = o.OrderDate,
                                TotalAmount = o.TotalAmount,
                                Status = o.Status,
                                PaymentMethod = o.PaymentMethod,
                                CustomerName = o.User.FirstName + " " + o.User.LastName,
                                Username = o.User.Username
                            })
                            .ToList();

            var viewModel = new OrderAdminListViewModel
            {
                Orders = orders,
                Message = (TempData["Message"] as string) ?? message
            };

            return View(viewModel);
        }

        // GET: Admin/Order/Detail/5
        public ActionResult Detail(int id)
        {
            var order = db.Orders
                          .Include(o => o.User)
                          .Include(o => o.OrderItems.Select(oi => oi.Product))
                          .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Message"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index");
            }

            return View(order);
        }

        // POST: Admin/Order/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int order_id, string status)
        {
            var order = db.Orders.Find(order_id);

            if (order != null)
            {
                order.Status = status;
                db.Entry(order).State = EntityState.Modified;

                // === LOGIC TỰ ĐỘNG NÂNG CẤP VIP ===
                if (status == "completed")
                {
                    var userId = order.UserId;
                    var userToUpdate = db.Users.Find(userId);

                    if (userToUpdate != null && userToUpdate.user_type != "vip")
                    {
                        var today = DateTime.Now;
                        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                        decimal totalSpentInMonth = db.Orders
                            .Where(o => o.UserId == userId &&
                                        o.Status == "completed" &&
                                        o.OrderDate >= firstDayOfMonth &&
                                        o.OrderDate <= lastDayOfMonth)
                            .Select(o => (decimal?)o.TotalAmount)
                            .Sum() ?? 0m;

                        if (totalSpentInMonth >= 100000000m) // 100 triệu
                        {
                            userToUpdate.user_type = "vip";
                            userToUpdate.vip_expiry_date = DateTime.Now.AddMonths(1);
                            db.Entry(userToUpdate).State = EntityState.Modified;
                        }
                    }
                }
                // === KẾT THÚC LOGIC ===

                try
                {
                    db.SaveChanges();
                    TempData["Message"] = $"Trạng thái đơn hàng #{order_id} đã được cập nhật thành công.";
                }
                catch (Exception ex)
                {
                    TempData["Message"] = "Có lỗi xảy ra khi lưu: " + ex.Message;
                }
            }
            else
            {
                TempData["Message"] = "Lỗi: Không tìm thấy đơn hàng.";
            }

            return RedirectToAction("Index");
        }
    }
}