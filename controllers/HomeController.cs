using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;

namespace PhoneStore_New.Controllers
{
    public class HomeController : Controller
    {
        // Đảm bảo tên DbContext này (PhoneStoreDBEntities) khớp với file Web.config
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // Hàm helper an toàn để kiểm tra VIP
        private bool IsUserVip()
        {
            return "vip".Equals(Session["UserType"] as string, StringComparison.OrdinalIgnoreCase);
        }

        // Action Trang chủ (Index)
        public ActionResult Index()
        {
            // Lấy cài đặt số cột
            var layoutSetting = db.Settings.FirstOrDefault(s => s.SettingKey == "products_per_row")?.SettingValue;
            int productsPerRow = int.TryParse(layoutSetting, out int rows) ? rows : 4;

            bool isVip = IsUserVip();

            // Lấy danh sách Banner
            var banners = db.Banners
                            .Where(b => b.IsActive)
                            .OrderBy(b => b.DisplayOrder)
                            .ToList();

            // Lấy và xử lý logic giá cho tất cả sản phẩm
            var allProductsWithPrice = db.Products
                .Include(p => p.Category) // Join với Category để lấy tên
                .Select(p => new
                {
                    Product = p,
                    CategoryName = p.Category.Name, // Lấy tên thương hiệu
                    RegularSalePrice = p.Price * (1m - (p.DiscountPercentage ?? 0) / 100m)
                })
                .ToList() // Lấy về bộ nhớ để xử lý logic VIP
                .Select(x => new
                {
                    CategoryName = x.CategoryName ?? "Chưa phân loại",
                    Card = new ProductCardViewModel
                    {
                        ProductId = x.Product.ProductId,
                        Name = x.Product.Name,
                        ImageUrl = x.Product.ImageUrl,
                        OriginalPrice = x.Product.Price,
                        DiscountPercentage = x.Product.DiscountPercentage ?? 0,

                        FinalPrice = (isVip && x.Product.vip_price.HasValue && x.Product.vip_price.Value < x.RegularSalePrice)
                                     ? x.Product.vip_price.Value
                                     : x.RegularSalePrice,

                        IsVipPrice = (isVip && x.Product.vip_price.HasValue && x.Product.vip_price.Value < x.RegularSalePrice)
                    }
                })
                .ToList();

            // Gom nhóm sản phẩm theo Tên Thương hiệu
            var productsByBrand = allProductsWithPrice
                .GroupBy(p => p.CategoryName)
                .ToDictionary(
                    g => g.Key, // Key là tên thương hiệu (ví dụ: "iPhone")
                    g => g.Select(p => p.Card)
                          .OrderByDescending(card => card.ProductId) // Lấy sản phẩm mới nhất lên đầu
                          .Take(productsPerRow) // Chỉ lấy số lượng sản phẩm bằng số cột
                          .ToList()
                );

            // Đóng gói ViewModel để gửi đi
            var viewModel = new HomeViewModel
            {
                ProductsPerRow = productsPerRow,
                Banners = banners,
                ProductsByBrand = productsByBrand
            };

            return View(viewModel);
        }

        // Action con, vẽ thanh Header (Navbar)
        [ChildActionOnly]
        [AllowAnonymous]
        public ActionResult Header()
        {
            var model = new HeaderViewModel();
            model.IsLoggedIn = User.Identity.IsAuthenticated;

            // Lấy thông tin User nếu đã đăng nhập
            if (model.IsLoggedIn)
            {
                var userIdString = IdentityExtensions.GetUserId(User.Identity);
                if (int.TryParse(userIdString, out int userId))
                {
                    var user = db.Users.Find(userId);
                    if (user != null)
                    {
                        model.UserFirstName = user.FirstName;
                        model.UserAvatarUrl = user.AvatarUrl;
                    }
                }
            }

            // Lấy các cài đặt chung
            var settings = db.Settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);
            model.LogoUrl = settings.GetValueOrDefault("logo_url", "");
            model.WelcomeText = settings.GetValueOrDefault("welcome_text", "Chào,");
            model.ShowSearchBar = bool.Parse(settings.GetValueOrDefault("show_search_bar", "true")); // Lấy cài đặt bật/tắt search

            // Lấy các mục Menu (đã hỗ trợ đa cấp)
            model.NavbarItems = db.NavbarItems
                                  .Where(n => n.ItemVisible == true)
                                  .OrderBy(n => n.ItemOrder)
                                  .ToList();

            // Lấy các danh mục (cho menu "Thương hiệu" cũ)
            model.Categories = db.Categories.Where(c => c.ParentId == null).OrderBy(c => c.Name).ToList(); // Chỉ lấy mục cha

            return PartialView("_HeaderPartial", model);
        }

        // Action con, vẽ Footer
        [ChildActionOnly]
        [AllowAnonymous]
        public ActionResult Footer()
        {
            var settings = db.Settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);

            var model = new FooterViewModel
            {
                FooterText = settings.GetValueOrDefault("footer_text"),
                FooterAddress = settings.GetValueOrDefault("footer_address"),
                FooterPhone = settings.GetValueOrDefault("footer_phone")
            };

            return PartialView("_FooterPartial", model);
        }

        // Dọn dẹp DbContext
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