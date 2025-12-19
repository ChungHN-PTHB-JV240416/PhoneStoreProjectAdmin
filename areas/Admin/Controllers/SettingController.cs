using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhoneStore_New.Areas.Admin.Models.ViewModels;
using PhoneStore_New.Models;
using PhoneStore_New;

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class SettingController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // =========================================================
        // 1. CÁC HÀM HELPER (HỖ TRỢ)
        // =========================================================

        // Danh sách các tùy chọn Bố cục / Chức năng cho Menu
        private List<SelectListItem> GetLayoutOptions()
        {
            return new List<SelectListItem>
            {
                // Nhóm hiển thị sản phẩm (View thường)
                new SelectListItem { Value = "0", Text = "Mặc định: Lưới Sản phẩm (Grid)" },
                new SelectListItem { Value = "1", Text = "Trang: Giới thiệu / Thông tin" },
                new SelectListItem { Value = "2", Text = "Trang: Săn Sale (Flash Sale)" },
                new SelectListItem { Value = "4", Text = "Trang: Bộ sưu tập ảnh (Gallery)" },

                // Nhóm Form Chức năng (View Form Mẫu) -> Bắt buộc dùng link Collection/Index/{ID}
                new SelectListItem { Value = "10", Text = "Form mẫu: Top Bán chạy (BestSellers)" },
                new SelectListItem { Value = "11", Text = "Form mẫu: Lịch sử đơn hàng (User)" },
                new SelectListItem { Value = "12", Text = "Form mẫu: Thông tin tài khoản (Profile)" },
                new SelectListItem { Value = "13", Text = "Form mẫu: Giỏ hàng (Shopping Cart)" }
            };
        }

        // Hàm tiện ích để cập nhật hoặc thêm mới Setting vào DB
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

        // =========================================================
        // 2. TRANG CHÍNH (INDEX) - LOAD DỮ LIỆU
        // =========================================================
        public ActionResult Index(string message)
        {
            // Lấy tất cả settings ra Dictionary cho dễ truy xuất
            var settingsDict = db.Settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);

            ViewBag.LayoutOptions = GetLayoutOptions();

            // 1. Load Navbar Items
            var navItems = db.NavbarItems
                             .OrderBy(n => n.ItemOrder)
                             .Select(n => new NavbarItemViewModel
                             {
                                 ItemId = n.ItemId,
                                 ItemText = n.ItemText,
                                 ItemUrl = n.ItemUrl,
                                 ItemOrder = n.ItemOrder ?? 0,
                                 ParentId = n.ParentId,
                                 LayoutType = n.LayoutType
                             })
                             .ToList();

            // 2. Load Parent List (cho Dropdown chọn cha)
            var parentList = navItems.Where(n => n.ParentId == null)
                                     .Select(n => new SelectListItem { Value = n.ItemId.ToString(), Text = n.ItemText })
                                     .ToList();
            parentList.Insert(0, new SelectListItem { Value = "", Text = "--- (Là mục cha) ---" });

            // 3. Load Page Links (Link tĩnh tới các bài viết)
            var pageLinks = db.Pages.Where(c => c.IsPublished)
                                    .OrderBy(c => c.Title)
                                    .Select(c => new SelectListItem { Value = "/Page/Show/" + c.Slug, Text = c.Title })
                                    .ToList();
            pageLinks.Insert(0, new SelectListItem { Value = "", Text = "--- Chọn Trang (Nội dung) ---" });

            // 4. Xử lý dữ liệu Flash Sale (Parse từ string trong DB ra DateTime)
            bool.TryParse(settingsDict.GetValueOrDefault("flash_sale_active"), out bool fsActive);

            DateTime.TryParse(settingsDict.GetValueOrDefault("flash_sale_start"), out DateTime fsStart);
            if (fsStart == DateTime.MinValue) fsStart = DateTime.Now;

            DateTime.TryParse(settingsDict.GetValueOrDefault("flash_sale_end"), out DateTime fsEnd);
            if (fsEnd == DateTime.MinValue) fsEnd = DateTime.Now.AddDays(1);

            // 5. Đổ dữ liệu vào ViewModel
            var model = new SettingViewModel
            {
                // Cài đặt chung
                WelcomeText = settingsDict.GetValueOrDefault("welcome_text"),
                ProductsPerRow = int.Parse(settingsDict.GetValueOrDefault("products_per_row", "4")),
                ShowSearchBar = bool.Parse(settingsDict.GetValueOrDefault("show_search_bar", "true")),
                FooterText = settingsDict.GetValueOrDefault("footer_text"),
                FooterAddress = settingsDict.GetValueOrDefault("footer_address"),
                FooterPhone = settingsDict.GetValueOrDefault("footer_phone"),
                CurrentLogoUrl = settingsDict.GetValueOrDefault("logo_url"),
                CurrentBackgroundUrl = settingsDict.GetValueOrDefault("background_image_url"),
                CurrentQrCodeUrl = settingsDict.GetValueOrDefault("qr_code_url"),

                // Cài đặt Flash Sale
                FlashSaleIsActive = fsActive,
                FlashSaleStartTime = fsStart,
                FlashSaleEndTime = fsEnd,

                // Các danh sách
                NavbarItems = navItems,
                PriceRanges = db.PriceRanges.OrderBy(r => r.RangeOrder)
                                            .Select(r => new PriceRangeViewModel { RangeId = r.RangeId, RangeLabel = r.RangeLabel, MinPrice = r.MinPrice, MaxPrice = r.MaxPrice, RangeOrder = r.RangeOrder ?? 0 })
                                            .ToList(),

                Message = (TempData["Message"] as string) ?? message,
                ParentNavbarItems = parentList,
                PageLinks = pageLinks
            };

            return View(model);
        }

        // =========================================================
        // 3. LƯU CÀI ĐẶT VĂN BẢN CHUNG
        // =========================================================
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
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index", new { message = "Lỗi nhập liệu." });
        }

        // =========================================================
        // 4. QUẢN LÝ FLASH SALE (MỚI)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveFlashSaleSettings(SettingViewModel model)
        {
            // Lưu trạng thái
            UpdateSetting("flash_sale_active", model.FlashSaleIsActive.ToString());

            // Lưu thời gian (Format chuẩn để dễ đọc lại)
            if (model.FlashSaleStartTime.HasValue)
                UpdateSetting("flash_sale_start", model.FlashSaleStartTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));

            if (model.FlashSaleEndTime.HasValue)
                UpdateSetting("flash_sale_end", model.FlashSaleEndTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));

            db.SaveChanges();
            TempData["Message"] = "Cập nhật cấu hình Flash Sale thành công!";
            return RedirectToAction("Index");
        }

        // =========================================================
        // 5. QUẢN LÝ NAVBAR (MENU) - LOGIC ĐIỀU HƯỚNG TRUNG TÂM
        // =========================================================
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
                        ParentId = model.ParentId,
                        ItemVisible = true,
                        LayoutType = model.LayoutType
                    };

                    db.NavbarItems.Add(newItem);
                    db.SaveChanges(); // Lưu lần 1 để lấy ItemId

                    // LOGIC URL:
                    // Nếu URL để trống HOẶC là trang Chức năng (LayoutType >= 10)
                    // -> Bắt buộc dùng link Collection chuẩn: /Collection/Index/{ID}
                    if (string.IsNullOrEmpty(model.ItemUrl) || model.LayoutType >= 10)
                    {
                        newItem.ItemUrl = "/Collection/Index/" + newItem.ItemId;
                        db.SaveChanges(); // Lưu lần 2 cập nhật URL
                    }
                    TempData["Message"] = "Thêm mục mới thành công!";
                }
                else // Sửa
                {
                    var item = db.NavbarItems.Find(model.ItemId);
                    if (item != null)
                    {
                        item.ItemText = model.ItemText;
                        item.ItemOrder = model.ItemOrder;
                        item.ParentId = model.ParentId;
                        item.LayoutType = model.LayoutType; // Cập nhật loại Layout

                        // LOGIC URL (Sửa):
                        // 1. Nếu là trang chức năng (Bestseller, History, Profile...) -> Luôn dùng link chuẩn
                        if (model.LayoutType >= 10)
                        {
                            item.ItemUrl = "/Collection/Index/" + item.ItemId;
                        }
                        // 2. Nếu User nhập link tay (ví dụ link tới bài viết) -> Dùng link đó
                        else if (!string.IsNullOrEmpty(model.ItemUrl))
                        {
                            item.ItemUrl = model.ItemUrl;
                        }
                        // 3. Nếu để trống -> Dùng link chuẩn
                        else
                        {
                            item.ItemUrl = "/Collection/Index/" + item.ItemId;
                        }

                        db.Entry(item).State = EntityState.Modified;
                        TempData["Message"] = "Cập nhật thành công!";
                    }
                }
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult DeleteNavbarItem(int id)
        {
            var item = db.NavbarItems.Find(id);
            if (item != null)
            {
                if (db.NavbarItems.Any(n => n.ParentId == id))
                {
                    TempData["Message"] = "Lỗi: Không thể xóa mục này vì nó đang là mục cha.";
                }
                else
                {
                    db.NavbarItems.Remove(item);
                    db.SaveChanges();
                    TempData["Message"] = "Xóa thành công.";
                }
            }
            return RedirectToAction("Index");
        }

        // =========================================================
        // 6. CÁC HÀM KHÁC (UPLOAD FILE, KHOẢNG GIÁ)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveFile(HttpPostedFileBase fileUpload, string settingKey)
        {
            if (fileUpload != null && fileUpload.ContentLength > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(fileUpload.FileName);
                var path = Path.Combine(Server.MapPath("~/uploads/"), fileName);
                fileUpload.SaveAs(path);
                UpdateSetting(settingKey, "~/uploads/" + fileName);
                db.SaveChanges();
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
                    db.PriceRanges.Add(new PriceRanx
                    {
                        RangeLabel = model.RangeLabel,
                        MinPrice = model.MinPrice,
                        MaxPrice = model.MaxPrice,
                        RangeOrder = model.RangeOrder
                    });
                }
                else
                {
                    var r = db.PriceRanges.Find(model.RangeId);
                    if (r != null)
                    {
                        r.RangeLabel = model.RangeLabel;
                        r.MinPrice = model.MinPrice;
                        r.MaxPrice = model.MaxPrice;
                        r.RangeOrder = model.RangeOrder;
                    }
                }
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult DeletePriceRange(int id)
        {
            var r = db.PriceRanges.Find(id);
            if (r != null)
            {
                db.PriceRanges.Remove(r);
                db.SaveChanges();
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