using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;
using PhoneStore_New;
using System.Data.Entity;
using System.Web;

// === THÊM CÁC DÒNG BỊ THIẾU VÀO ĐÂY ===
using System;
using System.IO;
// === KẾT THÚC THÊM MỚI ===

namespace PhoneStore_New.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // GET: /Profile/Index
        public ActionResult Index(string message)
        {
            var userIdString = IdentityExtensions.GetUserId(User.Identity);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Logout", "Login");
            }
            var user = db.Users.Find(userId);

            if (user == null)
            {
                return RedirectToAction("Logout", "Login");
            }

            var model = new ProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                AvatarUrl = user.AvatarUrl,
                Message = (TempData["Message"] as string) ?? message
            };

            return View(model);
        }

        // POST: /Profile/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(ProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userToUpdate = db.Users.Find(model.UserId);
                if (userToUpdate == null)
                {
                    return HttpNotFound();
                }

                string newAvatarPath = userToUpdate.AvatarUrl;

                if (model.ImageFile != null && model.ImageFile.ContentLength > 0)
                {
                    var uploadDir = Server.MapPath("~/uploads/avatars/");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    // Dòng 76 (cần 'using System.IO;' và 'using System;')
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                    var path = Path.Combine(uploadDir, fileName);

                    model.ImageFile.SaveAs(path);
                    newAvatarPath = "~/uploads/avatars/" + fileName;

                    if (!string.IsNullOrEmpty(model.AvatarUrl) && model.AvatarUrl != newAvatarPath)
                    {
                        // Dòng 94 (cần 'using System;')
                        try
                        {
                            var oldFilePath = Server.MapPath(model.AvatarUrl);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                        catch (Exception)
                        {
                            // Bỏ qua lỗi nếu không xóa được file cũ
                        }
                    }
                }

                userToUpdate.FirstName = model.FirstName;
                userToUpdate.LastName = model.LastName;
                userToUpdate.Email = model.Email;
                userToUpdate.PhoneNumber = model.PhoneNumber;
                userToUpdate.Address = model.Address;
                userToUpdate.AvatarUrl = newAvatarPath;

                db.Entry(userToUpdate).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Message"] = "Hồ sơ của bạn đã được cập nhật thành công!";
                return RedirectToAction("Index");
            }

            return View(model);
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