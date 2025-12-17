using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Areas.Admin.Models.ViewModels;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels; // Cần để dùng ProductCardViewModel
using PagedList; // Cần để dùng PagedList

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class ProductCollectionAdminController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // GET: Admin/ProductCollectionAdmin
        public ActionResult Index(string message)
        {
            ViewBag.Message = (TempData["Message"] as string) ?? message;
            var collections = db.ProductCollections
                                .OrderBy(c => c.Name)
                                .Select(c => new ProductCollectionViewModel
                                {
                                    CollectionId = c.CollectionId,
                                    Name = c.Name,
                                    Handle = c.Handle,
                                    IsPublished = c.IsPublished
                                })
                                .ToList();

            return View(collections);
        }

        // GET: Admin/ProductCollectionAdmin/Create
        public ActionResult Create()
        {
            var viewModel = new ProductCollectionViewModel
            {
                IsPublished = true
            };
            return View(viewModel);
        }

        // POST: Admin/ProductCollectionAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductCollectionViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (db.ProductCollections.Any(c => c.Handle == model.Handle))
                {
                    ModelState.AddModelError("Handle", "Đường dẫn (slug) này đã tồn tại. Vui lòng chọn đường dẫn khác.");
                    return View(model);
                }

                var newCollection = new ProductCollection
                {
                    Name = model.Name,
                    Handle = model.Handle.ToLower().Replace(" ", "-"),
                    IsPublished = model.IsPublished
                };

                db.ProductCollections.Add(newCollection);
                db.SaveChanges();

                TempData["Message"] = "Đã tạo Bộ sưu tập mới thành công! Giờ bạn có thể thêm sản phẩm vào.";
                return RedirectToAction("Edit", new { id = newCollection.CollectionId });
            }
            return View(model);
        }

        // === BẮT ĐẦU SỬA ĐỔI LỚN: NÂNG CẤP ACTION EDIT ===

        // GET: Admin/ProductCollectionAdmin/Edit/5
        public ActionResult Edit(int id, string searchKeyword, int? page)
        {
            var collection = db.ProductCollections.Find(id);
            if (collection == null)
            {
                return HttpNotFound();
            }

            // 1. Lấy thông tin cơ bản của Bộ sưu tập
            var collectionVM = new ProductCollectionViewModel
            {
                CollectionId = collection.CollectionId,
                Name = collection.Name,
                Handle = collection.Handle,
                IsPublished = collection.IsPublished
            };

            // 2. Lấy danh sách sản phẩm ĐÃ CÓ trong bộ sưu tập
            var productsInCollection = db.ProductCollectionItems
                .Where(ci => ci.CollectionId == id)
                .Include(ci => ci.Product)
                .OrderBy(ci => ci.DisplayOrder)
                .Select(ci => new ProductCardViewModel
                {
                    ProductId = ci.Product.ProductId,
                    Name = ci.Product.Name,
                    ImageUrl = ci.Product.ImageUrl,
                    OriginalPrice = ci.Product.Price // Lấy giá gốc để tham khảo
                })
                .ToList();

            // 3. Lấy danh sách sản phẩm CHƯA CÓ trong bộ sưu tập (để thêm vào)

            // Lấy danh sách ID các sản phẩm đã có
            var productIdsInCollection = productsInCollection.Select(p => p.ProductId).ToList();

            // Bắt đầu truy vấn
            var productsQuery = db.Products.AsQueryable();

            // Lọc các sản phẩm chưa có
            productsQuery = productsQuery.Where(p => !productIdsInCollection.Contains(p.ProductId));

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(searchKeyword));
            }

            // Sắp xếp
            var orderedQuery = productsQuery.OrderBy(p => p.Name);

            // Chuyển sang ProductCardViewModel
            var cardQuery = orderedQuery.Select(p => new ProductCardViewModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                ImageUrl = p.ImageUrl,
                OriginalPrice = p.Price
            });

            // Phân trang
            int pageSize = 5; // Hiển thị 5 sản phẩm 1 trang
            int pageNumber = (page ?? 1);
            var pagedProducts = cardQuery.ToPagedList(pageNumber, pageSize);

            // 4. Đóng gói tất cả vào ViewModel chính
            var viewModel = new ProductCollectionEditViewModel
            {
                Collection = collectionVM,
                ProductsInCollection = productsInCollection,
                ProductsNotInCollection = pagedProducts,
                SearchKeyword = searchKeyword
            };

            return View(viewModel);
        }

        // POST: Admin/ProductCollectionAdmin/Edit/5 (Chỉ sửa Tên/Handle/Published)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProductCollectionViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (db.ProductCollections.Any(c => c.Handle == model.Handle && c.CollectionId != model.CollectionId))
                {
                    ModelState.AddModelError("Handle", "Đường dẫn (slug) này đã tồn tại.");
                    return View(model); // Sẽ gây lỗi, nhưng chúng ta sẽ sửa View sau
                }

                var collectionToUpdate = db.ProductCollections.Find(model.CollectionId);
                if (collectionToUpdate == null)
                {
                    return HttpNotFound();
                }

                collectionToUpdate.Name = model.Name;
                collectionToUpdate.Handle = model.Handle.ToLower().Replace(" ", "-");
                collectionToUpdate.IsPublished = model.IsPublished;

                db.Entry(collectionToUpdate).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Message"] = "Đã cập nhật thông tin bộ sưu tập thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // === KẾT THÚC SỬA ĐỔI ===

        // POST: Admin/ProductCollectionAdmin/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var collection = db.ProductCollections.Find(id);
            if (collection == null)
            {
                return HttpNotFound();
            }

            db.ProductCollections.Remove(collection);
            db.SaveChanges();

            TempData["Message"] = "Đã xóa bộ sưu tập thành công.";
            return RedirectToAction("Index");
        }

        // === CÁC ACTION MỚI ĐỂ THÊM/XÓA SẢN PHẨM ===

        // POST: Admin/ProductCollectionAdmin/AddProductToCollection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddProductToCollection(int collectionId, int productId)
        {
            // Kiểm tra xem đã tồn tại chưa
            var exists = db.ProductCollectionItems.Any(ci => ci.CollectionId == collectionId && ci.ProductId == productId);
            if (!exists)
            {
                var newItem = new ProductCollectionItem
                {
                    CollectionId = collectionId,
                    ProductId = productId,
                    DisplayOrder = 0 // Tạm thời để 0
                };
                db.ProductCollectionItems.Add(newItem);
                db.SaveChanges();
            }
            // Quay lại trang Edit
            return RedirectToAction("Edit", new { id = collectionId });
        }

        // POST: Admin/ProductCollectionAdmin/RemoveProductFromCollection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveProductFromCollection(int collectionId, int productId)
        {
            var item = db.ProductCollectionItems.Find(collectionId, productId);
            if (item != null)
            {
                db.ProductCollectionItems.Remove(item);
                db.SaveChanges();
            }
            // Quay lại trang Edit
            return RedirectToAction("Edit", new { id = collectionId });
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