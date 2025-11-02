using PhoneStore_New;
using PhoneStore_New.Models;
using System;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace PhoneStore_New
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        // SỬA LẠI TÊN SỰ KIỆN VÀ THAM SỐ Ở ĐÂY
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];

            if (authCookie != null)
            {
                try
                {
                    var authTicket = FormsAuthentication.Decrypt(authCookie.Value);
                    if (authTicket != null && !authTicket.Expired)
                    {
                        string userIdStr = authTicket.Name;
                        if (!string.IsNullOrEmpty(userIdStr))
                        {
                            // Dùng using để đảm bảo DbContext được giải phóng
                            using (var db = new PhoneStoreDBEntities())
                            {
                                int userId = int.Parse(userIdStr);
                                var userFromDb = db.Users.Find(userId);
                                if (userFromDb != null)
                                {
                                    string[] roles = { userFromDb.Role };
                                    var newUser = new GenericPrincipal(new GenericIdentity(userIdStr), roles);
                                    HttpContext.Current.User = newUser;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Lỗi cookie, bỏ qua, không cần làm gì
                }
            }
        }
    }
}