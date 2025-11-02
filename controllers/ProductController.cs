using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;
using System;
using PagedList; // Đảm bảo đã có using này

namespace PhoneStore_New.Controllers
{
    public class ProductController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        private bool IsUserVip()
        {
            return "vip".Equals(Session["UserType"] as string, StringComparison.OrdinalIgnoreCase);
        }

        // GET: /Product/Detail/5
        public ActionResult Detail(int id)
        {
            var productEntity = db.Products.Find(id);

            if (productEntity == null)
            {
                return View("ProductNotFound");
            }

            bool isVip = IsUserVip();

            decimal regularSalePrice = productEntity.Price * (1m - (productEntity.DiscountPercentage ?? 0) / 100m);
            decimal finalPrice = regularSalePrice;
            bool isVipPrice = false;

            if (isVip && productEntity.vip_price.HasValue)
            {
                if (productEntity.vip_price.Value < regularSalePrice)
                {
                    finalPrice = productEntity.vip_price.Value;
                    isVipPrice = true;
                }
            }

            var reviews = db.Reviews
                            .Include(r => r.User)
                            .Where(r => r.ProductId == id)
                            .OrderByDescending(r => r.CreatedAt)
                            .ToList();

            decimal avgRating = reviews.Any() ? (decimal)reviews.Average(r => r.Rating) : 0m;

            var viewModel = new ProductDetailViewModel
            {
                Product = new ProductCardViewModel
                {
                    ProductId = productEntity.ProductId,
                    Name = productEntity.Name,
                    ImageUrl = productEntity.ImageUrl,
                    Description = productEntity.Description,
                    StockQuantity = productEntity.StockQuantity,
                    OriginalPrice = productEntity.Price,
                    DiscountPercentage = productEntity.DiscountPercentage ?? 0
                },

                AverageRating = avgRating,
                Reviews = reviews.Select(r => new ReviewViewModel
                {
                    FirstName = r.User?.FirstName ?? "Người dùng ẩn",
                    CommentText = r.CommentText,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt
                }).ToList(),
                Message = (TempData["Message"] as string),

                FinalPrice = finalPrice,
                OriginalPrice = productEntity.Price,
                IsVipPrice = isVipPrice
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddReview(ReviewViewModel model)
        {
            if (ModelState.IsValid)
            {
                var newReview = new Review
                {
                    ProductId = model.ProductId,
                    UserId = int.Parse(IdentityExtensions.GetUserId(User.Identity)),
                    CommentText = model.CommentText,
                    Rating = model.Rating,
                    CreatedAt = System.DateTime.Now
                };
                db.Reviews.Add(newReview);
                db.SaveChanges();
                TempData["Message"] = "Bình luận của bạn đã được gửi thành công!";
            }
            return RedirectToAction("Detail", new { id = model.ProductId });
        }

        // === BẮT ĐẦU SỬA ĐỔI PHÂN TRANG CHO 'ByCategory' ===
        public ActionResult ByCategory(int id, int? page)
        {
            var category = db.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }

            ViewBag.CategoryName = category.Name;
            bool isVip = IsUserVip();

            // 1. Lấy truy vấn (Query) các sản phẩm, CHƯA CHẠY
            var productsQuery = db.Products
                                 .Where(p => p.CategoryId == id)
                                 .OrderByDescending(p => p.CreatedAt) // Sắp xếp theo sản phẩm mới nhất
                                 .Select(p => new
                                 {
                                     Product = p,
                                     RegularSalePrice = p.Price * (1m - (p.DiscountPercentage ?? 0) / 100m)
                                 });

            // 2. Lấy về bộ nhớ để tính giá VIP
            var productCards = productsQuery
                                .ToList()
                                .Select(x => new ProductCardViewModel
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
                                });

            // 3. Thực hiện phân trang
            int pageSize = 9; // 9 sản phẩm một trang
            int pageNumber = (page ?? 1); // Nếu 'page' là null, mặc định là trang 1
            var pagedProducts = productCards.ToPagedList(pageNumber, pageSize);

            // 4. Trả về View với danh sách đã được phân trang
            return View(pagedProducts);
        }
        // === KẾT THÚC SỬA ĐỔI ===

