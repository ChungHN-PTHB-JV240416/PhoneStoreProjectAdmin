using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;

namespace PhoneStore_New.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // Hàm lấy UserID an toàn (Ưu tiên Session, fallback sang Identity)
        private int GetCurrentUserId()
        {
            if (Session["UserId"] != null) return Convert.ToInt32(Session["UserId"]);
            if (User.Identity.IsAuthenticated)
            {
                // Thử parse ID từ Name, nếu không được thì tìm trong DB
                if (int.TryParse(User.Identity.Name, out int uid)) return uid;

                var u = db.Users.FirstOrDefault(x => x.Username == User.Identity.Name);
                if (u != null) return u.UserId;
            }
            return 0;
        }

        // ==========================================
        // PHẦN 1: QUẢN LÝ THÔNG TIN CÁ NHÂN
        // ==========================================

        // GET: /Profile/Index
        public ActionResult Index(string message)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Logout", "Login");

            var user = db.Users.Find(userId);
            if (user == null) return RedirectToAction("Logout", "Login");

            var model = new ProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                AvatarUrl = user.AvatarUrl,
                Message = (TempData["Message"] as string) ?? message
            };

            return View(model);
        }

        // POST: /Profile/Index (Cập nhật thông tin & Avatar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(ProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userToUpdate = db.Users.Find(model.UserId);
                if (userToUpdate == null) return HttpNotFound();

                string newAvatarPath = userToUpdate.AvatarUrl;

                // Xử lý upload Avatar
                if (model.ImageFile != null && model.ImageFile.ContentLength > 0)
                {
                    var uploadDir = Server.MapPath("~/uploads/avatars/");
                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                    var path = Path.Combine(uploadDir, fileName);

                    model.ImageFile.SaveAs(path);
                    newAvatarPath = "~/uploads/avatars/" + fileName;

                    // Xóa ảnh cũ nếu có và khác ảnh mặc định
                    if (!string.IsNullOrEmpty(model.AvatarUrl) && model.AvatarUrl != newAvatarPath && !model.AvatarUrl.Contains("default"))
                    {
                        try
                        {
                            var oldFilePath = Server.MapPath(model.AvatarUrl);
                            if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                        }
                        catch { /* Bỏ qua lỗi xóa file */ }
                    }
                }

                userToUpdate.FirstName = model.FirstName;
                userToUpdate.LastName = model.LastName;
                userToUpdate.Email = model.Email;
                userToUpdate.PhoneNumber = model.PhoneNumber;
                userToUpdate.Address = model.Address;
                userToUpdate.AvatarUrl = newAvatarPath;

                db.Entry(userToUpdate).State = EntityState.Modified;
                db.SaveChanges();

                // Cập nhật lại Session
                Session["FirstName"] = userToUpdate.FirstName;

                TempData["Message"] = "Hồ sơ của bạn đã được cập nhật thành công!";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // ==========================================
        // PHẦN 2: HỘP THƯ HỖ TRỢ (TICKET SYSTEM)
        // ==========================================

        public ActionResult MyTickets()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Index", "Login");

            // Chỉ lấy phiếu của đúng User này
            var tickets = db.Contacts
                            .Where(c => c.UserId == userId)
                            .OrderByDescending(c => c.CreatedAt)
                            .ToList();
            return View(tickets);
        }

        // Chi tiết phiếu & Chat
        public ActionResult TicketDetail(int id)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Index", "Login");

            // BẢO MẬT: Kiểm tra phiếu này có phải của User này không
            var ticket = db.Contacts.FirstOrDefault(c => c.Id == id && c.UserId == userId);

            if (ticket == null) return HttpNotFound();

            ViewBag.Replies = db.ContactReplies
                                .Where(r => r.ContactId == id)
                                .OrderBy(r => r.CreatedAt)
                                .ToList();

            return View(ticket);
        }

        // User trả lời thêm vào phiếu
        [HttpPost]
        public ActionResult UserReply(int contactId, string message)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Index", "Login");

            // BẢO MẬT: Kiểm tra phiếu này có phải của User này không trước khi cho phép reply
            // (Tránh trường hợp hacker sửa contactId để spam phiếu người khác)
            var ticket = db.Contacts.FirstOrDefault(c => c.Id == contactId && c.UserId == userId);

            if (ticket == null) return HttpNotFound();

            if (!string.IsNullOrEmpty(message))
            {
                var reply = new ContactReply
                {
                    ContactId = contactId,
                    Message = message,
                    IsAdmin = false, // User trả lời
                    CreatedAt = DateTime.Now
                };
                db.ContactReplies.Add(reply);

                // Cập nhật trạng thái phiếu về "Chờ xử lý" (0) và "Chưa đọc" để Admin chú ý
                ticket.IsRead = false;
                ticket.Status = 0;

                db.SaveChanges();
            }

            return RedirectToAction("TicketDetail", new { id = contactId });
        }

        // ==========================================
        // PHẦN 3: ĐỔI MẬT KHẨU
        // ==========================================

        // GET: /Profile/ChangePassword
        public ActionResult ChangePassword()
        {
            if (GetCurrentUserId() == 0) return RedirectToAction("Logout", "Login");
            return View();
        }

        // POST: /Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Logout", "Login");

            var user = db.Users.Find(userId);
            if (user == null) return RedirectToAction("Logout", "Login");

            // 1. Kiểm tra mật khẩu cũ
            if (!System.Web.Helpers.Crypto.VerifyHashedPassword(user.PasswordHash, model.OldPassword))
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu hiện tại không chính xác.");
                return View(model);
            }

            // 2. Hash mật khẩu mới và lưu
            user.PasswordHash = System.Web.Helpers.Crypto.HashPassword(model.NewPassword);

            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();

            TempData["Message"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("ChangePassword");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}