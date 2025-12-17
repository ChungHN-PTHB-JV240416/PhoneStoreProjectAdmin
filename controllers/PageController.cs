using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models; // Đảm bảo đúng namespace

namespace PhoneStore_New.Controllers
{
    public class PageController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities(); // Đảm bảo đúng tên DbContext

        // GET: Page/View/{slug}
        // Ví dụ: /Page/View/ve-chung-toi
        public ActionResult View(string id) // 'id' ở đây chính là 'slug'
        {
            if (string.IsNullOrEmpty(id))
            {
                return HttpNotFound();
            }

            var page = db.Pages.FirstOrDefault(p => p.Slug == id && p.IsPublished);

            if (page == null)
            {
                // Nếu không tìm thấy trang, hoặc trang chưa được publish
                return HttpNotFound();
            }

            // Gửi toàn bộ đối tượng 'Page' cho View
            return View(page);
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