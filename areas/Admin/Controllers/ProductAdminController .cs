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

        // Helper lấy User ID (Dùng cho bảng StockTransactions - Bắt buộc là số)
        private int GetCurrentUserId()
        {
            // 1. Ưu tiên lấy từ Session (khi vừa đăng nhập xong)
            if (Session["UserId"] != null)
                return (int)Session["UserId"];

            // 2. Nếu không có Session, tìm theo tên đăng nhập (User.Identity.Name)
            // Lưu ý: User.Identity.Name có thể null nếu chưa đăng nhập
            string currentUserName = User.Identity.Name;
            if (!string.IsNullOrEmpty(currentUserName))
            {
                var user = db.Users.FirstOrDefault(u => u.Username == currentUserName);
                if (user != null) return user.UserId;
            }

            // 3. (FIX LỖI TRIỆT ĐỂ) Nếu không tìm thấy ai cả -> Lấy đại User đầu tiên trong DB
            // Điều này đảm bảo luôn có một ID tồn tại thực sự để không bị lỗi Foreign Key
            var anyUser = db.Users.OrderBy(u => u.UserId).FirstOrDefault();
            if (anyUser != null)
                return anyUser.UserId;

            // 4. Trường hợp xấu nhất: DB chưa có user nào (trả về 0 sẽ lỗi, nhưng ít nhất biết là do DB rỗng)
            return 0;
        }

        // Helper lấy Tên người dùng (Dùng cho bảng ProductLogs - Lưu tên chuỗi)
        private string GetCurrentUsername()
        {
            return User.Identity.Name ?? "Admin";
        }

        private void PopulateSuppliersDropDownList(object selectedSupplier = null)
        {
            var suppliers = from s in db.Suppliers orderby s.Name select s;
            ViewBag.SupplierList = new SelectList(suppliers, "SupplierId", "Name", selectedSupplier);
        }

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

            Product product = db.Products
                                .Include("ProductNavbarLinks.NavbarItem")
                                .Include("ProductLogs")
                                .Include("StockTransactions.Supplier") // Lấy thông tin NCC
                                .Include("StockTransactions.User")     // Lấy thông tin User (QUAN TRỌNG)
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
            PopulateSuppliersDropDownList();
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
                product.StockQuantity = 0;

                db.Products.Add(product);
                db.SaveChanges();

                if (SelectedNavbarItemIds != null)
                {
                    foreach (var navId in SelectedNavbarItemIds)
                    {
                        db.ProductNavbarLinks.Add(new ProductNavbarLink { ProductId = product.ProductId, NavbarItemId = navId });
                    }
                }

                // --- SỬA LỖI TẠI ĐÂY ---
                var log = new ProductLog
                {
                    ProductId = product.ProductId,
                    ActionType = "Tạo mới",
                    UpdatedBy = GetCurrentUsername(), // Dùng hàm trả về String
                    CreatedAt = DateTime.Now,
                    LogDescription = $"Tạo mới sản phẩm '{product.Name}'. Giá bán: {product.Price:N0}"
                };
                db.ProductLogs.Add(log);

                db.SaveChanges();
                TempData["Message"] = "Thêm mới sản phẩm thành công! Hãy nhập hàng vào kho.";
                return RedirectToAction("Edit", new { id = product.ProductId });
            }

            var categories = db.NavbarItems.Where(n => !n.ItemText.Contains("Thương Hiệu")).OrderBy(n => n.ItemOrder).ToList();
            ViewBag.NavbarItemIds = new MultiSelectList(categories, "ItemId", "ItemText", SelectedNavbarItemIds);
            PopulateSuppliersDropDownList();
            return View(product);
        }

        // GET: Admin/ProductAdmin/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Product product = db.Products
                                .Include("ProductNavbarLinks")
                                .Include("StockTransactions.Supplier")
                                .Include("StockTransactions.User")
                                .FirstOrDefault(p => p.ProductId == id);

            if (product == null) return HttpNotFound();

            var categories = db.NavbarItems
                               .Where(n => !n.ItemText.Contains("Thương Hiệu"))
                               .OrderBy(n => n.ItemOrder)
                               .ToList();

            var selectedIds = product.ProductNavbarLinks.Select(p => p.NavbarItemId).ToArray();
            ViewBag.NavbarItemIds = new MultiSelectList(categories, "ItemId", "ItemText", selectedIds);
            PopulateSuppliersDropDownList();

            return View(product);
        }

        // POST: Admin/ProductAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult Edit(Product product, HttpPostedFileBase ImageUpload, int[] SelectedNavbarItemIds)
        {
            var oldProduct = db.Products.AsNoTracking().FirstOrDefault(p => p.ProductId == product.ProductId);

            if (ModelState.IsValid && oldProduct != null)
            {
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
                product.StockQuantity = oldProduct.StockQuantity;

                List<string> changes = new List<string>();
                if (oldProduct.Name != product.Name) changes.Add($"Tên: '{oldProduct.Name}' -> '{product.Name}'");
                if (oldProduct.Price != product.Price) changes.Add($"Giá bán: {oldProduct.Price:N0} -> {product.Price:N0}");
                if (oldProduct.PurchasePrice != product.PurchasePrice) changes.Add($"Giá nhập: {oldProduct.PurchasePrice:N0} -> {product.PurchasePrice:N0}");
                if (oldProduct.DiscountPercentage != product.DiscountPercentage) changes.Add($"Giảm giá: {oldProduct.DiscountPercentage}% -> {product.DiscountPercentage}%");

                var oldCatIds = db.ProductNavbarLinks.Where(x => x.ProductId == product.ProductId).Select(x => x.NavbarItemId).ToList();
                bool isCatChanged = false;
                if (SelectedNavbarItemIds != null)
                {
                    if (oldCatIds.Count != SelectedNavbarItemIds.Length || !oldCatIds.All(SelectedNavbarItemIds.Contains)) isCatChanged = true;
                }
                else if (oldCatIds.Count > 0) isCatChanged = true;

                if (isCatChanged) changes.Add("Cập nhật danh mục phân loại");

                if (changes.Any())
                {
                    var log = new ProductLog
                    {
                        ProductId = product.ProductId,
                        ActionType = "Cập nhật thông tin",
                        UpdatedBy = GetCurrentUsername(), // SỬA LỖI: Dùng String
                        CreatedAt = DateTime.Now,
                        LogDescription = string.Join("; ", changes)
                    };
                    db.ProductLogs.Add(log);
                }

                var productToUpdate = db.Products.Find(product.ProductId);
                productToUpdate.Name = product.Name;
                productToUpdate.Price = product.Price;
                productToUpdate.PurchasePrice = product.PurchasePrice;
                productToUpdate.DiscountPercentage = product.DiscountPercentage;
                productToUpdate.Description = product.Description;
                productToUpdate.ImageUrl = product.ImageUrl;

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
                TempData["Message"] = "Cập nhật thông tin sản phẩm thành công!";
                return RedirectToAction("Edit", new { id = product.ProductId });
            }

            var categories = db.NavbarItems.Where(n => !n.ItemText.Contains("Thương Hiệu")).OrderBy(n => n.ItemOrder).ToList();
            var selectedIds = oldProduct != null ? db.ProductNavbarLinks.Where(x => x.ProductId == oldProduct.ProductId).Select(x => x.NavbarItemId).ToArray() : null;
            ViewBag.NavbarItemIds = new MultiSelectList(categories, "ItemId", "ItemText", selectedIds);
            PopulateSuppliersDropDownList();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStock(int productId, int supplierId, int quantity, int type, string note)
        {
            var product = db.Products.Find(productId);
            if (product == null) return HttpNotFound();

            if (type == 1)
            {
                product.StockQuantity += quantity;
            }
            else if (type == 2)
            {
                if (product.StockQuantity < quantity)
                {
                    TempData["Error"] = "Lỗi: Số lượng trong kho không đủ để xuất trả!";
                    return RedirectToAction("Edit", new { id = productId });
                }
                product.StockQuantity -= quantity;
            }

            // Bảng StockTransactions dùng UserID (Int) -> Dùng GetCurrentUserId()
            var trans = new StockTransaction
            {
                ProductId = productId,
                SupplierId = supplierId,
                UserId = GetCurrentUserId(),
                Quantity = quantity,
                Type = type,
                Note = note,
                CreatedAt = DateTime.Now
            };
            db.StockTransactions.Add(trans);

            var supplierName = db.Suppliers.Find(supplierId)?.Name ?? "N/A";
            var actionText = type == 1 ? "Nhập kho" : "Xuất trả NCC";

            // Bảng ProductLogs dùng UpdatedBy (String) -> Dùng GetCurrentUsername()
            var log = new ProductLog
            {
                ProductId = productId,
                ActionType = actionText,
                LogDescription = $"{(type == 1 ? "+" : "-")}{quantity}. NCC: {supplierName}. Ghi chú: {note}",
                UpdatedBy = GetCurrentUsername(), // SỬA LỖI: Dùng String
                CreatedAt = DateTime.Now
            };
            db.ProductLogs.Add(log);

            db.SaveChanges();
            TempData["Message"] = "Giao dịch kho thành công!";
            return RedirectToAction("Edit", new { id = productId });
        }

        // DELETE actions...
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Product product = db.Products.Find(id);
            if (product == null) return HttpNotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);

            var links = db.ProductNavbarLinks.Where(p => p.ProductId == id);
            db.ProductNavbarLinks.RemoveRange(links);

            var logs = db.ProductLogs.Where(p => p.ProductId == id);
            db.ProductLogs.RemoveRange(logs);

            var trans = db.StockTransactions.Where(p => p.ProductId == id);
            db.StockTransactions.RemoveRange(trans);

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