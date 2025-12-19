using System;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;
using System.Collections.Generic;
using System.Data.Entity;
using System.Web.Security;

namespace PhoneStore_New.Controllers
{
    public class CollectionController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // Helper: Lấy User an toàn
        private User GetCurrentUser()
        {
            if (!User.Identity.IsAuthenticated) return null;
            var identityKey = User.Identity.Name;
            if (int.TryParse(identityKey, out int userId)) return db.Users.Find(userId);
            return db.Users.FirstOrDefault(u => u.Username == identityKey || u.Email == identityKey);
        }

        // Helper: Bắt buộc đăng nhập
        private ActionResult RequireLogin()
        {
            if (User.Identity.IsAuthenticated) { FormsAuthentication.SignOut(); Session.Clear(); }
            return RedirectToAction("Index", "Login", new { returnUrl = Request.Url.PathAndQuery });
        }

        // GET: Collection/Index/{id}
        public ActionResult Index(int? id)
        {
            if (id == null) return HttpNotFound();

            var navItem = db.NavbarItems.Find(id);
            if (navItem == null) return HttpNotFound();

            ViewBag.Title = navItem.ItemText;
            ViewBag.LayoutType = navItem.LayoutType;
            ViewBag.CurrentItemId = id;

            switch (navItem.LayoutType)
            {
                case 10: return RenderBestsellers();
                case 11:
                    if (GetCurrentUser() == null) return RequireLogin();
                    return RenderOrderHistory();
                case 12:
                    if (GetCurrentUser() == null) return RequireLogin();
                    return RenderProfile();
                    // === THÊM CASE GIỎ HÀNG ===
                case 13:
                    return RenderCart();

                case 1: return View("InfoLayout", navItem);

                // === SỬA LOGIC FLASH SALE TẠI ĐÂY ===
                case 2:
                    // 1. Lấy cấu hình từ bảng Settings
                    var settings = db.Settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);

                    bool.TryParse(settings.GetValueOrDefault("flash_sale_active"), out bool isActive);
                    DateTime.TryParse(settings.GetValueOrDefault("flash_sale_start"), out DateTime start);
                    DateTime.TryParse(settings.GetValueOrDefault("flash_sale_end"), out DateTime end);

                    if (start == DateTime.MinValue) start = DateTime.Now;
                    if (end == DateTime.MinValue) end = DateTime.Now.AddDays(1);

                    DateTime now = DateTime.Now;

                    // 2. Kiểm tra điều kiện chạy: Phải Bật VÀ (Giờ hiện tại nằm trong khoảng Start - End)
                    bool isRunning = isActive && (now >= start) && (now <= end);

                    // 3. Gửi thông tin sang View
                    ViewBag.IsSaleRunning = isRunning;
                    ViewBag.StartTime = start;
                    ViewBag.EndTime = end;

                    List<Product> saleProducts;

                    if (isRunning)
                    {
                        // Nếu đang chạy -> Lấy sản phẩm
                        saleProducts = db.Products
                                         .Where(p => p.CategoryId == id && p.DiscountPercentage > 0)
                                         .OrderByDescending(p => p.DiscountPercentage)
                                         .ToList();
                    }
                    else
                    {
                        // Nếu tắt -> Trả về danh sách rỗng (Ẩn sản phẩm)
                        saleProducts = new List<Product>();
                    }

                    return View("SaleLayout", saleProducts);

                case 4:
                    var galleryProducts = db.Products.Where(p => p.CategoryId == id).ToList();
                    return View("GalleryLayout", galleryProducts);
                case 0:
                default:
                    var products = db.Products.Where(p => p.CategoryId == id).OrderByDescending(p => p.ProductId).ToList();
                    return View("GridLayout", products);
            }
        }
        // === HÀM RENDER GIỎ HÀNG ===
        private ActionResult RenderCart()
        {
            // Lấy giỏ hàng từ Session
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            return View("CartLayout", cart);
        }
        private ActionResult RenderBestsellers()
        {
            var bestsellers = db.Products
                .Select(p => new ProductCardViewModel
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    ImageUrl = p.ImageUrl,
                    OriginalPrice = p.Price,
                    DiscountPercentage = p.DiscountPercentage ?? 0,
                    SoldQuantity = db.OrderItems.Where(oi => oi.ProductId == p.ProductId).Sum(oi => (int?)oi.Quantity) ?? 0
                })
                .OrderByDescending(p => p.SoldQuantity)
                .Take(8)
                .ToList();
            return View("BestsellerLayout", bestsellers);
        }

        private ActionResult RenderOrderHistory()
        {
            var user = GetCurrentUser();
            if (user == null) return RequireLogin();
            var orders = db.Orders.Where(o => o.UserId == user.UserId).OrderByDescending(o => o.OrderDate).ToList();
            return View("OrderHistoryLayout", orders);
        }

        private ActionResult RenderProfile()
        {
            var user = GetCurrentUser();
            if (user == null) return RequireLogin();
            return View("ProfileLayout", user);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(User model)
        {
            var user = GetCurrentUser();
            if (user != null)
            {
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;
                db.SaveChanges();
                TempData["Message"] = "Cập nhật hồ sơ thành công!";
            }
            if (Request.UrlReferrer != null) return Redirect(Request.UrlReferrer.ToString());
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}