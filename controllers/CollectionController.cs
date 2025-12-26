using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;

namespace PhoneStore_New.Controllers
{
    public class CollectionController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // === CÁC HÀM HỖ TRỢ (HELPER) ===

        private User GetCurrentUser()
        {
            if (!User.Identity.IsAuthenticated) return null;
            var identityKey = User.Identity.Name;
            if (int.TryParse(identityKey, out int userId)) return db.Users.Find(userId);
            return db.Users.FirstOrDefault(u => u.Username == identityKey || u.Email == identityKey);
        }

        private ActionResult RequireLogin()
        {
            if (User.Identity.IsAuthenticated) { FormsAuthentication.SignOut(); Session.Clear(); }
            return RedirectToAction("Index", "Login", new { returnUrl = Request.Url.PathAndQuery });
        }

        // Hàm chuyển đổi từ Product Entity sang ViewModel (Xử lý giá VIP/Giảm giá)
        private List<ProductCardViewModel> MapToCards(List<Product> products)
        {
            bool isVip = false;
            // Kiểm tra Session hoặc DB để xác định VIP
            if (Session["UserType"] != null && "vip".Equals(Session["UserType"].ToString(), StringComparison.OrdinalIgnoreCase)) isVip = true;
            else if (User.Identity.IsAuthenticated)
            {
                var u = db.Users.FirstOrDefault(x => x.Username == User.Identity.Name);
                if (u != null && u.user_type == "vip") isVip = true;
            }

            return products.Select(p => {
                decimal regular = p.Price * (1m - (p.DiscountPercentage ?? 0) / 100m);
                bool isVipPrice = (isVip && p.vip_price.HasValue && p.vip_price.Value < regular);
                return new ProductCardViewModel
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    ImageUrl = p.ImageUrl,
                    OriginalPrice = p.Price,
                    DiscountPercentage = p.DiscountPercentage ?? 0,
                    FinalPrice = isVipPrice ? p.vip_price.Value : regular,
                    IsVipPrice = isVipPrice,
                    SoldQuantity = db.OrderItems.Where(oi => oi.ProductId == p.ProductId).Sum(oi => (int?)oi.Quantity) ?? 0,
                    Description = p.Description,
                    StockQuantity = p.StockQuantity
                };
            }).ToList();
        }

        // === ACTION MỚI: HIỂN THỊ TẤT CẢ SẢN PHẨM THEO THƯƠNG HIỆU ===
        public ActionResult ShowAllProducts()
        {
            // 1. Tìm danh mục cha là "Thương Hiệu"
            var brandParent = db.NavbarItems.FirstOrDefault(n => n.ItemText.Trim().Equals("Thương Hiệu", StringComparison.OrdinalIgnoreCase)
                                                              || n.ItemText.Trim().Equals("THƯƠNG HIỆU", StringComparison.OrdinalIgnoreCase));

            if (brandParent == null) return HttpNotFound("Không tìm thấy danh mục Thương Hiệu trong hệ thống.");

            // 2. Lấy danh sách các thương hiệu con
            var childBrands = db.NavbarItems
                                .Where(n => n.ParentId == brandParent.ItemId && n.ItemVisible == true)
                                .OrderBy(n => n.ItemOrder)
                                .ToList();

            var model = new List<BrandSectionViewModel>();

            // 3. Duyệt qua từng thương hiệu để lấy sản phẩm
            foreach (var brand in childBrands)
            {
                var products = db.Products
                                 .Where(p => p.ProductNavbarLinks.Any(link => link.NavbarItemId == brand.ItemId))
                                 .OrderByDescending(p => p.CreatedAt)
                                 .Take(8) // Giới hạn 8 sản phẩm mỗi mục
                                 .ToList();

                if (products.Any())
                {
                    var productCards = MapToCards(products);

                    model.Add(new BrandSectionViewModel
                    {
                        BrandId = brand.ItemId,
                        BrandName = brand.ItemText,
                        Products = productCards
                    });
                }
            }

            return View("ShowAllProducts", model);
        }

        // === ACTION INDEX: ĐIỀU HƯỚNG DỰA TRÊN LAYOUT TYPE ===
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
                case 11: if (GetCurrentUser() == null) return RequireLogin(); return RenderOrderHistory();
                case 12: if (GetCurrentUser() == null) return RequireLogin(); return RenderProfile();
                case 13: return RenderCart();
                case 1: return View("InfoLayout", navItem);

                case 2: // Flash Sale
                    var settings = db.Settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);
                    string activeVal = settings.ContainsKey("flash_sale_active") ? settings["flash_sale_active"].Trim().ToLower() : "false";
                    bool isActive = (activeVal == "true" || activeVal == "1" || activeVal == "on" || activeVal == "yes");

                    DateTime start, end;
                    if (!DateTime.TryParse(settings.ContainsKey("flash_sale_start") ? settings["flash_sale_start"] : null, out start)) start = new DateTime(2000, 1, 1);
                    if (!DateTime.TryParse(settings.ContainsKey("flash_sale_end") ? settings["flash_sale_end"] : null, out end)) end = new DateTime(2099, 12, 31);

                    DateTime now = DateTime.Now;
                    bool isRunning = isActive && (now >= start) && (now <= end);

                    ViewBag.IsSaleRunning = isRunning;
                    ViewBag.StartTime = start;
                    ViewBag.EndTime = end;

                    List<ProductCardViewModel> saleViewModels = new List<ProductCardViewModel>();

                    if (isRunning)
                    {
                        var saleProducts = db.Products
                                             .Where(p => p.ProductNavbarLinks.Any(link => link.NavbarItemId == id)
                                                      && p.DiscountPercentage > 0)
                                             .OrderByDescending(p => p.DiscountPercentage)
                                             .ToList();
                        saleViewModels = MapToCards(saleProducts);
                    }
                    return View("SaleLayout", saleViewModels);

                case 4:
                    // Truyền thông tin NavbarItem sang để lấy tiêu đề
                    return View("SupportLayout", navItem);

                // Case 5: Trang Quảng bá sản phẩm (PromoLayout)
                case 5:
                    // Lấy sản phẩm thuộc danh mục này để hiển thị dạng Highlight
                    var promoProducts = db.Products
                                          .Where(p => p.ProductNavbarLinks.Any(link => link.NavbarItemId == id))
                                          .OrderByDescending(p => p.Price) // Ưu tiên hàng đắt tiền để quảng bá
                                          .Take(5)
                                          .ToList();
                    return View("PromoLayout", promoProducts);

                default: // Grid Layout (Mặc định)
                    var targetNavbarIds = new List<int>();
                    targetNavbarIds.Add(id.Value);

                    var childIds = db.NavbarItems
                                     .Where(n => n.ParentId == id)
                                     .Select(n => n.ItemId)
                                     .ToList();
                    targetNavbarIds.AddRange(childIds);

                    var products = db.Products
                                     .Where(p => p.ProductNavbarLinks.Any(link => targetNavbarIds.Contains(link.NavbarItemId)))
                                     .OrderByDescending(p => p.CreatedAt)
                                     .ToList();

                    return View("GridLayout", products);
            }
        }

        // === CÁC ACTION RENDER PHỤ ===

        private ActionResult RenderCart()
        {
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            return View("CartLayout", cart);
        }

        private ActionResult RenderBestsellers()
        {
            var bestsellers = db.Products
                                .Select(p => new {
                                    Product = p,
                                    Sold = db.OrderItems.Where(oi => oi.ProductId == p.ProductId).Sum(oi => (int?)oi.Quantity) ?? 0
                                })
                                .OrderByDescending(x => x.Sold)
                                .Take(8)
                                .ToList()
                                .Select(x => x.Product)
                                .ToList();

            var viewModels = MapToCards(bestsellers);
            return View("BestsellerLayout", viewModels);
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
        // CollectionController.cs

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitContact(string fullName, string email, string phone, string message)
        {
            try
            {
                var contact = new Contact
                {
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    Message = message,
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    Status = 0, // Mới tạo
                    UserId = null // Mặc định null
                };

                // Nếu đã đăng nhập, lưu UserId lại
                if (User.Identity.IsAuthenticated)
                {
                    // Code lấy User hiện tại (helper GetCurrentUser() bạn đã có)
                    var currentUser = GetCurrentUser();
                    if (currentUser != null)
                    {
                        contact.UserId = currentUser.UserId;
                        // Nếu form không nhập, lấy data từ user
                        if (string.IsNullOrEmpty(contact.Email)) contact.Email = currentUser.Email;
                    }
                }

                db.Contacts.Add(contact);
                db.SaveChanges();
                TempData["Message"] = "Gửi yêu cầu thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            // Redirect về trang cũ...
            if (Request.UrlReferrer != null) return Redirect(Request.UrlReferrer.ToString());
            return RedirectToAction("Index", "Home");
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

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}