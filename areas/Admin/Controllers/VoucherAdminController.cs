using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Areas.Admin.Models.ViewModels;
using PhoneStore_New.Models;

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class VoucherAdminController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        public ActionResult Index(string message)
        {
            ViewBag.Message = (TempData["Message"] as string) ?? message;
            var vouchers = db.Vouchers
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new VoucherViewModel
                {
                    VoucherId = v.VoucherId,
                    Code = v.Code,
                    DiscountType = v.DiscountType,
                    DiscountValue = v.DiscountValue,
                    ExpiryDate = v.ExpiryDate,
                    UsageLimit = v.UsageLimit,
                    UsageCount = v.UsageCount,
                    IsActive = v.IsActive,
                    VipOnly = v.VipOnly
                })
                .ToList();

            return View(vouchers);
        }

        public ActionResult Create()
        {
            var viewModel = new VoucherViewModel
            {
                IsActive = true,
                UsageLimit = 1,
                DiscountType = "fixed_amount" // Sửa lại giá trị mặc định cho đúng
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(VoucherViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (db.Vouchers.Any(v => v.Code.Equals(model.Code, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("Code", "Mã code này đã tồn tại. Vui lòng chọn mã khác.");
                    return View(model);
                }

                var newVoucher = new Voucher
                {
                    Code = model.Code.ToUpper(),

                    // === SỬA LỖI QUAN TRỌNG: Gửi giá trị gốc, không ToUpper() ===
                    DiscountType = model.DiscountType,

                    DiscountValue = model.DiscountValue,
                    ExpiryDate = model.ExpiryDate,
                    UsageLimit = model.UsageLimit,
                    UsageCount = 0,
                    IsActive = model.IsActive,
                    VipOnly = model.VipOnly,
                    CreatedAt = DateTime.Now
                };

                db.Vouchers.Add(newVoucher);
                db.SaveChanges(); // Lỗi đã được khắc phục

                TempData["Message"] = "Đã tạo voucher mới thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var voucher = db.Vouchers.Find(id);
            if (voucher == null)
            {
                return HttpNotFound();
            }

            var viewModel = new VoucherViewModel
            {
                VoucherId = voucher.VoucherId,
                Code = voucher.Code,
                DiscountType = voucher.DiscountType, // Giữ nguyên giá trị từ CSDL (fixed_amount hoặc percentage)
                DiscountValue = voucher.DiscountValue,
                ExpiryDate = voucher.ExpiryDate,
                UsageLimit = voucher.UsageLimit,
                UsageCount = voucher.UsageCount,
                IsActive = voucher.IsActive,
                VipOnly = voucher.VipOnly
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(VoucherViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (db.Vouchers.Any(v => v.Code.Equals(model.Code, StringComparison.OrdinalIgnoreCase) && v.VoucherId != model.VoucherId))
                {
                    ModelState.AddModelError("Code", "Mã code này đã tồn tại. Vui lòng chọn mã khác.");
                    return View(model);
                }

                var voucherToUpdate = db.Vouchers.Find(model.VoucherId);
                if (voucherToUpdate == null)
                {
                    return HttpNotFound();
                }

                voucherToUpdate.Code = model.Code.ToUpper();

                // === SỬA LỖI QUAN TRỌNG: Gửi giá trị gốc, không ToUpper() ===
                voucherToUpdate.DiscountType = model.DiscountType;

                voucherToUpdate.DiscountValue = model.DiscountValue;
                voucherToUpdate.ExpiryDate = model.ExpiryDate;
                voucherToUpdate.UsageLimit = model.UsageLimit;
                voucherToUpdate.IsActive = model.IsActive;
                voucherToUpdate.VipOnly = model.VipOnly;

                db.Entry(voucherToUpdate).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Message"] = "Đã cập nhật voucher thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var voucher = db.Vouchers.Find(id);
            if (voucher == null)
            {
                return HttpNotFound();
            }

            if (voucher.UsageCount > 0)
            {
                TempData["Message"] = "Lỗi: Không thể xóa voucher đã có người sử dụng. Bạn chỉ có thể tắt (IsActive = false).";
                return RedirectToAction("Index");
            }

            db.Vouchers.Remove(voucher);
            db.SaveChanges();

            TempData["Message"] = "Đã xóa voucher thành công.";
            return RedirectToAction("Index");
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