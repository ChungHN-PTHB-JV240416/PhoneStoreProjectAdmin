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

        // === ACTION 1: DANH SÁCH SẢN PHẨM ===
        public ActionResult Products(string search_query, string message)
        {
            // Include NavbarItem để có thể hiển thị tên danh mục nếu cần sau này
            var products = db.Products.Include(p => p.NavbarItem).AsQueryable();

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

        // === HÀM PHỤ: TẠO DROPDOWN DANH MỤC TỪ MENU (NAVBAR) ===
        private void PopulateNavbarDropDownList(object selectedId = null)
        {
            // Lấy danh sách từ NavbarItems thay vì Categories cũ
            var items = db.NavbarItems
                          .OrderBy(n => n.ItemOrder)
                          .Select(n => new
                          {
                              ItemId = n.ItemId,
                              // Hiển thị tên kèm Bố cục để Admin dễ phân biệt
                              DisplayText = n.ItemText + (
                                  n.LayoutType == 0 ? " (Lưới SP)" :
                                  n.LayoutType == 2 ? " (Flash Sale)" :
                                  n.LayoutType == 4 ? " (Gallery)" :
                                  " (Khác)")
                          })
                          .ToList();

            // Vẫn giữ tên ViewBag.Categories để không phải sửa file View .cshtml
            ViewBag.Categories = new SelectList(items, "ItemId", "DisplayText", selectedId);
        }

        // === ACTION 2: TRANG THÊM/SỬA SẢN PHẨM (GET) ===
        // GET: Admin/ProductAdmin/EditProduct/5
        public ActionResult EditProduct(int id)
        {
            // Nếu id = 0 là Thêm mới
            if (id == 0) 
            {
                PopulateNavbarDropDownList(); // Load danh sách Menu
                ViewBag.Title = "Thêm Sản phẩm mới";
                return View(new ProductAdminEditViewModel());
            }

            // Nếu id > 0 là Sửa
            var productEntity = db.Products.Find(id);
            if (productEntity == null) return HttpNotFound();

            PopulateNavbarDropDownList(productEntity.CategoryId); // Load danh sách Menu và chọn mục hiện tại

            ViewBag.Title = "Sửa Sản phẩm: " + productEntity.Name;

            var viewModel = new ProductAdminEditViewModel
            {
                ProductId = productEntity.ProductId,
                Name = productEntity.Name,
                Description = productEntity.Description,
                Price = productEntity.Price,
                StockQuantity = productEntity.StockQuantity,
                CategoryId = productEntity.CategoryId, // Đây chính là ID của NavbarItem
                DiscountPercentage = productEntity.DiscountPercentage ?? 0,
                CurrentImageUrl = productEntity.ImageUrl,
                PurchasePrice = productEntity.PurchasePrice,
                vip_price = productEntity.vip_price
            };

            return View(viewModel);
        }

        // === ACTION 3: LƯU SẢN PHẨM (POST) ===
        // POST: Admin/ProductAdmin/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(ProductAdminEditViewModel model, HttpPostedFileBase imageFile)
        {
            if (!ModelState.IsValid)
            {
                // Nếu lỗi, load lại dropdown
                PopulateNavbarDropDownList(model.CategoryId);
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
                    CategoryId = model.CategoryId, // Lưu liên kết với NavbarItem
                    DiscountPercentage = model.DiscountPercentage,
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.Now,
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
                    productToUpdate.CategoryId = model.CategoryId; // Cập nhật liên kết
                    productToUpdate.DiscountPercentage = model.DiscountPercentage;
                    productToUpdate.ImageUrl = imageUrl;
                    productToUpdate.PurchasePrice = model.PurchasePrice;
                    productToUpdate.vip_price = model.vip_price;

                    db.Entry(productToUpdate).State = EntityState.Modified;
                    TempData["Message"] = "Sản phẩm đã được cập nhật thành công.";
                }
            }

            db.SaveChanges();
            return RedirectToAction("Products");
        }

        // === ACTION 4: XÓA SẢN PHẨM ===
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