using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using PhoneStore_New.Areas.Admin.Models.ViewModels;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class SupplierController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // GET: Hiển thị danh sách nhà cung cấp
        public ActionResult Index(string search_query, string message)
        {
            var suppliersQuery = db.Suppliers.AsQueryable();

            if (!string.IsNullOrEmpty(search_query))
            {
                suppliersQuery = suppliersQuery.Where(s => s.Name.Contains(search_query) || s.Phone.Contains(search_query) || s.Address.Contains(search_query));
            }

            var suppliers = suppliersQuery
                .OrderByDescending(s => s.SupplierId)
                .Select(s => new SupplierViewModel
                {
                    SupplierId = s.SupplierId,
                    Name = s.Name,
                    Phone = s.Phone,
                    Address = s.Address,
                    ContactPerson = s.ContactPerson,
                    Email = s.Email
                }).ToList();

            var viewModel = new SupplierListViewModel
            {
                Suppliers = suppliers,
                SearchQuery = search_query,
                Message = (TempData["Message"] as string) ?? message
            };

            return View(viewModel);
        }

        // POST: Xử lý Thêm Mới hoặc Cập Nhật nhà cung cấp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Manage(SupplierViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.SupplierId == 0) // Thêm mới
                {
                    var newSupplier = new Supplier
                    {
                        Name = model.Name,
                        ContactPerson = model.ContactPerson,
                        Phone = model.Phone,
                        Email = model.Email,
                        Address = model.Address,
                        CreatedAt = DateTime.Now
                    };
                    db.Suppliers.Add(newSupplier);
                    TempData["Message"] = "Nhà cung cấp đã được thêm thành công.";
                }
                else // Cập nhật
                {
                    var supplierToUpdate = db.Suppliers.Find(model.SupplierId);
                    if (supplierToUpdate != null)
                    {
                        supplierToUpdate.Name = model.Name;
                        supplierToUpdate.ContactPerson = model.ContactPerson;
                        supplierToUpdate.Phone = model.Phone;
                        supplierToUpdate.Email = model.Email;
                        supplierToUpdate.Address = model.Address;
                        db.Entry(supplierToUpdate).State = EntityState.Modified;
                        TempData["Message"] = "Thông tin nhà cung cấp đã được cập nhật thành công.";
                    }
                }
                db.SaveChanges();
            }
            else
            {
                TempData["Message"] = "Lỗi: Vui lòng kiểm tra lại thông tin nhập liệu.";
            }

            return RedirectToAction("Index");
        }

        // POST: Xóa nhà cung cấp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var supplier = db.Suppliers.Find(id);
            if (supplier != null)
            {
                db.Suppliers.Remove(supplier);
                db.SaveChanges();
                TempData["Message"] = "Nhà cung cấp đã được xóa thành công.";
            }
            else
            {
                TempData["Message"] = "Lỗi: Không tìm thấy nhà cung cấp để xóa.";
            }
            return RedirectToAction("Index");
        }
    }
}