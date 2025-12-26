using System;
using System.Collections.Generic;
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
            if (Session["UserType"] != null) return "vip".Equals(Session["UserType"] as string, StringComparison.OrdinalIgnoreCase);
            if (User.Identity.IsAuthenticated)
            {
                var username = User.Identity.Name;
                var user = db.Users.FirstOrDefault(u => u.Username == username);
                if (user != null && user.user_type == "vip") return true;
            }
            return false;
        }

        private List<ProductCardViewModel> MapToCards(List<Product> products, bool isVip)
        {
            return products.Select(p => {
                decimal regularPrice = p.Price * (1m - (p.DiscountPercentage ?? 0) / 100m);
                decimal finalPrice = regularPrice;
                bool isVipPrice = false;
                if (isVip && p.vip_price.HasValue && p.vip_price.Value < regularPrice)
                {
                    finalPrice = p.vip_price.Value; isVipPrice = true;
                }
                return new ProductCardViewModel
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    ImageUrl = p.ImageUrl,
                    OriginalPrice = p.Price,
                    DiscountPercentage = p.DiscountPercentage ?? 0,
                    FinalPrice = finalPrice,
                    IsVipPrice = isVipPrice
                };
            }).ToList();
        }

        public ActionResult Index()
        {
            var viewModel = new HomeViewModel();
            bool isVip = IsUserVip();
            var settings = db.Settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);

            viewModel.ProductsPerRow = settings.ContainsKey("products_per_row") && int.TryParse(settings["products_per_row"], out int perRow) ? perRow : 5;

            // --- 1. LOGIC FLASH SALE ---
            bool isFlashSaleActive = false;
            if (settings.ContainsKey("flash_sale_active"))
            {
                string val = settings["flash_sale_active"].Trim().ToLower();
                if (val == "true" || val == "1" || val == "on" || val == "yes") isFlashSaleActive = true;
            }

            DateTime start, end;
            if (!DateTime.TryParse(settings.ContainsKey("flash_sale_start") ? settings["flash_sale_start"] : null, out start)) start = new DateTime(2000, 1, 1);
            if (!DateTime.TryParse(settings.ContainsKey("flash_sale_end") ? settings["flash_sale_end"] : null, out end)) end = new DateTime(2099, 12, 31);

            DateTime now = DateTime.Now;
            bool isRunning = isFlashSaleActive && (now >= start) && (now <= end);

            // Truyền thời gian kết thúc sang View để làm đồng hồ đếm ngược
            ViewBag.IsFlashSaleRunning = isRunning;
            ViewBag.FlashSaleEndTime = end;

            viewModel.MinPrice = null;
            viewModel.MaxPrice = null;

            // --- 2. BANNER & SIDEBAR ---
            viewModel.Banners = db.Banners.Where(b => b.IsActive).OrderBy(b => b.DisplayOrder).ToList();

            var allNavItems = db.NavbarItems.Where(n => n.ItemVisible == true).ToList();
            var brandParent = allNavItems.FirstOrDefault(n => n.ItemText.Trim().Equals("Thương Hiệu", StringComparison.OrdinalIgnoreCase) || n.ItemText.Trim().Equals("THƯƠNG HIỆU", StringComparison.OrdinalIgnoreCase));

            viewModel.SidebarItems = brandParent != null
                ? allNavItems.Where(n => n.ParentId == brandParent.ItemId).OrderBy(n => n.ItemOrder).ThenBy(n => n.ItemText).ToList()
                : new List<NavbarItem>();

            // --- 3. SECTIONS ---
            viewModel.Sections = new List<HomeSection>();

            // Section 1: FLASH SALE (Lấy từ DB Navbar có LayoutType = 2)
            if (isRunning)
            {
                var flashSaleMenu = db.NavbarItems.FirstOrDefault(n => n.LayoutType == 2);
                if (flashSaleMenu != null)
                {
                    var flashSaleProds = db.Products
                                           .Where(p => p.ProductNavbarLinks.Any(link => link.NavbarItemId == flashSaleMenu.ItemId)
                                                    && p.DiscountPercentage > 0)
                                           .OrderByDescending(p => p.DiscountPercentage)
                                           .Take(10)
                                           .ToList();

                    if (flashSaleProds.Any())
                    {
                        viewModel.Sections.Add(new HomeSection
                        {
                            SectionId = 1,
                            Title = "FLASH SALE",
                            LayoutType = 2, // Đánh dấu là Flash Sale để View render khác biệt
                            ViewAllUrl = Url.Action("Index", "Collection", new { id = flashSaleMenu.ItemId }),
                            Products = MapToCards(flashSaleProds, isVip)
                        });
                    }
                }
            }

            // Section 2: HÀNG MỚI VỀ
            var newArrivalProds = db.Products.OrderByDescending(p => p.ProductId).Take(10).ToList();
            if (newArrivalProds.Any())
                viewModel.Sections.Add(new HomeSection { SectionId = 2, Title = "HÀNG MỚI VỀ", LayoutType = 0, ViewAllUrl = Url.Action("Index", "Shop", new { sort = "newest" }), Products = MapToCards(newArrivalProds, isVip) });

            // Section 3: GỢI Ý HÔM NAY
            var allProds = db.Products.OrderBy(p => p.StockQuantity).Take(24).ToList();
            if (allProds.Any())
                viewModel.Sections.Add(new HomeSection { SectionId = 3, Title = "GỢI Ý HÔM NAY", LayoutType = 1, ViewAllUrl = Url.Action("Index", "Shop"), Products = MapToCards(allProds, isVip) });

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
                // SỬA LẠI ĐÚNG NHƯ CODE CŨ CỦA BẠN (KHÔNG DÙNG Microsoft.AspNet...)
                var userIdString = User.Identity.GetUserId();

                if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId))
                {
                    var user = db.Users.Find(userId);
                    if (user != null) { model.UserFirstName = user.FirstName; model.UserAvatarUrl = user.AvatarUrl; }
                }
            }
            var settings = db.Settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);
            model.LogoUrl = settings.ContainsKey("logo_url") ? settings["logo_url"] : "";
            model.WelcomeText = settings.ContainsKey("welcome_text") ? settings["welcome_text"] : "Chào,";
            if (settings.ContainsKey("show_search_bar"))
            {
                string val = settings["show_search_bar"].Trim().ToLower();
                model.ShowSearchBar = (val == "true" || val == "1" || val == "on" || val == "yes");
            }
            else { model.ShowSearchBar = true; }
            model.NavbarItems = db.NavbarItems.Where(n => n.ItemVisible == true).OrderBy(n => n.ItemOrder).ToList();
            return PartialView("_HeaderPartial", model);
        }

        [ChildActionOnly][AllowAnonymous] public ActionResult Footer() { return PartialView("_FooterPartial", new FooterViewModel()); }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}