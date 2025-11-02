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
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        private bool IsUserVip()
        {
            return "vip".Equals(Session["UserType"] as string, StringComparison.OrdinalIgnoreCase);
        }

        public ActionResult Index()
        {
            var layoutSetting = db.Settings.FirstOrDefault(s => s.SettingKey == "products_per_row")?.SettingValue;
            int productsPerRow = int.TryParse(layoutSetting, out int rows) ? rows : 4;

            bool isVip = IsUserVip();

            var banners = db.Banners
                            .Where(b => b.IsActive)
                            .OrderBy(b => b.DisplayOrder)
                            .ToList();

            var allProductsWithPrice = db.Products
                .Include(p => p.Category)
                .Select(p => new
                {
                    Product = p,
                    CategoryName = p.Category.Name,
                    RegularSalePrice = p.Price * (1m - (p.DiscountPercentage ?? 0) / 100m)
                })
                .ToList()
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

            var productsByBrand = allProductsWithPrice
                .GroupBy(p => p.CategoryName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => p.Card)
                          .OrderByDescending(card => card.ProductId)
                          .Take(productsPerRow)
                          .ToList()
                );

            var viewModel = new HomeViewModel
            {
                ProductsPerRow = productsPerRow,
                Banners = banners,
                ProductsByBrand = productsByBrand
            };

            return View(viewModel);
        }

        [ChildActionOnly]
        [AllowAnonymous]
        public ActionResult Header()
        {
            var model = new HeaderViewModel();
            model.IsLoggedIn = User.Identity.IsAuthenticated;

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

            var settings = db.Settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);
            model.LogoUrl = settings.GetValueOrDefault("logo_url", ""); // Để trống nếu không có logo
            model.WelcomeText = settings.GetValueOrDefault("welcome_text", "Chào,");

            model.NavbarItems = db.NavbarItems
                                  .Where(n => n.ItemVisible == true)
                                  .OrderBy(n => n.ItemOrder)
                                  .Select(n => new NavbarItemViewModel { ItemText = n.ItemText, ItemUrl = n.ItemUrl })
                                  .ToList();

            model.Categories = db.Categories.OrderBy(c => c.Name).ToList();

            // === SỬA ĐỔI QUAN TRỌNG: ĐỌC CÀI ĐẶT CỦA ADMIN ===
            model.ShowSearchBar = bool.Parse(settings.GetValueOrDefault("show_search_bar", "true")); // Mặc định là bật

            return PartialView("_HeaderPartial", model);
        }

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