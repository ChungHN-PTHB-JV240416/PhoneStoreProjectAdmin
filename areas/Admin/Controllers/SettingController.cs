using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhoneStore_New.Areas.Admin.Models.ViewModels;
using PhoneStore_New.Models;
using System.Collections.Generic; // Cần cho List
using PhoneStore_New; // Cần cho IdentityExtensions

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class SettingController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities(); // Đảm bảo đúng tên DbContext

        // Hàm tiện ích để cập nhật Settings (INSERT OR UPDATE)
        private void UpdateSetting(string key, string value)
        {
            var setting = db.Settings.FirstOrDefault(s => s.SettingKey == key);
            if (setting == null)
            {
                db.Settings.Add(new Setting { SettingKey = key, SettingValue = value });
            }
            else
            {
                setting.SettingValue = value;
                db.Entry(setting).State = EntityState.Modified;
            }
        }

        // GET: Admin/Setting/Index (Tải và hiển thị tất cả cài đặt)
        public ActionResult Index(string message)
        {
            var settingsDict = db.Settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);

            // === BẮT ĐẦU SỬA ĐỔI: TẢI DỮ LIỆU CHO MENU ĐA CẤP ===

            // 1. Lấy danh sách Navbar Items
            var navItems = db.NavbarItems
                             .OrderBy(n => n.ItemOrder)
                             .Select(n => new NavbarItemViewModel
                             {
                                 ItemId = n.ItemId,
                                 ItemText = n.ItemText,
                                 ItemUrl = n.ItemUrl,
                                 ItemOrder = n.ItemOrder ?? 0,
                                 ParentId = n.ParentId // Lấy ParentId
                             })
                             .ToList();

            // 2. Lấy danh sách các mục có thể làm cha (là các mục không có ParentId)
            var parentList = navItems
                .Where(n => n.ParentId == null)
                .Select(n => new SelectListItem
                {
                    Value = n.ItemId.ToString(),
                    Text = n.ItemText
                })
                .ToList();
            parentList.Insert(0, new SelectListItem { Value = "", Text = "--- (Là mục cha)" });

            // 3. Lấy danh sách các Trang (Pages) (để gán link)
            var pageLinks = db.Pages
                .Where(c => c.IsPublished)
                .OrderBy(c => c.Title)
                .Select(c => new SelectListItem
                {
                    Value = "/Page/View/" + c.Slug, // Giá trị là đường link
                    Text = c.Title // Tên hiển thị
                })
                .ToList();
            pageLinks.Insert(0, new SelectListItem { Value = "", Text = "--- Chọn Trang (Nội dung) ---" });

            // === KẾT THÚC SỬA ĐỔI ===

            var priceRanges = db.PriceRanges
                                .OrderBy(r => r.RangeOrder)
                                .Select(r => new PriceRangeViewModel
                                {
                                    RangeId = r.RangeId,
                                    RangeLabel = r.RangeLabel,
                                    MinPrice = r.MinPrice,
                                    MaxPrice = r.MaxPrice,
                                    RangeOrder = r.RangeOrder ?? 0
                                })
                                .ToList();

            var model = new SettingViewModel
            {
                WelcomeText = settingsDict.GetValueOrDefault("welcome_text"),
                ProductsPerRow = int.Parse(settingsDict.GetValueOrDefault("products_per_row", "4")),
                ShowSearchBar = bool.Parse(settingsDict.GetValueOrDefault("show_search_bar", "true")),
                FooterText = settingsDict.GetValueOrDefault("footer_text"),
                FooterAddress = settingsDict.GetValueOrDefault("footer_address"),
                FooterPhone = settingsDict.GetValueOrDefault("footer_phone"),
                CurrentLogoUrl = settingsDict.GetValueOrDefault("logo_url"),
                CurrentBackgroundUrl = settingsDict.GetValueOrDefault("background_image_url"),
                CurrentQrCodeUrl = settingsDict.GetValueOrDefault("qr_code_url"),

                NavbarItems = navItems,
                PriceRanges = priceRanges,
                Message = (TempData["Message"] as string) ?? message,

                // Gán các danh sách mới vào ViewModel
                ParentNavbarItems = parentList,
                PageLinks = pageLinks
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTextSettings(SettingViewModel model)
        {
            if (ModelState.IsValid)
            {
                UpdateSetting("welcome_text", model.WelcomeText);
                UpdateSetting("products_per_row", model.ProductsPerRow.ToString());
                UpdateSetting("footer_text", model.FooterText);
                UpdateSetting("footer_address", model.FooterAddress);
                UpdateSetting("footer_phone", model.FooterPhone);
                UpdateSetting("show_search_bar", model.ShowSearchBar.ToString().ToLower());

                db.SaveChanges();
                TempData["Message"] = "Cài đặt văn bản đã được lưu thành công.";
                return RedirectToAction("Index"); // Redirect để tải lại
            }

            // Nếu lỗi, tải lại các danh sách
            model.ParentNavbarItems = db.NavbarItems.Where(n => n.ParentId == null).Select(n => new SelectListItem { Value = n.ItemId.ToString(), Text = n.ItemText }).ToList();
            model.PageLinks = db.Pages.Where(c => c.IsPublished).OrderBy(c => c.Title).Select(c => new SelectListItem { Value = "/Page/View/" + c.Slug, Text = c.Title }).ToList();
            model.NavbarItems = db.NavbarItems.OrderBy(n => n.ItemOrder).Select(n => new NavbarItemViewModel { ItemId = n.ItemId, ItemText = n.ItemText, ItemUrl = n.ItemUrl, ItemOrder = n.ItemOrder ?? 0, ParentId = n.ParentId }).ToList();
            model.PriceRanges = db.PriceRanges.OrderBy(r => r.RangeOrder).Select(r => new PriceRangeViewModel { RangeId = r.RangeId, RangeLabel = r.RangeLabel, MinPrice = r.MinPrice, MaxPrice = r.MaxPrice, RangeOrder = r.RangeOrder ?? 0 }).ToList();

            return View("Index", model);
        }

        // === SỬA ĐỔI LOGIC LƯU ParentId ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManageNavbar(NavbarItemViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.ItemId == 0) // Thêm mới
                {
                    var newItem = new NavbarItem
                    {
                        ItemText = model.ItemText,
                        ItemUrl = model.ItemUrl,
                        ItemOrder = model.ItemOrder,
                        ParentId = model.ParentId, // Lưu ParentId
                        ItemVisible = true
                    };
                    db.NavbarItems.Add(newItem);
                    TempData["Message"] = "Mục Navbar đã được thêm thành công!";
                }
                else // Sửa
                {
                    var item = db.NavbarItems.Find(model.ItemId);
                    if (item != null)
                    {
                        item.ItemText = model.ItemText;
                        item.ItemUrl = model.ItemUrl;
                        item.ItemOrder = model.ItemOrder;
                        item.ParentId = model.ParentId; // Lưu ParentId
                        db.Entry(item).State = EntityState.Modified;
                        TempData["Message"] = "Mục Navbar đã được cập nhật thành công!";
                    }
                }
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // GET: Admin/Setting/DeleteNavbarItem/5
        public ActionResult DeleteNavbarItem(int id)
        {
            var item = db.NavbarItems.Find(id);
            if (item != null)
            {
                // Kiểm tra xem có mục con nào không
                if (db.NavbarItems.Any(n => n.ParentId == id))
                {
                    TempData["Message"] = "Lỗi: Không thể xóa mục này vì nó đang là mục cha của các mục khác.";
                }
                else
                {
                    db.NavbarItems.Remove(item);
                    db.SaveChanges();
                    TempData["Message"] = "Mục Navbar đã được xóa thành công.";
                }
            }
            return RedirectToAction("Index");
        }

        // --- Các Action khác (SaveFile, PriceRange...) giữ nguyên ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveFile(HttpPostedFileBase fileUpload, string settingKey)
        {
            if (fileUpload != null && fileUpload.ContentLength > 0 && !string.IsNullOrEmpty(settingKey))
            {
                try
                {
                    var uploadDir = Server.MapPath("~/uploads/");
                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(fileUpload.FileName);
                    var path = Path.Combine(uploadDir, fileName);
                    fileUpload.SaveAs(path);
                    var newUrl = "~/uploads/" + fileName;
                    UpdateSetting(settingKey, newUrl);
                    db.SaveChanges();
                    TempData["Message"] = $"File cho {settingKey} đã được cập nhật thành công.";
                }
                catch (Exception ex)
                {
                    TempData["Message"] = $"Lỗi khi tải file lên: {ex.Message}";
                }
            }
            else
            {
                TempData["Message"] = "Vui lòng chọn file.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManagePriceRange(PriceRangeViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.RangeId == 0)
                {
                    var newRange = new PriceRanx
                    {
                        RangeLabel = model.RangeLabel,
                        MinPrice = model.MinPrice,
                        MaxPrice = model.MaxPrice,
                        RangeOrder = model.RangeOrder
                    };
                    db.PriceRanges.Add(newRange);
                }
                else
                {
                    var rangeToUpdate = db.PriceRanges.Find(model.RangeId);
                    if (rangeToUpdate != null)
                    {
                        rangeToUpdate.RangeLabel = model.RangeLabel;
                        rangeToUpdate.MinPrice = model.MinPrice;
                        rangeToUpdate.MaxPrice = model.MaxPrice;
                        rangeToUpdate.RangeOrder = model.RangeOrder;
                        db.Entry(rangeToUpdate).State = EntityState.Modified;
                    }
                }
                db.SaveChanges();
                TempData["Message"] = "Khoảng giá đã được lưu thành công.";
            }
            return RedirectToAction("Index");
        }

        public ActionResult DeletePriceRange(int id)
        {
            var range = db.PriceRanges.Find(id);
            if (range != null)
            {
                db.PriceRanges.Remove(range);
                db.SaveChanges();
                TempData["Message"] = "Khoảng giá đã được xóa thành công.";
            }
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