using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels; // Mở cái này nếu dùng ViewModel
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace PhoneStore_New.Controllers
{
    public class LoginController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();
        private const string DEFAULT_BACKGROUND_URL = "~/Content/images/autoimage.jpg";

        // ==========================================
        // 1. ĐĂNG NHẬP (LOGIN)
        // ==========================================

        // GET: /Login/Index
        [AllowAnonymous]
        public ActionResult Index(string message, string returnUrl)
        {
            // --- FIX LỖI: BẤM ĐĂNG NHẬP BỊ LOAD LẠI TRANG ---
            // Nếu hệ thống thấy user đã có Cookie cũ (IsAuthenticated = true)
            // Thay vì đá về Home (gây vòng lặp), ta xóa sạch Cookie đi để bắt đăng nhập lại.
            if (User.Identity.IsAuthenticated)
            {
                FormsAuthentication.SignOut();
                Session.Clear();
                Session.Abandon();
            }
            // ------------------------------------------------

            ViewBag.ReturnUrl = returnUrl;
            // Lấy ảnh nền từ DB (nếu có bảng Settings)
            var bgSetting = db.Settings.FirstOrDefault(s => s.SettingKey == "background_image_url");
            ViewBag.BackgroundUrl = bgSetting != null ? bgSetting.SettingValue : DEFAULT_BACKGROUND_URL;

            ViewBag.Message = TempData["Message"] ?? message;
            return View();
        }

        // POST: /Login/Index
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Index(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(u => u.Username == model.Username);

                // Kiểm tra mật khẩu (Hỗ trợ cả Hash và Không Hash)
                bool isPasswordCorrect = false;
                if (user != null)
                {
                    // Case 1: Pass thường
                    if (user.PasswordHash == model.Password) isPasswordCorrect = true;
                    // Case 2: Pass Hash
                    else
                    {
                        try
                        {
                            if (System.Web.Helpers.Crypto.VerifyHashedPassword(user.PasswordHash, model.Password)) isPasswordCorrect = true;
                        }
                        catch { }
                    }
                }

                if (isPasswordCorrect)
                {
                    // 1. Thiết lập Session & Cookie
                    FormsAuthentication.SetAuthCookie(user.UserId.ToString(), false);
                    Session["UserId"] = user.UserId;
                    Session["Username"] = user.Username;
                    Session["FirstName"] = user.FirstName;
                    Session["UserType"] = user.user_type; // user.Role nếu dùng role

                    // 2. Logic Gộp Giỏ Hàng (Session -> DB)
                    try
                    {
                        var sessionCart = Session["Cart"] as List<ProductCardViewModel>; // Hoặc CartItem tùy model của mày
                        if (sessionCart != null && sessionCart.Any())
                        {
                            foreach (var item in sessionCart)
                            {
                                var dbItem = db.Carts.FirstOrDefault(c => c.UserId == user.UserId && c.ProductId == item.ProductId);
                                if (dbItem == null)
                                {
                                    db.Carts.Add(new Cart { UserId = user.UserId, ProductId = item.ProductId, Quantity = item.StockQuantity, CreatedAt = DateTime.Now });
                                }
                                else
                                {
                                    dbItem.Quantity += item.StockQuantity;
                                }
                            }
                            db.SaveChanges();
                        }
                    }
                    catch { /* Bỏ qua lỗi giỏ hàng để đăng nhập được */ }

                    // 3. Chuyển hướng
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    if (user.user_type == "admin") return RedirectToAction("Index", "Admin", new { Area = "Admin" });
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        // ==========================================
        // 2. ĐĂNG KÝ (REGISTER)
        // ==========================================
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
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
                    FirstName = "New", // Giá trị mặc định
                    LastName = "User",
                    // Hash mật khẩu
                    PasswordHash = System.Web.Helpers.Crypto.HashPassword(model.Password),
                    Role = "user",
                    user_type = "regular",
                    CreatedAt = DateTime.Now,
                    AvatarUrl = "~/Content/images/default-user.png"
                };

                db.Users.Add(newUser);
                db.SaveChanges();

                TempData["Message"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // ==========================================
        // 3. ĐĂNG XUẤT (LOGOUT)
        // ==========================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            // Xóa cookie thủ công cho chắc
            if (Response.Cookies["ASP.NET_SessionId"] != null)
            {
                Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddYears(-1);
            }

            return RedirectToAction("Index", "Login");
        }

        // ==========================================
        // 4. QUÊN MẬT KHẨU
        // ==========================================
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string email)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Email không tồn tại.";
                return View();
            }

            string newPass = Guid.NewGuid().ToString().Substring(0, 8);
            user.PasswordHash = System.Web.Helpers.Crypto.HashPassword(newPass);
            db.SaveChanges();

            // Gửi email (Copy logic gửi mail của mày vào đây nếu cần)
            ViewBag.Success = "Mật khẩu mới: " + newPass; // Hiển thị tạm để test

            return View();
        }

        // ==========================================
        // 5. ĐỔI MẬT KHẨU
        // ==========================================
        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            if (!System.Web.Helpers.Crypto.VerifyHashedPassword(user.PasswordHash, model.OldPassword))
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu cũ không đúng.");
                return View(model);
            }

            user.PasswordHash = System.Web.Helpers.Crypto.HashPassword(model.NewPassword);
            db.SaveChanges();

            TempData["Message"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index", "Profile"); // Hoặc về Login
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}