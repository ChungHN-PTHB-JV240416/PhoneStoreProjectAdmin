using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;
using PagedList;

namespace PhoneStore_New.Controllers
{
    public class ProductController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        private bool IsUserVip()
        {
            if (Session["UserType"] == null) return false;
            return "vip".Equals(Session["UserType"] as string, StringComparison.OrdinalIgnoreCase);
        }

        // ==========================================================
        // 1. TRANG CHI TIẾT SẢN PHẨM
        // ==========================================================
        public ActionResult Detail(int id)
        {
            var productEntity = db.Products.Find(id);
            if (productEntity == null) return HttpNotFound();

            bool isVip = IsUserVip();
            decimal regularSalePrice = productEntity.Price * (1m - (productEntity.DiscountPercentage ?? 0) / 100m);
            decimal finalPrice = regularSalePrice;
            bool isVipPrice = false;

            if (isVip && productEntity.vip_price.HasValue && productEntity.vip_price.Value < regularSalePrice)
            {
                finalPrice = productEntity.vip_price.Value;
                isVipPrice = true;
            }

            var reviews = db.Reviews.Include(r => r.User).Where(r => r.ProductId == id).OrderByDescending(r => r.CreatedAt).ToList();
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
                    DiscountPercentage = productEntity.DiscountPercentage ?? 0,
                    FinalPrice = finalPrice,
                    IsVipPrice = isVipPrice
                },
                AverageRating = avgRating,
                Reviews = reviews.Select(r => new ReviewViewModel { FirstName = r.User?.FirstName ?? "Ẩn danh", CommentText = r.CommentText, Rating = r.Rating, CreatedAt = r.CreatedAt }).ToList(),
                FinalPrice = finalPrice,
                OriginalPrice = productEntity.Price,
                IsVipPrice = isVipPrice
            };

            // Related Products: Find products that share AT LEAST ONE category with the current product
            var currentCategoryIds = productEntity.ProductNavbarLinks.Select(l => l.NavbarItemId).ToList();

            ViewBag.RelatedProducts = db.Products
                                        .Where(p => p.ProductId != id &&
                                                    p.ProductNavbarLinks.Any(l => currentCategoryIds.Contains(l.NavbarItemId)))
                                        .Take(4)
                                        .ToList();

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddReview(ReviewViewModel model)
        {
            /* ... (Keep existing review logic) ... */
            if (ModelState.IsValid)
            {
                string currentUsername = User.Identity.Name;
                var currentUser = db.Users.FirstOrDefault(u => u.Username == currentUsername || u.Email == currentUsername);

                if (currentUser != null)
                {
                    var newReview = new Review
                    {
                        ProductId = model.ProductId,
                        UserId = currentUser.UserId,
                        CommentText = model.CommentText,
                        Rating = model.Rating,
                        CreatedAt = DateTime.Now
                    };
                    db.Reviews.Add(newReview);
                    db.SaveChanges();
                    TempData["Message"] = "Đánh giá của bạn đã được gửi thành công!";
                }
            }
            return RedirectToAction("Detail", new { id = model.ProductId });
        }

        // ==========================================================
        // 2. TRANG TÌM KIẾM (SEARCH)
        // ==========================================================
        [ValidateInput(false)]
        public ActionResult Search(string keyword, int? selectedCategoryId, int? selectedPriceRangeId, int? page)
        {
            var viewModel = new ProductSearchViewModel();

            var brandParent = db.NavbarItems.FirstOrDefault(n => n.ItemText.Trim().Equals("Thương Hiệu", StringComparison.OrdinalIgnoreCase)
                                                              || n.ItemText.Trim().Equals("THƯƠNG HIỆU", StringComparison.OrdinalIgnoreCase));

            List<NavbarItem> brandChildren;
            if (brandParent != null)
            {
                brandChildren = db.NavbarItems.Where(n => n.ParentId == brandParent.ItemId && n.ItemVisible == true)
                                              .OrderBy(n => n.ItemOrder).ThenBy(n => n.ItemText).ToList();
            }
            else
            {
                brandChildren = db.NavbarItems.Where(n => n.ParentId == null && n.ItemVisible == true).ToList();
            }

            viewModel.Categories = brandChildren.Select(n => new SelectListItem
            {
                Value = n.ItemId.ToString(),
                Text = n.ItemText,
                Selected = n.ItemId == selectedCategoryId
            }).ToList();

            viewModel.PriceRanges = db.PriceRanges.OrderBy(r => r.RangeOrder)
                                      .Select(r => new SelectListItem { Value = r.RangeId.ToString(), Text = r.RangeLabel, Selected = r.RangeId == selectedPriceRangeId })
                                      .ToList();

            var query = db.Products.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p => p.Name.Contains(keyword) || p.Description.Contains(keyword));
            }

            if (selectedCategoryId.HasValue)
            {
                // Many-to-Many Filter
                query = query.Where(p => p.ProductNavbarLinks.Any(l => l.NavbarItemId == selectedCategoryId.Value));
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

            var resultsQuery = query.ToList().Select(p => {
                decimal regularPrice = p.Price * (1m - (p.DiscountPercentage ?? 0) / 100m);
                decimal finalPrice = regularPrice;
                bool isVipPrice = false;

                if (isVip && p.vip_price.HasValue && p.vip_price.Value < regularPrice)
                {
                    finalPrice = p.vip_price.Value;
                    isVipPrice = true;
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
            });

            var sortedQuery = resultsQuery.OrderByDescending(p => p.ProductId);

            int pageSize = 9;
            int pageNumber = (page ?? 1);
            viewModel.Results = sortedQuery.ToPagedList(pageNumber, pageSize);

            viewModel.Keyword = keyword;
            viewModel.SelectedCategoryId = selectedCategoryId;
            viewModel.SelectedPriceRangeId = selectedPriceRangeId;

            return View(viewModel);
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}