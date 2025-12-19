using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models;
using PhoneStore_New.Areas.Admin.Models.ViewModels;

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // =========================================================
        // 1. DASHBOARD (Trang chủ Admin) - ĐÃ SỬA LỖI NULL MODEL
        // =========================================================
        public ActionResult Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Tạo ViewModel chứa dữ liệu thống kê
            var viewModel = new DashboardViewModel
            {
                // Đếm đơn hàng đang chờ xử lý
                NewOrdersCount = db.Orders.Count(o => o.Status == "pending"),

                // Tính tổng doanh thu hôm nay (Completed)
                DailyRevenue = db.Orders
                    .Where(o => o.Status == "completed" && o.OrderDate >= today && o.OrderDate < tomorrow)
                    .Select(o => (decimal?)o.TotalAmount)
                    .Sum() ?? 0m,

                // Đếm thành viên mới đăng ký hôm nay
                NewUsersCount = db.Users.Count(u => u.CreatedAt >= today && u.CreatedAt < tomorrow),

                // Lấy top 5 sản phẩm sắp hết hàng (< 10)
                LowStockProducts = db.Products
                    .Where(p => p.StockQuantity < 10)
                    .OrderBy(p => p.StockQuantity)
                    .Take(5)
                    .ToList()
            };

            // [QUAN TRỌNG]: Phải truyền viewModel vào đây thì View mới hiển thị được
            return View(viewModel);
        }

        // =========================================================
        // 2. DANH SÁCH NGƯỜI DÙNG (Quản lý User)
        // =========================================================
        public ActionResult Users(string search_query, string message)
        {
            var usersQuery = db.Users.AsQueryable();

            // Xử lý tìm kiếm
            if (!string.IsNullOrEmpty(search_query))
            {
                search_query = search_query.Trim();
                usersQuery = usersQuery.Where(u =>
                    u.Username.Contains(search_query) ||
                    u.FirstName.Contains(search_query) ||
                    u.LastName.Contains(search_query) ||
                    u.Email.Contains(search_query));
            }

            // Map dữ liệu từ DB sang ViewModel
            var userList = usersQuery
                .OrderByDescending(u => u.UserId)
                .ToList() // Lấy dữ liệu về trước khi Select để tránh lỗi LINQ với hàm custom
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

        // =========================================================
        // 3. CẬP NHẬT NGƯỜI DÙNG (Lưu trực tiếp trên bảng)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateUser([Bind(Prefix = "Users")] List<UserViewModel> users, int index, string SearchQuery)
        {
            // Kiểm tra dữ liệu đầu vào
            if (index < 0 || users == null || index >= users.Count || users[index] == null)
            {
                TempData["Message"] = "Lỗi: Dữ liệu không hợp lệ.";
                return RedirectToAction("Users", new { search_query = SearchQuery });
            }

            var userVm = users[index];
            var userToUpdate = db.Users.Find(userVm.UserId);

            if (userToUpdate == null)
            {
                TempData["Message"] = "Lỗi: Không tìm thấy người dùng.";
                return RedirectToAction("Users", new { search_query = SearchQuery });
            }

            // Cập nhật thông tin cơ bản
            userToUpdate.FirstName = userVm.FirstName?.Trim();
            userToUpdate.LastName = userVm.LastName?.Trim();
            userToUpdate.Email = userVm.Email?.Trim();

            // Cập nhật Quyền và Loại tài khoản (Admin/User, VIP/Regular)
            userToUpdate.Role = userVm.Role;
            userToUpdate.user_type = userVm.UserType;
            userToUpdate.vip_expiry_date = userVm.VipExpiryDate;

            try
            {
                db.SaveChanges();
                TempData["Message"] = $"Cập nhật thành công cho tài khoản: {userToUpdate.Username}";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Lỗi khi lưu: " + ex.Message;
            }

            return RedirectToAction("Users", new { search_query = SearchQuery });
        }

        // =========================================================
        // 4. XÓA NGƯỜI DÙNG
        // =========================================================
        public ActionResult DeleteUser(int id)
        {
            // Lấy ID người đang đăng nhập để tránh tự xóa mình
            int currentUserId = 0;
            if (Session["UserId"] != null)
            {
                currentUserId = (int)Session["UserId"];
            }
            else if (User.Identity.IsAuthenticated && int.TryParse(User.Identity.Name, out int uid))
            {
                currentUserId = uid;
            }

            if (id == currentUserId)
            {
                TempData["Message"] = "Lỗi: Bạn không thể tự xóa tài khoản của mình.";
                return RedirectToAction("Users");
            }

            var userToDelete = db.Users.Find(id);
            if (userToDelete == null)
            {
                TempData["Message"] = "Lỗi: Người dùng không tồn tại.";
                return RedirectToAction("Users");
            }

            try
            {
                db.Users.Remove(userToDelete);
                db.SaveChanges();
                TempData["Message"] = $"Đã xóa thành công tài khoản: {userToDelete.Username}";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Lỗi khi xóa (có thể do ràng buộc dữ liệu đơn hàng): " + ex.Message;
            }

            return RedirectToAction("Users");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}