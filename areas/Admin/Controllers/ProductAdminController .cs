using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using PhoneStore_New.Areas.Admin.Models.ViewModels;
using PhoneStore_New.Models;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System;
using PhoneStore_New.Models.ViewModels;

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class ProductAdminController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        public ActionResult Products(string search_query, string message)
        {
            var products = db.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search_query))
            {
                products = products.Where(p => p.Name.Contains(search_query));
            }

            var productList = products
                                .OrderByDescending(p => p.ProductId)
                                .Select(p => new ProductAdminViewModel
                                {
                                    ProductId = p.ProductId,
                                    Name = p.Name,
                                    Price = p.Price,
                                    StockQuantity = p.StockQuantity,
                                    DiscountPercentage = p.DiscountPercentage ?? 0,
                                    ImageUrl = p.ImageUrl
                                }).ToList();

            var viewModel = new ProductAdminListViewModel
            {
                Products = productList,
                SearchQuery = search_query,
                Message = (TempData["Message"] as string) ?? message
            };

            return View(viewModel);
        }

        // GET: Admin/ProductAdmin/EditProduct/5
        public ActionResult EditProduct(int id)
        {
            ViewBag.Categories = db.Categories
                                    .Select(c => new SelectListItem
                                    {
                                        Value = c.CategoryId.ToString(),
                                        Text = c.Name
                                    })
                                    .ToList();

            if (id == 0) // Thêm mới
            {
                ViewBag.Title = "Thêm Sản phẩm mới";
                return View(new ProductAdminEditViewModel());
            }

            // Sửa sản phẩm
            var productEntity = db.Products.Find(id);
            if (productEntity == null) return HttpNotFound();

            ViewBag.Title = "Sửa Sản phẩm: " + productEntity.Name;

            var viewModel = new ProductAdminEditViewModel
            {
                ProductId = productEntity.ProductId,
                Name = productEntity.Name,
                Description = productEntity.Description,
                Price = productEntity.Price,
                StockQuantity = productEntity.StockQuantity,
                CategoryId = productEntity.CategoryId,
                DiscountPercentage = productEntity.DiscountPercentage ?? 0,
                CurrentImageUrl = productEntity.ImageUrl,

                // === SỬA ĐỔI: LẤY DỮ LIỆU MỚI TỪ DATABASE ===
                PurchasePrice = productEntity.PurchasePrice,
                vip_price = productEntity.vip_price
            };

            return View(viewModel);
        }

        // POST: Admin/ProductAdmin/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(ProductAdminEditViewModel model, HttpPostedFileBase imageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = db.Categories.Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name, Selected = c.CategoryId == model.CategoryId }).ToList();
                return View("EditProduct", model);
            }

            string imageUrl = model.CurrentImageUrl;

            if (imageFile != null && imageFile.ContentLength > 0)
            {
                var uploadDir = Server.MapPath("~/uploads/");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var path = Path.Combine(uploadDir, fileName);
                imageFile.SaveAs(path);
                imageUrl = "~/uploads/" + fileName;
            }

            if (model.ProductId == 0) // Thêm mới
            {
                var newProduct = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    StockQuantity = model.StockQuantity,
                    CategoryId = model.CategoryId,
                    DiscountPercentage = model.DiscountPercentage,
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.Now,

                    // === SỬA ĐỔI: LƯU DỮ LIỆU MỚI VÀO DATABASE ===
                    PurchasePrice = model.PurchasePrice,
                    vip_price = model.vip_price
                };
                db.Products.Add(newProduct);
                TempData["Message"] = "Sản phẩm đã được thêm thành công.";
            }
            else // Cập nhật
            {
                var productToUpdate = db.Products.Find(model.ProductId);
                if (productToUpdate != null)
                {
                    productToUpdate.Name = model.Name;
                    productToUpdate.Description = model.Description;
                    productToUpdate.Price = model.Price;
                    productToUpdate.StockQuantity = model.StockQuantity;
                    productToUpdate.CategoryId = model.CategoryId;
                    productToUpdate.DiscountPercentage = model.DiscountPercentage;
                    productToUpdate.ImageUrl = imageUrl;

                    // === SỬA ĐỔI: LƯU DỮ LIỆU MỚI VÀO DATABASE ===
                    productToUpdate.PurchasePrice = model.PurchasePrice;
                    productToUpdate.vip_price = model.vip_price;

                    db.Entry(productToUpdate).State = EntityState.Modified;
                    TempData["Message"] = "Sản phẩm đã được cập nhật thành công.";
                }
            }

            db.SaveChanges();
            return RedirectToAction("Products");
        }

        // POST: Admin/ProductAdmin/DeleteProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProduct(int id)
        {
            var product = db.Products.Find(id);
            if (product != null)
            {
                db.Products.Remove(product);
                db.SaveChanges();
                TempData["Message"] = "Sản phẩm đã được xóa thành công.";
            }
            return RedirectToAction("Products");
        }
    }
}