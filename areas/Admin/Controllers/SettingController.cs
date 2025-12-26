using PhoneStore_New.Areas.Admin.Models.ViewModels;
using PhoneStore_New.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class SettingController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // =========================================================
        // 1. CÁC HÀM HELPER (HỖ TRỢ TẠO LIST)
        // =========================================================

        private List<SelectListItem> GetLayoutOptions()
        {
            return new List<SelectListItem>
    {
        // Nhóm hiển thị sản phẩm
        new SelectListItem { Value = "0", Text = "Mặc định: Lưới Sản phẩm (Grid)" },
        new SelectListItem { Value = "2", Text = "Trang Săn Sale (Flash Sale)" },
        
        // --- SỬA 2 DÒNG NÀY ---
        new SelectListItem { Value = "5", Text = "Trang Quảng bá Sản phẩm (Promo/Landing)" }, // Cũ là List
        new SelectListItem { Value = "4", Text = "Trang Chăm sóc khách hàng (Support)" },     // Cũ là Gallery
        // ----------------------

        new SelectListItem { Value = "1", Text = "Trang Thông tin (Text đơn giản)" },

        // Nhóm Form Chức năng
        new SelectListItem { Value = "10", Text = "Form mẫu: Top Bán chạy (BestSellers)" },
        new SelectListItem { Value = "11", Text = "Form mẫu: Lịch sử đơn hàng (User)" },
        new SelectListItem { Value = "12", Text = "Form mẫu: Hồ sơ cá nhân (Profile)" },
        new SelectListItem { Value = "13", Text = "Form mẫu: Giỏ hàng (Shopping Cart)" }
    };
        }

        private void UpdateSetting(string key, string value)
        {
            var setting = db.Settings.FirstOrDefault(s => s.SettingKey == key);
            if (setting == null)
            {
                db.Settings.Add(new Setting { SettingKey = key, SettingValue = value ?? "" });
            }
            else
            {
                setting.SettingValue = value ?? "";
                db.Entry(setting).State = EntityState.Modified;
            }
        }

        // =========================================================
        // 2. TRANG CHÍNH (INDEX) - LOAD DỮ LIỆU
        // =========================================================
        public ActionResult Index(string message)
        {
            var settingsDict = db.Settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);

            // Populate Dropdown Layouts
            ViewBag.LayoutOptions = GetLayoutOptions();

            // 1. Load Navbar Items
            var navItems = db.NavbarItems.OrderBy(n => n.ItemOrder).Select(n => new NavbarItemViewModel
            {
                ItemId = n.ItemId,
                ItemText = n.ItemText,
                ItemUrl = n.ItemUrl,
                ItemOrder = n.ItemOrder ?? 0,
                ParentId = n.ParentId,
                LayoutType = n.LayoutType
            }).ToList();

            // 2. Load Parent List
            var parentList = navItems.Where(n => n.ParentId == null)
                                     .Select(n => new SelectListItem { Value = n.ItemId.ToString(), Text = n.ItemText })
                                     .ToList();
            parentList.Insert(0, new SelectListItem { Value = "", Text = "-- Không có (Là mục gốc) --" });

            // 3. Load Page Links (Trang tĩnh)
            var pageLinks = db.Pages.Where(c => c.IsPublished)
                                    .OrderBy(c => c.Title)
                                    // SỬA DÒNG NÀY: Đổi thành /Page/PageShow/
                                    .Select(c => new SelectListItem { Value = "/Page/PageShow/" + c.Slug, Text = c.Title })
                                    .ToList();
            // Thêm các trang hệ thống vào dropdown
            pageLinks.Insert(0, new SelectListItem { Value = Url.Action("Index", "Home", new { area = "" }), Text = "Trang chủ" });
            pageLinks.Insert(1, new SelectListItem { Value = Url.Action("Index", "Cart", new { area = "" }), Text = "Giỏ hàng" });
            pageLinks.Insert(0, new SelectListItem { Value = "", Text = "-- Chọn Trang có sẵn --" });

            // 4. Parse Settings
            int.TryParse(settingsDict.GetValueOrDefault("products_per_row", "4"), out int ppr);
            bool.TryParse(settingsDict.GetValueOrDefault("show_search_bar", "true"), out bool showSearch);

            // Flash Sale Parsing
            bool.TryParse(settingsDict.GetValueOrDefault("flash_sale_active", "false"), out bool fsActive);
            DateTime.TryParse(settingsDict.GetValueOrDefault("flash_sale_start"), out DateTime fsStart);
            if (fsStart == DateTime.MinValue) fsStart = DateTime.Now;
            DateTime.TryParse(settingsDict.GetValueOrDefault("flash_sale_end"), out DateTime fsEnd);
            if (fsEnd == DateTime.MinValue) fsEnd = DateTime.Now.AddDays(1);

            // 5. Init ViewModel
            var model = new SettingViewModel
            {
                WelcomeText = settingsDict.GetValueOrDefault("welcome_text"),
                ProductsPerRow = ppr,
                ShowSearchBar = showSearch,
                FooterText = settingsDict.GetValueOrDefault("footer_text"),
                FooterAddress = settingsDict.GetValueOrDefault("footer_address"),
                FooterPhone = settingsDict.GetValueOrDefault("footer_phone"),
                CurrentLogoUrl = settingsDict.GetValueOrDefault("logo_url"),
                CurrentQrCodeUrl = settingsDict.GetValueOrDefault("qr_code_url"),

                FlashSaleIsActive = fsActive,
                FlashSaleStartTime = fsStart,
                FlashSaleEndTime = fsEnd,

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
        // 3. XỬ LÝ POST: CÀI ĐẶT CHUNG
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTextSettings(SettingViewModel model)
        {
            UpdateSetting("welcome_text", model.WelcomeText);
            UpdateSetting("products_per_row", model.ProductsPerRow.ToString());
            UpdateSetting("footer_text", model.FooterText);
            UpdateSetting("footer_address", model.FooterAddress);
            UpdateSetting("footer_phone", model.FooterPhone);
            UpdateSetting("show_search_bar", model.ShowSearchBar.ToString().ToLower());

            db.SaveChanges();
            TempData["Message"] = "Cập nhật cài đặt chung thành công!";
            return RedirectToAction("Index");
        }

        // =========================================================
        // 4. XỬ LÝ POST: FLASH SALE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveFlashSaleSettings(SettingViewModel model)
        {
            UpdateSetting("flash_sale_active", model.FlashSaleIsActive.ToString().ToLower());

            if (model.FlashSaleStartTime.HasValue)
                UpdateSetting("flash_sale_start", model.FlashSaleStartTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));

            if (model.FlashSaleEndTime.HasValue)
                UpdateSetting("flash_sale_end", model.FlashSaleEndTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));

            db.SaveChanges();
            TempData["Message"] = "Cập nhật cấu hình Flash Sale thành công!";
            return RedirectToAction("Index");
        }

        // =========================================================
        // 5. QUẢN LÝ NAVBAR (MENU)
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
                    db.SaveChanges(); // Lưu để lấy ItemId

                    // Tự động tạo Link nếu trống hoặc là trang chức năng
                    if (string.IsNullOrEmpty(model.ItemUrl) || model.LayoutType >= 10)
                    {
                        newItem.ItemUrl = "/Collection/Index/" + newItem.ItemId;
                        db.SaveChanges();
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
                        item.LayoutType = model.LayoutType;

                        // Logic cập nhật Link
                        if (model.LayoutType >= 10) // Trang chức năng luôn dùng link chuẩn
                        {
                            item.ItemUrl = "/Collection/Index/" + item.ItemId;
                        }
                        else if (!string.IsNullOrEmpty(model.ItemUrl)) // Nếu người dùng nhập link tay
                        {
                            item.ItemUrl = model.ItemUrl;
                        }
                        else // Nếu để trống -> Dùng link chuẩn
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
                    TempData["Message"] = "Lỗi: Không thể xóa mục này vì nó đang chứa mục con.";
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
        // 6. XỬ LÝ FILE UPLOAD (LOGO / QR)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveFile(HttpPostedFileBase fileUpload, string settingKey)
        {
            if (fileUpload != null && fileUpload.ContentLength > 0)
            {
                // Lưu vào thư mục Content/images để dễ quản lý
                string fileName = Path.GetFileNameWithoutExtension(fileUpload.FileName) + "_" + Guid.NewGuid().ToString().Substring(0, 5) + Path.GetExtension(fileUpload.FileName);
                string path = Path.Combine(Server.MapPath("~/Content/images/"), fileName);

                // Tạo thư mục nếu chưa có
                if (!Directory.Exists(Server.MapPath("~/Content/images/")))
                {
                    Directory.CreateDirectory(Server.MapPath("~/Content/images/"));
                }

                fileUpload.SaveAs(path);
                UpdateSetting(settingKey, "~/Content/images/" + fileName);
                db.SaveChanges();
                TempData["Message"] = "Cập nhật hình ảnh thành công!";
            }
            else
            {
                TempData["Message"] = "Vui lòng chọn file ảnh!";
            }
            return RedirectToAction("Index");
        }

        // =========================================================
        // 7. QUẢN LÝ KHOẢNG GIÁ
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManagePriceRange(PriceRangeViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.RangeId == 0) // Thêm mới
                {
                    // SỬA LỖI CHÍNH TẢ Ở ĐÂY: PriceRanx -> PriceRange
                    db.PriceRanges.Add(new PriceRanx
                    {
                        RangeLabel = model.RangeLabel,
                        MinPrice = model.MinPrice,
                        MaxPrice = model.MaxPrice,
                        RangeOrder = model.RangeOrder
                    });
                    TempData["Message"] = "Thêm khoảng giá thành công!";
                }
                else // Sửa
                {
                    var r = db.PriceRanges.Find(model.RangeId);
                    if (r != null)
                    {
                        r.RangeLabel = model.RangeLabel;
                        r.MinPrice = model.MinPrice;
                        r.MaxPrice = model.MaxPrice;
                        r.RangeOrder = model.RangeOrder;
                        db.Entry(r).State = EntityState.Modified;
                        TempData["Message"] = "Cập nhật khoảng giá thành công!";
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
                TempData["Message"] = "Xóa khoảng giá thành công!";
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