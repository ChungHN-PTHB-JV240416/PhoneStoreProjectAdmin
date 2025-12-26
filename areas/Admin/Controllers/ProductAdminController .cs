using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PhoneStore_New.Models;

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class ProductAdminController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // GET: Admin/ProductAdmin/Products
        public ActionResult Products()
        {
            var products = db.Products.Include(p => p.ProductNavbarLinks.Select(l => l.NavbarItem));
            return View(products.ToList());
        }

        // GET: Admin/ProductAdmin/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // Include cả Log và Danh mục để hiển thị
            Product product = db.Products
                                .Include("ProductNavbarLinks.NavbarItem")
                                .Include("ProductLogs")
                                .FirstOrDefault(p => p.ProductId == id);

            if (product == null) return HttpNotFound();
            return View(product);
        }

        // GET: Admin/ProductAdmin/Create
        public ActionResult Create()
        {
            var categories = db.NavbarItems
                               .Where(n => !n.ItemText.Contains("Thương Hiệu"))
                               .OrderBy(n => n.ItemOrder)
                               .Select(n => new { ItemId = n.ItemId, ItemText = n.ItemText })
                               .ToList();

            ViewBag.NavbarItemIds = new MultiSelectList(categories, "ItemId", "ItemText");
            return View();
        }

        // POST: Admin/ProductAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult Create(Product product, HttpPostedFileBase ImageUpload, int[] SelectedNavbarItemIds)
        {
            if (ModelState.IsValid)
            {
                // 1. Xử lý ảnh
                if (ImageUpload != null && ImageUpload.ContentLength > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(ImageUpload.FileName);
                    string extension = Path.GetExtension(ImageUpload.FileName);
                    fileName = fileName + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                    string path = Path.Combine(Server.MapPath("~/Content/images/products/"), fileName);

                    string folder = Server.MapPath("~/Content/images/products/");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    ImageUpload.SaveAs(path);
                    product.ImageUrl = "~/Content/images/products/" + fileName;
                }

                product.CreatedAt = DateTime.Now;
                db.Products.Add(product);
                db.SaveChanges(); // Lưu để có ProductId

                // 2. Lưu đa danh mục
                if (SelectedNavbarItemIds != null)
                {
                    foreach (var navId in SelectedNavbarItemIds)
                    {
                        db.ProductNavbarLinks.Add(new ProductNavbarLink { ProductId = product.ProductId, NavbarItemId = navId });
                    }
                }

                // 3. Ghi Log tạo mới
                var log = new ProductLog
                {
                    ProductId = product.ProductId,
                    ActionType = "Tạo mới",
                    UpdatedBy = User.Identity.Name ?? "Admin",
                    CreatedAt = DateTime.Now,
                    LogDescription = $"Tạo mới sản phẩm '{product.Name}'. Giá bán: {product.Price:N0}"
                };
                db.ProductLogs.Add(log);

                db.SaveChanges();
                TempData["Message"] = "Thêm mới sản phẩm thành công!";
                return RedirectToAction("Products");
            }

            var categories = db.NavbarItems.Where(n => !n.ItemText.Contains("Thương Hiệu")).OrderBy(n => n.ItemOrder).ToList();
            ViewBag.NavbarItemIds = new MultiSelectList(categories, "ItemId", "ItemText", SelectedNavbarItemIds);
            return View(product);
        }

        // GET: Admin/ProductAdmin/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Product product = db.Products.Include("ProductNavbarLinks").FirstOrDefault(p => p.ProductId == id);
            if (product == null) return HttpNotFound();

            var categories = db.NavbarItems
                               .Where(n => !n.ItemText.Contains("Thương Hiệu"))
                               .OrderBy(n => n.ItemOrder)
                               .ToList();

            var selectedIds = product.ProductNavbarLinks.Select(p => p.NavbarItemId).ToArray();
            ViewBag.NavbarItemIds = new MultiSelectList(categories, "ItemId", "ItemText", selectedIds);
            return View(product);
        }

        // POST: Admin/ProductAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult Edit(Product product, HttpPostedFileBase ImageUpload, int[] SelectedNavbarItemIds)
        {
            if (ModelState.IsValid)
            {
                // 1. Lấy dữ liệu CŨ để so sánh (Dùng AsNoTracking)
                var oldProduct = db.Products.AsNoTracking().FirstOrDefault(p => p.ProductId == product.ProductId);
                if (oldProduct == null) return HttpNotFound();

                // 2. Xử lý ảnh
                if (ImageUpload != null && ImageUpload.ContentLength > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(ImageUpload.FileName);
                    string extension = Path.GetExtension(ImageUpload.FileName);
                    fileName = fileName + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                    string path = Path.Combine(Server.MapPath("~/Content/images/products/"), fileName);
                    ImageUpload.SaveAs(path);
                    product.ImageUrl = "~/Content/images/products/" + fileName;
                }
                else
                {
                    product.ImageUrl = oldProduct.ImageUrl;
                }
                product.CreatedAt = oldProduct.CreatedAt;

                // 3. LOGIC GHI LOG THAY ĐỔI
                List<string> changes = new List<string>();
                if (oldProduct.Name != product.Name) changes.Add($"Tên: {oldProduct.Name} -> {product.Name}");
                if (oldProduct.Price != product.Price) changes.Add($"Giá bán: {oldProduct.Price:N0} -> {product.Price:N0}");
                if (oldProduct.PurchasePrice != product.PurchasePrice) changes.Add($"Giá nhập: {oldProduct.PurchasePrice:N0} -> {product.PurchasePrice:N0}");
                if (oldProduct.StockQuantity != product.StockQuantity) changes.Add($"Kho: {oldProduct.StockQuantity} -> {product.StockQuantity}");
                if (oldProduct.DiscountPercentage != product.DiscountPercentage) changes.Add($"Giảm giá: {oldProduct.DiscountPercentage}% -> {product.DiscountPercentage}%");

                // Check danh mục thay đổi
                var oldCatIds = db.ProductNavbarLinks.Where(x => x.ProductId == product.ProductId).Select(x => x.NavbarItemId).ToList();
                bool isCatChanged = false;
                if (SelectedNavbarItemIds != null)
                {
                    if (oldCatIds.Count != SelectedNavbarItemIds.Length || !oldCatIds.All(SelectedNavbarItemIds.Contains)) isCatChanged = true;
                }
                else if (oldCatIds.Count > 0) isCatChanged = true;

                if (isCatChanged) changes.Add("Cập nhật danh mục");

                if (changes.Any())
                {
                    var log = new ProductLog
                    {
                        ProductId = product.ProductId,
                        ActionType = "Cập nhật",
                        UpdatedBy = User.Identity.Name ?? "Admin",
                        CreatedAt = DateTime.Now,
                        LogDescription = string.Join("; ", changes)
                    };
                    db.ProductLogs.Add(log);
                }

                // 4. Lưu sản phẩm
                db.Entry(product).State = EntityState.Modified;

                // 5. Cập nhật danh mục
                var existingLinks = db.ProductNavbarLinks.Where(p => p.ProductId == product.ProductId);
                db.ProductNavbarLinks.RemoveRange(existingLinks);
                if (SelectedNavbarItemIds != null)
                {
                    foreach (var navId in SelectedNavbarItemIds)
                    {
                        db.ProductNavbarLinks.Add(new ProductNavbarLink { ProductId = product.ProductId, NavbarItemId = navId });
                    }
                }

                db.SaveChanges();
                TempData["Message"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Products");
            }

            var categories = db.NavbarItems.Where(n => !n.ItemText.Contains("Thương Hiệu")).OrderBy(n => n.ItemOrder).ToList();
            ViewBag.NavbarItemIds = new MultiSelectList(categories, "ItemId", "ItemText", SelectedNavbarItemIds);
            return View(product);
        }

        // GET: Admin/ProductAdmin/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Product product = db.Products.Find(id);
            if (product == null) return HttpNotFound();
            return View(product);
        }

        // POST: Admin/ProductAdmin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);
            db.Products.Remove(product);
            db.SaveChanges();
            TempData["Message"] = "Đã xóa sản phẩm thành công!";
            return RedirectToAction("Products");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}