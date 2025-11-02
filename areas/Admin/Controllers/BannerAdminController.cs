using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhoneStore_New.Areas.Admin.Models.ViewModels;
using PhoneStore_New.Models;
using System.Data.Entity.Validation; // <-- THÊM DÒNG NÀY ĐỂ BẮT LỖI CỤ THỂ

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class BannerAdminController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities(); // Đảm bảo đúng tên DbContext

        // GET: Admin/BannerAdmin
        public ActionResult Index(string message)
        {
            ViewBag.Message = (TempData["Message"] as string) ?? message;

            var banners = db.Banners
                            .OrderBy(b => b.DisplayOrder)
                            .Select(b => new BannerViewModel
                            {
                                BannerId = b.BannerId,
                                ImageUrl = b.ImageUrl,
                                LinkUrl = b.LinkUrl,
                                Title = b.Title,
                                DisplayOrder = b.DisplayOrder,
                                IsActive = b.IsActive
                            })
                            .ToList();

            return View(banners);
        }

        // GET: Admin/BannerAdmin/Create
        public ActionResult Create()
        {
            var viewModel = new BannerViewModel
            {
                IsActive = true,
                DisplayOrder = 1
            };
            return View(viewModel);
        }

        // POST: Admin/BannerAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(BannerViewModel model)
        {
            if (model.ImageFile == null || model.ImageFile.ContentLength == 0)
            {
                ModelState.AddModelError("ImageFile", "Vui lòng chọn một ảnh banner.");
            }

            if (ModelState.IsValid)
            {
                string imageUrl = SaveImageFile(model.ImageFile);
                if (imageUrl == null)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tải ảnh lên.");
                    return View(model);
                }

                var newBanner = new Banner
                {
                    ImageUrl = imageUrl,
                    LinkUrl = model.LinkUrl,
                    Title = model.Title,
                    DisplayOrder = model.DisplayOrder,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };

                db.Banners.Add(newBanner);

                // === BẮT ĐẦU SỬA LỖI LOGIC: THÊM KHỐI BẮT LỖI VALIDATION ===
                try
                {
                    db.SaveChanges(); // Dòng 80
                    TempData["Message"] = "Đã thêm banner mới thành công.";
                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    // "Giải nén" lỗi và hiển thị
                    foreach (var entityError in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in entityError.ValidationErrors)
                        {
                            // Thêm lỗi vào ModelState để nó hiển thị trên View
                            ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                    // Quay trở lại form Create và hiển thị lỗi
                    return View(model);
                }
                catch (Exception ex)
                {
                    // Lỗi chung khác
                    ModelState.AddModelError("", "Đã xảy ra lỗi không xác định: " + ex.Message);
                    return View(model);
                }
                // === KẾT THÚC SỬA LỖI LOGIC ===
            }
            return View(model);
        }

        // GET: Admin/BannerAdmin/Edit/5
        public ActionResult Edit(int id)
        {
            var banner = db.Banners.Find(id);
            if (banner == null)
            {
                return HttpNotFound();
            }

            var viewModel = new BannerViewModel
            {
                BannerId = banner.BannerId,
                ImageUrl = banner.ImageUrl,
                LinkUrl = banner.LinkUrl,
                Title = banner.Title,
                DisplayOrder = banner.DisplayOrder,
                IsActive = banner.IsActive
            };

            return View(viewModel);
        }

        // POST: Admin/BannerAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(BannerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var bannerToUpdate = db.Banners.Find(model.BannerId);
                if (bannerToUpdate == null)
                {
                    return HttpNotFound();
                }

                if (model.ImageFile != null && model.ImageFile.ContentLength > 0)
                {
                    string newImageUrl = SaveImageFile(model.ImageFile);
                    if (newImageUrl != null)
                    {
                        DeleteImageFile(bannerToUpdate.ImageUrl);
                        bannerToUpdate.ImageUrl = newImageUrl;
                    }
                }

                bannerToUpdate.LinkUrl = model.LinkUrl;
                bannerToUpdate.Title = model.Title;
                bannerToUpdate.DisplayOrder = model.DisplayOrder;
                bannerToUpdate.IsActive = model.IsActive;

                db.Entry(bannerToUpdate).State = EntityState.Modified;

                // === BẮT ĐẦU SỬA LỖI LOGIC: THÊM KHỐI BẮT LỖI VALIDATION ===
                try
                {
                    db.SaveChanges();
                    TempData["Message"] = "Đã cập nhật banner thành công.";
                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var entityError in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in entityError.ValidationErrors)
                        {
                            ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                    return View(model);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Đã xảy ra lỗi không xác định: " + ex.Message);
                    return View(model);
                }
                // === KẾT THÚC SỬA LỖI LOGIC ===
            }
            return View(model);
        }

        // POST: Admin/BannerAdmin/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var banner = db.Banners.Find(id);
            if (banner == null)
            {
                return HttpNotFound();
            }

            DeleteImageFile(banner.ImageUrl);
            db.Banners.Remove(banner);
            db.SaveChanges();

            TempData["Message"] = "Đã xóa banner thành công.";
            return RedirectToAction("Index");
        }

        private string SaveImageFile(HttpPostedFileBase file)
        {
            try
            {
                var uploadDir = Server.MapPath("~/uploads/banners/");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string path = Path.Combine(uploadDir, fileName);
                file.SaveAs(path);
                return "~/uploads/banners/" + fileName;
            }
            catch (Exception) { return null; }
        }

        private void DeleteImageFile(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            try
            {
                string path = Server.MapPath(imageUrl);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            catch (Exception) { }
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