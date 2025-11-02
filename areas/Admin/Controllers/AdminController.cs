using System;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Areas.Admin.Models.ViewModels;
using PhoneStore_New.Models;

using System.Collections.Generic;
using PhoneStore_New.Models.ViewModels; // Cần cho List

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // GET: Admin/Admin/Index (Dashboard)
        public ActionResult Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var viewModel = new DashboardViewModel
            {
                NewOrdersCount = db.Orders.Count(o => o.Status == "pending"),
                DailyRevenue = db.Orders
                    .Where(o => o.Status == "completed" && o.OrderDate >= today && o.OrderDate < tomorrow)
                    .Select(o => (decimal?)o.TotalAmount)
                    .Sum() ?? 0m,
                NewUsersCount = db.Users.Count(u => u.CreatedAt >= today && u.CreatedAt < tomorrow),
                LowStockProducts = db.Products
                    .Where(p => p.StockQuantity < 10)
                    .OrderBy(p => p.StockQuantity)
                    .Take(5)
                    .ToList()
            };

            return View(viewModel);
        }

        // GET: Admin/Admin/Users
        public ActionResult Users(string search_query, string message)
        {
            var usersQuery = db.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search_query))
            {
                search_query = search_query.Trim();
                usersQuery = usersQuery.Where(u =>
                    u.Username.Contains(search_query) ||
                    u.FirstName.Contains(search_query) ||
                    u.LastName.Contains(search_query) ||
                    u.Email.Contains(search_query));
            }

            var userList = usersQuery
                .OrderByDescending(u => u.UserId)
                .Select(u => new UserViewModel
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Role = u.Role,
                    UserType = u.user_type ?? "regular",
                    VipExpiryDate = u.vip_expiry_date
                })
                .ToList();

            var viewModel = new UserListViewModel
            {
                Users = userList,
                SearchQuery = search_query,
                Message = (TempData["Message"] as string) ?? message
            };

            return View(viewModel);
        }

        // POST: Admin/Admin/UpdateUser (ĐÃ SỬA LẠI HOÀN TOÀN)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateUser([Bind(Prefix = "Users")] List<UserViewModel> users, int index, string SearchQuery)
        {
            if (index < 0 || users == null || index >= users.Count || users[index] == null)
            {
                TempData["Message"] = "Lỗi: Dữ liệu gửi lên không hợp lệ.";
                return RedirectToAction("Users", new { search_query = SearchQuery });
            }

            var userVm = users[index];
            var userToUpdate = db.Users.Find(userVm.UserId);

            if (userToUpdate == null)
            {
                TempData["Message"] = "Lỗi: Không tìm thấy người dùng.";
                return RedirectToAction("Users", new { search_query = SearchQuery });
            }

            // Chỉ cập nhật các trường cơ bản, không đụng đến Role hay UserType
            userToUpdate.FirstName = userVm.FirstName?.Trim();
            userToUpdate.LastName = userVm.LastName?.Trim();
            userToUpdate.Email = userVm.Email?.Trim();

            try
            {
                db.SaveChanges();
                TempData["Message"] = $"Cập nhật thông tin cho \"{userToUpdate.Username}\" thành công!";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Lỗi khi lưu: " + ex.Message;
            }

            return RedirectToAction("Users", new { search_query = SearchQuery });
        }


        // GET: Admin/Admin/DeleteUser/5
        public ActionResult DeleteUser(int id)
        {
            var currentUserIdStr = User.Identity.GetUserId();
            if (!int.TryParse(currentUserIdStr, out int currentUserId))
            {
                TempData["Message"] = "Lỗi: Không xác định được người dùng hiện tại.";
                return RedirectToAction("Users");
            }

            if (id == currentUserId)
            {
                TempData["Message"] = "Lỗi: Bạn không thể xóa tài khoản của chính mình.";
                return RedirectToAction("Users");
            }

            var userToDelete = db.Users.Find(id);
            if (userToDelete == null)
            {
                TempData["Message"] = "Lỗi: Không tìm thấy người dùng để xóa.";
                return RedirectToAction("Users");
            }

            try
            {
                db.Users.Remove(userToDelete);
                db.SaveChanges();
                TempData["Message"] = $"Đã xóa người dùng \"{userToDelete.Username}\" thành công.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Lỗi khi xóa: " + ex.Message;
            }

            return RedirectToAction("Users");
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