using System.Web.Mvc;
using System.Web.Routing;

namespace PhoneStore_New
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                // THÊM DÒNG NÀY VÀO
                namespaces: new[] { "PhoneStore_New.Controllers" }
            );
        }
    }
}