        public ActionResult SaleProducts()
        {
            bool isVip = IsUserVip();

            var products = db.Products
                             .Where(p => p.DiscountPercentage > 0 || p.vip_price.HasValue)
                             .Include(p => p.Category)
                             .ToList();

            var productsByCategory = products
                .Select(p => new
                {
                    CategoryName = p.Category.Name,
                    Card = new ProductCardViewModel
                    {
                        ProductId = p.ProductId,
                        Name = p.Name,
                        ImageUrl = p.ImageUrl,
                        OriginalPrice = p.Price,
                        DiscountPercentage = p.DiscountPercentage ?? 0,
                        FinalPrice = (isVip && p.vip_price.HasValue && p.vip_price.Value < (p.Price * (1m - (p.DiscountPercentage ?? 0) / 100m)))
                                     ? p.vip_price.Value
                                     : (p.Price * (1m - (p.DiscountPercentage ?? 0) / 100m)),
                        IsVipPrice = (isVip && p.vip_price.HasValue && p.vip_price.Value < (p.Price * (1m - (p.DiscountPercentage ?? 0) / 100m)))
                    }
                })
                .GroupBy(p => p.CategoryName)
                .ToDictionary(g => g.Key, g => g.Select(p => p.Card).ToList());

            var productsPerRowSetting = db.Settings.FirstOrDefault(s => s.SettingKey == "products_per_row")?.SettingValue;
            int productsPerRow = int.TryParse(productsPerRowSetting, out int rows) ? rows : 4;

            var viewModel = new SaleProductsViewModel
            {
                ProductsPerRow = productsPerRow,
                ProductsByCategory = productsByCategory
            };

            return View(viewModel);
        }

        [ValidateInput(false)]
        public ActionResult Search(string keyword, int? selectedCategoryId, int? selectedPriceRangeId, int? page)
        {
            var viewModel = new ProductSearchViewModel();

            viewModel.Categories = db.Categories.Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name }).ToList();
            viewModel.PriceRanges = db.PriceRanges.OrderBy(r => r.RangeOrder).Select(r => new SelectListItem { Value = r.RangeId.ToString(), Text = r.RangeLabel }).ToList();

            var query = db.Products.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p => p.Name.Contains(keyword) || p.Description.Contains(keyword));
            }
            if (selectedCategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == selectedCategoryId.Value);
            }
            if (selectedPriceRangeId.HasValue)
            {
                var range = db.PriceRanges.Find(selectedPriceRangeId.Value);
                if (range != null)
                {
                    query = query.Where(p => p.Price >= range.MinPrice && p.Price <= range.MaxPrice);
                }
            }

            bool isVip = IsUserVip();

            var resultsQuery = query.Select(p => new
            {
                Product = p,
                RegularSalePrice = p.Price * (1m - (p.DiscountPercentage ?? 0) / 100m)
            })
            .Select(x => new ProductCardViewModel
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
            });

            var sortedQuery = resultsQuery.OrderBy(p => p.Name);

            int pageSize = 9;
            int pageNumber = (page ?? 1);
            viewModel.Results = sortedQuery.ToPagedList(pageNumber, pageSize);

            viewModel.Keyword = keyword;
            viewModel.SelectedCategoryId = selectedCategoryId;
            viewModel.SelectedPriceRangeId = selectedPriceRangeId;

            return View(viewModel);
        }

        public ActionResult Bestsellers()
        {
            bool isVip = IsUserVip();
            var productsPerRowSetting = db.Settings.FirstOrDefault(s => s.SettingKey == "products_per_row")?.SettingValue;
            int productsPerRow = int.TryParse(productsPerRowSetting, out int rows) ? rows : 4;

            var allSoldItems = db.OrderItems
                .Include(oi => oi.Product.Category)
                .GroupBy(oi => new { oi.ProductId, oi.Product.Category.Name })
                .Select(g => new
                {
                    CategoryName = g.Key.Name ?? "Chưa phân loại",
                    ProductId = g.Key.ProductId,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    Product = g.FirstOrDefault().Product
                })
                .Where(p => p.Product != null)
                .ToList();

            var productsByBrand = allSoldItems
                .GroupBy(item => item.CategoryName)
                .ToDictionary(
                    brandGroup => brandGroup.Key,
                    brandGroup => brandGroup
                        .OrderByDescending(item => item.TotalSold)
                        .Take(productsPerRow)
                        .Select(item => new ProductCardViewModel
                        {
                            ProductId = item.ProductId,
                            Name = item.Product.Name,
                            ImageUrl = item.Product.ImageUrl,
                            OriginalPrice = item.Product.Price,
                            DiscountPercentage = item.Product.DiscountPercentage ?? 0,

                            FinalPrice = (isVip && item.Product.vip_price.HasValue && item.Product.vip_price.Value < (item.Product.Price * (1m - (item.Product.DiscountPercentage ?? 0) / 100m)))
                                         ? item.Product.vip_price.Value
                                         : (item.Product.Price * (1m - (item.Product.DiscountPercentage ?? 0) / 100m)),

                            IsVipPrice = (isVip && item.Product.vip_price.HasValue && item.Product.vip_price.Value < (item.Product.Price * (1m - (item.Product.DiscountPercentage ?? 0) / 100m)))
                        })
                        .ToList()
                );

            var viewModel = new HomeViewModel
            {
                ProductsByBrand = productsByBrand,
                ProductsPerRow = productsPerRow,
                Banners = new List<Models.Banner>()
            };

            return View(viewModel);
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