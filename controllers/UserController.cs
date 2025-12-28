using System;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models;
using System.Data.Entity;

namespace PhoneStore_New.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        private int GetCurrentUserId()
        {
            if (Session["UserId"] != null) return (int)Session["UserId"];

            if (User.Identity.IsAuthenticated)
            {
                var username = User.Identity.Name;
                var user = db.Users.FirstOrDefault(u => u.Username == username);
                if (user != null)
                {
                    Session["UserId"] = user.UserId;
                    Session["UserType"] = user.user_type;
                    return user.UserId;
                }
            }
            return 0;
        }

        // =========================================================
        public new ActionResult Profile()
        {
            int userId = GetCurrentUserId();
            // --- SỬA LỖI REDIRECT: Trỏ về Index của LoginController ---
            if (userId == 0) return RedirectToAction("Index", "Login");

            var userModel = db.Users.Find(userId);
            return View(userModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(PhoneStore_New.Models.User model)
        {
            int userId = GetCurrentUserId();
            var userInDb = db.Users.Find(userId);

            if (userInDb != null)
            {
                userInDb.FirstName = model.FirstName;
                userInDb.LastName = model.LastName;
                userInDb.PhoneNumber = model.PhoneNumber;
                userInDb.Address = model.Address;

                db.SaveChanges();
                TempData["Message"] = "Cập nhật thông tin thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy thông tin.";
            }

            return RedirectToAction("Profile");
        }

        public ActionResult OrderHistory()
        {
            int userId = GetCurrentUserId();
            // --- SỬA LỖI REDIRECT ---
            if (userId == 0) return RedirectToAction("Index", "Login");

            var orders = db.Orders
                           .Where(o => o.UserId == userId)
                           .OrderByDescending(o => o.OrderDate)
                           .ToList();
            return View(orders);
        }

        public ActionResult OrderDetails(int id)
        {
            int userId = GetCurrentUserId();
            // --- SỬA LỖI REDIRECT ---
            if (userId == 0) return RedirectToAction("Index", "Login");

            var order = db.Orders.FirstOrDefault(o => o.OrderId == id && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("OrderHistory");
            }
            return View(order);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}