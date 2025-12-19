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

        // Helper: Kiểm tra user có phải VIP không
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

            // Tính toán giá
            decimal regularSalePrice = productEntity.Price * (1m - (productEntity.DiscountPercentage ?? 0) / 100m);
            decimal finalPrice = regularSalePrice;
            bool isVipPrice = false;

            // Logic giá VIP
            if (isVip && productEntity.vip_price.HasValue)
            {
                if (productEntity.vip_price.Value < regularSalePrice)
                {
                    finalPrice = productEntity.vip_price.Value;
                    isVipPrice = true;
                }
            }

            // Lấy đánh giá
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
                    DiscountPercentage = productEntity.DiscountPercentage ?? 0,
                    FinalPrice = finalPrice,
                    IsVipPrice = isVipPrice
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

            // Lấy sản phẩm liên quan (cùng danh mục)
            ViewBag.RelatedProducts = db.Products
                                        .Where(p => p.CategoryId == productEntity.CategoryId && p.ProductId != id)
                                        .Take(4)
                                        .ToList();

            return View(viewModel);
        }

        // Action thêm đánh giá
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddReview(ReviewViewModel model)
        {
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
        // 2. TRANG TÌM KIẾM (SEARCH) - ĐÃ CẬP NHẬT LOGIC NAVBAR
        // ==========================================================
        [ValidateInput(false)]
        public ActionResult Search(string keyword, int? selectedCategoryId, int? selectedPriceRangeId, int? page)
        {
            var viewModel = new ProductSearchViewModel();

            // 1. Load danh sách Thương hiệu từ NavbarItems (thay vì Categories cũ)
            // Chỉ lấy các mục là Layout sản phẩm (0: Grid, 2: Sale, 4: Gallery)
            viewModel.Categories = db.NavbarItems
                                     .Where(n => n.ItemVisible == true && (n.LayoutType == 0 || n.LayoutType == 2 || n.LayoutType == 4))
                                     .OrderBy(n => n.ItemOrder)
                                     .Select(n => new SelectListItem
                                     {
                                         Value = n.ItemId.ToString(),
                                         Text = n.ItemText,
                                         Selected = n.ItemId == selectedCategoryId
                                     })
                                     .ToList();

            // 2. Load khoảng giá
            viewModel.PriceRanges = db.PriceRanges
                                      .OrderBy(r => r.RangeOrder)
                                      .Select(r => new SelectListItem
                                      {
                                          Value = r.RangeId.ToString(),
                                          Text = r.RangeLabel,
                                          Selected = r.RangeId == selectedPriceRangeId
                                      })
                                      .ToList();

            // 3. Xây dựng truy vấn tìm kiếm
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

            // 4. Kiểm tra VIP để tính giá
            bool isVip = IsUserVip();

            // 5. Chuyển đổi sang ViewModel
            var resultsQuery = query.ToList().Select(p => new
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

                // Logic chọn giá cuối cùng
                FinalPrice = (isVip && x.Product.vip_price.HasValue && x.Product.vip_price.Value < x.RegularSalePrice)
                             ? x.Product.vip_price.Value
                             : x.RegularSalePrice,

                IsVipPrice = (isVip && x.Product.vip_price.HasValue && x.Product.vip_price.Value < x.RegularSalePrice)
            });

            // Sắp xếp mặc định: Mới nhất lên đầu
            var sortedQuery = resultsQuery.OrderByDescending(p => p.ProductId);

            // 6. Phân trang
            int pageSize = 9;
            int pageNumber = (page ?? 1);
            viewModel.Results = sortedQuery.ToPagedList(pageNumber, pageSize);

            // Gán lại giá trị bộ lọc để View hiển thị
            viewModel.Keyword = keyword;
            viewModel.SelectedCategoryId = selectedCategoryId;
            viewModel.SelectedPriceRangeId = selectedPriceRangeId;

            return View(viewModel);
        }

        // ==========================================================
        // 3. CÁC TRANG KHÁC (GIỮ NGUYÊN ĐỂ KHÔNG BỊ LỖI)
        // ==========================================================
        public ActionResult Bestsellers()
        {
            // Logic lấy Top bán chạy
            var topProducts = db.OrderItems
                                .GroupBy(oi => oi.ProductId)
                                .OrderByDescending(g => g.Sum(oi => oi.Quantity))
                                .Select(g => g.Key)
                                .Take(12)
                                .ToList();

            var products = db.Products.Where(p => topProducts.Contains(p.ProductId)).ToList();
            if (!products.Any()) products = db.Products.OrderByDescending(p => p.CreatedAt).Take(8).ToList();

            return View(products);
        }

        public ActionResult SaleProducts()
        {
            // Logic lấy sản phẩm giảm giá
            var saleProducts = db.Products
                                 .Where(p => p.DiscountPercentage > 0)
                                 .OrderByDescending(p => p.DiscountPercentage)
                                 .ToList();
            return View("~/Views/Collection/SaleLayout.cshtml", saleProducts);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}