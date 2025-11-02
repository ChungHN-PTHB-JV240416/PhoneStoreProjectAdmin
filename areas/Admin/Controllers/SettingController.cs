using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using PhoneStore_New.Models;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System;
using PhoneStore_New;

// Sửa lại using alias cho ViewModel của Admin
using AdminVMs = PhoneStore_New.Areas.Admin.Models.ViewModels;

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
            // Di chuyển SaveChanges ra ngoài để gọi một lần
        }

        // GET: Admin/Setting/Index (Tải và hiển thị tất cả cài đặt)
        public ActionResult Index(string message)
        {
            var settingsDict = db.Settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);
            var navItems = db.NavbarItems
                             .OrderBy(n => n.ItemOrder)
                             .Select(n => new AdminVMs.NavbarItemViewModel
                             {
                                 ItemId = n.ItemId,
                                 ItemText = n.ItemText,
                                 ItemUrl = n.ItemUrl,
                                 ItemOrder = n.ItemOrder ?? 0
                             })
                             .ToList();

            var priceRanges = db.PriceRanges
                                .OrderBy(r => r.RangeOrder)
                                .Select(r => new AdminVMs.PriceRangeViewModel
                                {
                                    RangeId = r.RangeId,
                                    RangeLabel = r.RangeLabel,
                                    MinPrice = r.MinPrice,
                                    MaxPrice = r.MaxPrice,
                                    RangeOrder = r.RangeOrder ?? 0
                                })
                                .ToList();

            var model = new AdminVMs.SettingViewModel
            {
                WelcomeText = settingsDict.GetValueOrDefault("welcome_text"),
                ProductsPerRow = int.Parse(settingsDict.GetValueOrDefault("products_per_row", "4")),
                FooterText = settingsDict.GetValueOrDefault("footer_text"),
                FooterAddress = settingsDict.GetValueOrDefault("footer_address"),
                FooterPhone = settingsDict.GetValueOrDefault("footer_phone"),
                CurrentLogoUrl = settingsDict.GetValueOrDefault("logo_url"),
                CurrentBackgroundUrl = settingsDict.GetValueOrDefault("background_image_url"),
                CurrentQrCodeUrl = settingsDict.GetValueOrDefault("qr_code_url"),
                NavbarItems = navItems,
                PriceRanges = priceRanges,
                Message = (TempData["Message"] as string) ?? message,

                // === SỬA ĐỔI: TẢI GIÁ TRỊ TỪ CSDL ===
                // Mặc định là "true" (bật) nếu chưa có cài đặt
                ShowSearchBar = bool.Parse(settingsDict.GetValueOrDefault("show_search_bar", "true"))
            };

            return View(model);
        }

        // POST: Admin/Setting/SaveFile (Xử lý upload Logo, QR, Background)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveFile(HttpPostedFileBase fileUpload, string settingKey)
        {
            if (fileUpload != null && fileUpload.ContentLength > 0 && !string.IsNullOrEmpty(settingKey))
            {
                try
                {
                    var uploadDir = Server.MapPath("~/uploads/");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(fileUpload.FileName);
                    var path = Path.Combine(uploadDir, fileName);
                    fileUpload.SaveAs(path);

                    var newUrl = "~/uploads/" + fileName;
                    UpdateSetting(settingKey, newUrl);
                    db.SaveChanges(); // Lưu thay đổi

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

        // POST: Admin/Setting/SaveTextSettings (Xử lý cập nhật văn bản và bố cục)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTextSettings(AdminVMs.SettingViewModel model)
        {
            if (ModelState.IsValid)
            {
                UpdateSetting("welcome_text", model.WelcomeText);
                UpdateSetting("products_per_row", model.ProductsPerRow.ToString());
                UpdateSetting("footer_text", model.FooterText);
                UpdateSetting("footer_address", model.FooterAddress);
                UpdateSetting("footer_phone", model.FooterPhone);

                // === SỬA ĐỔI: LƯU CÀI ĐẶT MỚI VÀO CSDL ===
                // Lưu dưới dạng chuỗi "true" hoặc "false"
                UpdateSetting("show_search_bar", model.ShowSearchBar.ToString().ToLower());

                db.SaveChanges(); // Lưu tất cả thay đổi chung
                TempData["Message"] = "Cài đặt văn bản đã được lưu thành công.";
            }
            return RedirectToAction("Index");
        }

        // POST: Admin/Setting/ManageNavbar (Thêm/Sửa mục Navbar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManageNavbar(AdminVMs.NavbarItemViewModel model)
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
                db.NavbarItems.Remove(item);
                db.SaveChanges();
                TempData["Message"] = "Mục Navbar đã được xóa thành công.";
            }
            return RedirectToAction("Index");
        }

        // POST: Admin/Setting/ManagePriceRange (Thêm/Sửa khoảng giá)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManagePriceRange(AdminVMs.PriceRangeViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.RangeId == 0) // Thêm mới
                {
                    var newRange = new PriceRanx // Tên bảng CSDL của bạn
                    {
                        RangeLabel = model.RangeLabel,
                        MinPrice = model.MinPrice,
                        MaxPrice = model.MaxPrice,
                        RangeOrder = model.RangeOrder
                    };
                    db.PriceRanges.Add(newRange);
                }
                else // Sửa
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

        // GET: Admin/Setting/DeletePriceRange/5
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
    }
}