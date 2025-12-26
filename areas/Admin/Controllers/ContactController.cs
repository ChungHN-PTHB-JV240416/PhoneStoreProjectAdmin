using System;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models;

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class ContactController : Controller
    {
        private PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // 1. Danh sách yêu cầu hỗ trợ
        public ActionResult Index()
        {
            var items = db.Contacts.OrderByDescending(c => c.CreatedAt).ToList();
            return View(items);
        }

        // 2. Xem chi tiết & Load lịch sử chat
        public ActionResult Detail(int id)
        {
            var item = db.Contacts.Find(id);

            if (item != null)
            {
                // Đánh dấu đã đọc nếu chưa đọc
                if (!item.IsRead)
                {
                    item.IsRead = true;
                    db.SaveChanges();
                }

                // --- QUAN TRỌNG: Lấy lịch sử chat để hiển thị ra View ---
                // (Nếu không có dòng này, View sẽ báo lỗi hoặc không hiện tin nhắn)
                ViewBag.Replies = db.ContactReplies
                                    .Where(r => r.ContactId == id)
                                    .OrderBy(r => r.CreatedAt)
                                    .ToList();

                return View(item);
            }
            return RedirectToAction("Index");
        }

        // 3. Xử lý Admin trả lời (Action này MỚI THÊM)
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Reply(int contactId, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                TempData["Error"] = "Nội dung không được để trống";
                return RedirectToAction("Detail", new { id = contactId });
            }

            try
            {
                // Tạo tin nhắn trả lời mới
                var reply = new ContactReply
                {
                    ContactId = contactId,
                    Message = message,
                    IsAdmin = true, // Đánh dấu là Admin trả lời
                    CreatedAt = DateTime.Now
                };

                db.ContactReplies.Add(reply);

                // Cập nhật trạng thái phiếu: Từ "Chờ xử lý" (0) -> "Đang hỗ trợ" (1)
                var ticket = db.Contacts.Find(contactId);
                if (ticket != null && ticket.Status == 0)
                {
                    ticket.Status = 1;
                }

                db.SaveChanges();
                TempData["Message"] = "Đã gửi phản hồi thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            // Quay lại trang chi tiết để xem tin nhắn vừa gửi
            return RedirectToAction("Detail", new { id = contactId });
        }

        // 4. Xóa yêu cầu
        public ActionResult Delete(int id)
        {
            var item = db.Contacts.Find(id);
            if (item != null)
            {
                // Nếu xóa phiếu cha, Entity Framework thường sẽ tự xóa các tin nhắn con (ContactReplies)
                // nếu bạn đã cấu hình Cascade Delete trong Database.
                // Nếu chưa, bạn cần xóa thủ công:
                var replies = db.ContactReplies.Where(r => r.ContactId == id);
                db.ContactReplies.RemoveRange(replies);

                db.Contacts.Remove(item);
                db.SaveChanges();
                TempData["Message"] = "Đã xóa phiếu hỗ trợ.";
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}