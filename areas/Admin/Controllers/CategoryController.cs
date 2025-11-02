using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Areas.Admin.Models.ViewModels;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class CategoryController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // GET: Admin/Category/Index (Hiển thị danh sách, tìm kiếm, form thêm)
        public ActionResult Index(string search_query, string message)
        {
            var categories = db.Categories.AsQueryable();

            if (!string.IsNullOrEmpty(search_query))
            {
                // Tìm kiếm theo tên danh mục
                categories = categories.Where(c => c.Name.Contains(search_query));
            }

            var viewModel = new CategoryListViewModel
            {
                // Ánh xạ sang ViewModel để hiển thị
                Categories = categories.Select(c => new CategoryViewModel { CategoryId = c.CategoryId, Name = c.Name }).ToList(),
                SearchQuery = search_query,
                Message = (TempData["Message"] as string) ?? message
            };

            return View(viewModel);
        }

        // POST: Admin/Category/Add (Thêm danh mục mới - Tương đương admin_categories.php POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra trùng lặp
                if (db.Categories.Any(c => c.Name == model.Name))
                {
                    TempData["Message"] = "Lỗi: Tên danh mục đã tồn tại.";
                    return RedirectToAction("Index");
                }

                // 2. Thêm mới
                var newCategory = new Category { Name = model.Name };
                db.Categories.Add(newCategory);
                db.SaveChanges();

                TempData["Message"] = "Danh mục đã được thêm thành công.";
            }
            else
            {
                TempData["Message"] = "Lỗi: Tên danh mục không hợp lệ.";
            }
            return RedirectToAction("Index");
        }

        // GET: Admin/Category/Delete/5 (Xóa danh mục - Tương đương admin_categories.php?action=delete)
        public ActionResult Delete(int id)
        {
            var category = db.Categories.Find(id);

            if (category != null)
            {
                // Kiểm tra ràng buộc: Không cho xóa nếu có sản phẩm đang sử dụng
                if (db.Products.Any(p => p.CategoryId == id))
                {
                    TempData["Message"] = "Lỗi: Không thể xóa danh mục này vì có sản phẩm đang sử dụng.";
                }
                else
                {
                    db.Categories.Remove(category);
                    db.SaveChanges();
                    TempData["Message"] = "Danh mục đã được xóa thành công.";
                }
            }
            else
            {
                TempData["Message"] = "Lỗi: Không tìm thấy danh mục để xóa.";
            }
            return RedirectToAction("Index");
        }
    }
}