using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;

namespace PhoneStore_New.Controllers
{
    // BỎ [Authorize] Ở ĐÂY
    public class LoginController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();
        private const string DEFAULT_BACKGROUND_URL = "~/Content/images/autoimage.jpg";

        // GET: /Login/Index
        [AllowAnonymous] // THÊM [AllowAnonymous] ĐỂ CHO PHÉP TRUY CẬP CÔNG KHAI
        public ActionResult Index(string message)
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("admin"))
                {
                    return RedirectToAction("Index", "Admin", new { Area = "Admin" });
                }
                return RedirectToAction("Index", "Home");
            }

            ViewBag.BackgroundUrl = db.Settings.FirstOrDefault(s => s.SettingKey == "background_image_url")?.SettingValue ?? DEFAULT_BACKGROUND_URL;
            ViewBag.Message = TempData["Message"] ?? message;
            return View();
        }

        // POST: /Login/Index
        [HttpPost]
        [AllowAnonymous] // THÊM [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Index(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(u => u.Username == model.Username);

                if (user != null && System.Web.Helpers.Crypto.VerifyHashedPassword(user.PasswordHash, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(user.UserId.ToString(), false);

                    Session["UserId"] = user.UserId;
                    Session["Username"] = user.Username;
                    Session["FirstName"] = user.FirstName;

                    bool isVip = false;
                    if (user.user_type == "vip" && (user.vip_expiry_date == null || user.vip_expiry_date >= DateTime.Today))
                    {
                        isVip = true;
                    }
                    Session["UserType"] = isVip ? "vip" : "regular";

                    if (user.Role == "admin")
                    {
                        return RedirectToAction("Index", "Admin", new { Area = "Admin" });
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            ViewBag.BackgroundUrl = db.Settings.FirstOrDefault(s => s.SettingKey == "background_image_url")?.SettingValue ?? DEFAULT_BACKGROUND_URL;
            return View(model);
        }

        // GET: /Login/Register
        [AllowAnonymous] // THÊM [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        // POST: /Login/Register
        [HttpPost]
        [AllowAnonymous] // THÊM [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (db.Users.Any(u => u.Username == model.Username || u.Email == model.Email))
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc email đã tồn tại.");
                    return View(model);
                }

                var newUser = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = System.Web.Helpers.Crypto.HashPassword(model.Password),
                    Role = "user",
                    user_type = "regular",
                    CreatedAt = DateTime.Now
                };

                db.Users.Add(newUser);
                db.SaveChanges();

                TempData["Message"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [Authorize] // Giữ lại Authorize cho Logout để đảm bảo chỉ người đã đăng nhập mới có thể đăng xuất
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Index", "Login");
        }
    }
}