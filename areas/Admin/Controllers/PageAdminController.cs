using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Areas.Admin.Models.ViewModels; // Đảm bảo using đúng
using PhoneStore_New.Models; // Đảm bảo using đúng

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class PageAdminController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities(); // Đảm bảo đúng tên DbContext

        // GET: Admin/PageAdmin (Hiển thị danh sách các trang)
        public ActionResult Index(string message)
        {
            ViewBag.Message = (TempData["Message"] as string) ?? message;
            var pages = db.Pages
                          .OrderBy(p => p.Title)
                          .Select(p => new PageViewModel
                          {
                              PageId = p.PageId,
                              Title = p.Title,
                              Slug = p.Slug,
                              IsPublished = p.IsPublished
                          })
                          .ToList();

            return View(pages);
        }

        // GET: Admin/PageAdmin/Create (Hiển thị form tạo mới)
        public ActionResult Create()
        {
            var viewModel = new PageViewModel
            {
                IsPublished = true // Mặc định là hiển thị
            };
            return View(viewModel);
        }

        // POST: Admin/PageAdmin/Create (Xử lý tạo mới)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)] // Cho phép nhập HTML vào trường Content
        public ActionResult Create(PageViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem Slug (URL) đã tồn tại chưa
                if (db.Pages.Any(p => p.Slug == model.Slug))
                {
                    ModelState.AddModelError("Slug", "Đường dẫn (slug) này đã tồn tại. Vui lòng chọn đường dẫn khác.");
                    return View(model);
                }

                var newPage = new Page
                {
                    Title = model.Title,
                    Slug = model.Slug.ToLower().Replace(" ", "-"), // Tự động dọn dẹp URL
                    Content = model.Content,
                    IsPublished = model.IsPublished,
                    CreatedAt = DateTime.Now
                };

                db.Pages.Add(newPage);
                db.SaveChanges();

                TempData["Message"] = "Đã tạo trang mới thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // GET: Admin/PageAdmin/Edit/5 (Hiển thị form sửa trang)
        public ActionResult Edit(int id)
        {
            var page = db.Pages.Find(id);
            if (page == null)
            {
                return HttpNotFound();
            }

            var viewModel = new PageViewModel
            {
                PageId = page.PageId,
                Title = page.Title,
                Slug = page.Slug,
                Content = page.Content,
                IsPublished = page.IsPublished
            };

            return View(viewModel);
        }

        // POST: Admin/PageAdmin/Edit/5 (Xử lý sửa trang)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)] // Cho phép nhập HTML vào trường Content
        public ActionResult Edit(PageViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng Slug (trừ chính nó)
                if (db.Pages.Any(p => p.Slug == model.Slug && p.PageId != model.PageId))
                {
                    ModelState.AddModelError("Slug", "Đường dẫn (slug) này đã tồn tại. Vui lòng chọn đường dẫn khác.");
                    return View(model);
                }

                var pageToUpdate = db.Pages.Find(model.PageId);
                if (pageToUpdate == null)
                {
                    return HttpNotFound();
                }

                pageToUpdate.Title = model.Title;
                pageToUpdate.Slug = model.Slug.ToLower().Replace(" ", "-");
                pageToUpdate.Content = model.Content;
                pageToUpdate.IsPublished = model.IsPublished;

                db.Entry(pageToUpdate).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Message"] = "Đã cập nhật trang thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // POST: Admin/PageAdmin/Delete/5 (Xử lý xóa trang)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var page = db.Pages.Find(id);
            if (page == null)
            {
                return HttpNotFound();
            }

            db.Pages.Remove(page);
            db.SaveChanges();

            TempData["Message"] = "Đã xóa trang thành công.";
            return RedirectToAction("Index");
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