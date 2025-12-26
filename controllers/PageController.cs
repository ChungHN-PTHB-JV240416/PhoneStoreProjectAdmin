using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models;

namespace PhoneStore_New.Controllers
{
    public class PageController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // URL sẽ là: /Page/PageShow/{slug}
        public ActionResult PageShow(string id) // id nhận vào là slug
        {
            if (string.IsNullOrEmpty(id))
            {
                return HttpNotFound();
            }

            var page = db.Pages.FirstOrDefault(p => p.Slug == id && p.IsPublished);

            if (page == null)
            {
                return HttpNotFound();
            }

            // Vẫn dùng file View cũ là "Show.cshtml" để đỡ phải đổi tên file
            return View("Show", page);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}