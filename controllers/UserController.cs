using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models;
using System.Data.Entity;

namespace PhoneStore_New.Controllers
{
    [Authorize] // Bắt buộc đăng nhập mới vào được các trang này
    public class UserController : Controller
    {
        private PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // Lấy ID User đang đăng nhập (Giả sử bạn dùng email làm username)
        private int GetCurrentUserId()
        {
            var email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == email || u.Email == email);
            return user != null ? user.UserId : 0;
        }

        // 1. TRANG THÔNG TIN CÁ NHÂN
        public ActionResult Profile()
        {
            int userId = GetCurrentUserId();
            var user = db.Users.Find(userId);
            return View(user);
        }

        [HttpPost]
        public ActionResult UpdateProfile(User model)
        {
            int userId = GetCurrentUserId();
            var user = db.Users.Find(userId);
            if (user != null)
            {
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;
                db.SaveChanges();
                TempData["Message"] = "Cập nhật thông tin thành công!";
            }
            return RedirectToAction("Profile");
        }

        // 2. TRANG LỊCH SỬ ĐƠN HÀNG
        public ActionResult OrderHistory()
        {
            int userId = GetCurrentUserId();
            var orders = db.Orders.Where(o => o.UserId == userId)
                                  .OrderByDescending(o => o.OrderDate)
                                  .ToList();
            return View(orders);
        }
    }
}