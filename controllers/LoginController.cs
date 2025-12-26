using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;
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

        // GET: /Login/Index
        [AllowAnonymous]
        public ActionResult Index(string message, string returnUrl) // <--- Thêm tham số returnUrl
        {
            // Nếu đã đăng nhập thì đá đi chỗ khác
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("admin")) return RedirectToAction("Index", "Admin", new { Area = "Admin" });
                return RedirectToAction("Index", "Home");
            }

            // Truyền returnUrl ra View để form POST biết đường quay lại
            ViewBag.ReturnUrl = returnUrl;

            ViewBag.BackgroundUrl = db.Settings.FirstOrDefault(s => s.SettingKey == "background_image_url")?.SettingValue ?? DEFAULT_BACKGROUND_URL;
            ViewBag.Message = TempData["Message"] ?? message;
            return View();
        }

        // POST: /Login/Index
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Index(LoginViewModel model, string returnUrl) // <--- Nhận lại returnUrl từ Form
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(u => u.Username == model.Username);

                // Kiểm tra User và Pass
                if (user != null && System.Web.Helpers.Crypto.VerifyHashedPassword(user.PasswordHash, model.Password))
                {
                    // 1. THIẾT LẬP AUTHENTICATION (COOKIE & SESSION)
                    FormsAuthentication.SetAuthCookie(user.UserId.ToString(), false);
                    Session["UserId"] = user.UserId;
                    Session["Username"] = user.Username;
                    Session["FirstName"] = user.FirstName;

                    bool isVip = user.user_type == "vip" && (user.vip_expiry_date == null || user.vip_expiry_date >= DateTime.Today);
                    Session["UserType"] = isVip ? "vip" : "regular";

                    // =========================================================================
                    // 2. GỘP GIỎ HÀNG: SESSION -> DB (QUAN TRỌNG ĐỂ KHÔNG MẤT HÀNG VỪA CHỌN)
                    // =========================================================================
                    try
                    {
                        var sessionCart = Session["Cart"] as List<CartItem>;
                        if (sessionCart != null && sessionCart.Any())
                        {
                            foreach (var item in sessionCart)
                            {
                                // Check xem trong DB có chưa
                                var dbItem = db.Carts.FirstOrDefault(c => c.UserId == user.UserId && c.ProductId == item.ProductId);
                                if (dbItem == null)
                                {
                                    db.Carts.Add(new Cart { UserId = user.UserId, ProductId = item.ProductId, Quantity = item.Quantity, CreatedAt = DateTime.Now });
                                }
                                else
                                {
                                    // Có rồi thì cộng thêm số lượng khách vừa chọn
                                    dbItem.Quantity += item.Quantity;
                                }
                            }
                            db.SaveChanges(); // Lưu chốt
                        }
                    }
                    catch (Exception) { /* Bỏ qua lỗi merge nếu có */ }

                    // =========================================================================
                    // 3. TẢI GIỎ HÀNG: DB -> SESSION (ĐỂ HIỂN THỊ ĐẦY ĐỦ CẢ CŨ LẪN MỚI)
                    // =========================================================================
                    try
                    {
                        var dbCartList = db.Carts.Where(c => c.UserId == user.UserId).ToList();
                        if (dbCartList.Any())
                        {
                            var newSessionCart = new List<CartItem>();
                            foreach (var dbItem in dbCartList)
                            {
                                var product = db.Products.Find(dbItem.ProductId);
                                if (product != null)
                                {
                                    decimal finalPrice = product.Price * (1m - (product.DiscountPercentage ?? 0) / 100m);
                                    if (isVip && product.vip_price.HasValue && product.vip_price < product.Price)
                                    {
                                        finalPrice = product.vip_price.Value;
                                    }

                                    newSessionCart.Add(new CartItem
                                    {
                                        ProductId = product.ProductId,
                                        Name = product.Name,
                                        ImageUrl = product.ImageUrl,
                                        Price = finalPrice,
                                        Quantity = dbItem.Quantity
                                    });
                                }
                            }
                            Session["Cart"] = newSessionCart;
                        }
                    }
                    catch (Exception) { /* Bỏ qua lỗi load */ }

                    // =========================================================================
                    // 4. CHUYỂN HƯỚNG (QUAN TRỌNG: XỬ LÝ RETURN URL)
                    // =========================================================================

                    // Nếu có ReturnUrl (ví dụ: đang thanh toán dở), quay lại đó
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    // Nếu không, đi theo quyền hạn
                    if (user.Role == "admin") return RedirectToAction("Index", "Admin", new { Area = "Admin" });
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            // Nếu đăng nhập thất bại, phải giữ lại ReturnUrl để người dùng nhập lại
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.BackgroundUrl = db.Settings.FirstOrDefault(s => s.SettingKey == "background_image_url")?.SettingValue ?? DEFAULT_BACKGROUND_URL;
            return View(model);
        }

        // --- CÁC HÀM KHÁC GIỮ NGUYÊN ---
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
                    PasswordHash = System.Web.Helpers.Crypto.HashPassword(model.Password),
                    Role = "user",
                    user_type = "regular",
                    CreatedAt = DateTime.Now,
                    AvatarUrl = "/Content/images/default-user.png"
                };

                db.Users.Add(newUser);
                db.SaveChanges();

                TempData["Message"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.";
                return RedirectToAction("Index");
            }
            return View(model);
        }
        // GET: /Login/ChangePassword
        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Login/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Lấy ID user hiện tại (Dùng session hoặc User.Identity)
            int userId = 0;
            if (Session["UserId"] != null) userId = Convert.ToInt32(Session["UserId"]);
            else if (User.Identity.IsAuthenticated && int.TryParse(User.Identity.Name, out int uid)) userId = uid;

            if (userId == 0) return RedirectToAction("Index", "Login");

            var user = db.Users.Find(userId);
            if (user == null) return RedirectToAction("Index", "Login");

            // Kiểm tra mật khẩu cũ
            if (!System.Web.Helpers.Crypto.VerifyHashedPassword(user.PasswordHash, model.OldPassword))
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu hiện tại không chính xác.");
                return View(model);
            }

            // Đổi mật khẩu mới
            user.PasswordHash = System.Web.Helpers.Crypto.HashPassword(model.NewPassword);
            db.Entry(user).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            TempData["Message"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index", "Profile"); // Hoặc về trang Profile
        }
        // GET: /Login/ForgotPassword
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
                ViewBag.Error = "Email không tồn tại trong hệ thống.";
                return View();
            }

            // Tạo mật khẩu mới (8 ký tự)
            string newPass = Guid.NewGuid().ToString().Substring(0, 8);
            user.PasswordHash = System.Web.Helpers.Crypto.HashPassword(newPass);
            db.SaveChanges();

            // Gửi Email
            try
            {
                var msg = new MailMessage();
                msg.From = new MailAddress(ConfigurationManager.AppSettings["EmailUserName"], ConfigurationManager.AppSettings["EmailFrom"]);
                msg.To.Add(user.Email);
                msg.Subject = "Cấp lại mật khẩu PhoneStore";
                msg.Body = $"Mật khẩu mới của bạn là: <b>{newPass}</b><br/>Vui lòng đổi lại ngay sau khi đăng nhập.";
                msg.IsBodyHtml = true;

                using (var client = new SmtpClient(ConfigurationManager.AppSettings["EmailHost"], int.Parse(ConfigurationManager.AppSettings["EmailPort"])))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["EmailUserName"], ConfigurationManager.AppSettings["EmailPassword"]);
                    client.Send(msg);
                }
                ViewBag.Success = "Đã gửi mật khẩu mới vào email của bạn.";
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi gửi mail: " + ex.Message;
            }
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Login");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}