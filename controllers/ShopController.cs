using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;
using PagedList;

namespace PhoneStore_New.Controllers
{
    public class ShopController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        private List<PriceRangeItem> GetPriceRanges()
        {
            return new List<PriceRangeItem>
            {
                new PriceRangeItem { Id = 1, Text = "Dưới 5 triệu", Min = 0, Max = 5000000 },
                new PriceRangeItem { Id = 2, Text = "Từ 5 - 10 triệu", Min = 5000000, Max = 10000000 },
                new PriceRangeItem { Id = 3, Text = "Từ 10 - 20 triệu", Min = 10000000, Max = 20000000 },
                new PriceRangeItem { Id = 4, Text = "Trên 20 triệu", Min = 20000000, Max = null }
            };
        }

        private ProductCardViewModel MapToCard(Product p, bool isVip)
        {
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
        }

        public ActionResult Index(string search, int? categoryId, int? priceRangeId, string sort, int? page)
        {
            var query = db.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search));
                ViewBag.Title = $"Tìm kiếm: {search}";
            }
            else ViewBag.Title = "Cửa hàng";

            if (categoryId.HasValue)
            {
                // Logic Many-to-Many: 
                // Product belongs to (This Category OR any of its Children) via ProductNavbarLinks
                var catIds = new List<int> { categoryId.Value };
                var childIds = db.NavbarItems.Where(n => n.ParentId == categoryId.Value).Select(n => n.ItemId).ToList();
                catIds.AddRange(childIds);

                // Use .Any() to check if product has a link to any of these category IDs
                query = query.Where(p => p.ProductNavbarLinks.Any(link => catIds.Contains(link.NavbarItemId)));
            }

            if (priceRangeId.HasValue)
            {
                var range = GetPriceRanges().FirstOrDefault(r => r.Id == priceRangeId.Value);
                if (range != null)
                {
                    if (range.Min.HasValue) query = query.Where(p => p.Price >= range.Min.Value);
                    if (range.Max.HasValue) query = query.Where(p => p.Price <= range.Max.Value);
                }
            }

            switch (sort)
            {
                case "price_asc": query = query.OrderBy(p => p.Price); break;
                case "price_desc": query = query.OrderByDescending(p => p.Price); break;
                case "newest": query = query.OrderByDescending(p => p.ProductId); break;
                default: query = query.OrderByDescending(p => p.CreatedAt); break;
            }

            int pageSize = 12;
            int pageNumber = (page ?? 1);

            bool isVip = false;
            if (Session["UserType"] != null && Session["UserType"].ToString().ToLower() == "vip") isVip = true;
            else if (User.Identity.IsAuthenticated)
            {
                var u = db.Users.FirstOrDefault(x => x.Username == User.Identity.Name);
                if (u != null && u.user_type == "vip") isVip = true;
            }

            var pagedProducts = query.ToPagedList(pageNumber, pageSize);
            var cardList = pagedProducts.Select(p => MapToCard(p, isVip)).ToList();
            var pagedModel = new StaticPagedList<ProductCardViewModel>(cardList, pagedProducts.GetMetaData());

            var brandParent = db.NavbarItems.FirstOrDefault(n => n.ItemText.Trim().Equals("Thương Hiệu", StringComparison.OrdinalIgnoreCase)
                                                              || n.ItemText.Trim().Equals("THƯƠNG HIỆU", StringComparison.OrdinalIgnoreCase));

            List<NavbarItem> brandChildren;
            if (brandParent != null)
            {
                brandChildren = db.NavbarItems
                                  .Where(n => n.ParentId == brandParent.ItemId && n.ItemVisible == true)
                                  .OrderBy(n => n.ItemOrder)
                                  .ThenBy(n => n.ItemText)
                                  .ToList();
            }
            else
            {
                brandChildren = db.NavbarItems.Where(n => n.ParentId == null && n.ItemVisible == true).ToList();
            }

            var model = new ShopViewModel
            {
                Products = pagedModel,
                Search = search,
                CategoryId = categoryId,
                PriceRangeId = priceRangeId,
                Sort = sort,
                FilterCategories = brandChildren,
                CategoryList = new SelectList(brandChildren, "ItemId", "ItemText", categoryId),
                PriceRangeList = new SelectList(GetPriceRanges(), "Id", "Text", priceRangeId)
            };

            return View(model);
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}