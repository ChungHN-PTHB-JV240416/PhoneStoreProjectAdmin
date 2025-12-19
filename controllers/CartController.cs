using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models;
using PhoneStore_New.Models.ViewModels;

namespace PhoneStore_New.Controllers
{
    public class CartController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        // Helper lấy User ID
        private int? GetCurrentUserId()
        {
            if (Session["UserId"] != null && int.TryParse(Session["UserId"].ToString(), out int uid)) return uid;
            return null;
        }

        // Helper lấy Giỏ hàng từ Session
        private List<CartItem> GetSessionCart()
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null) { cart = new List<CartItem>(); Session["Cart"] = cart; }
            return cart;
        }

        // Helper đồng bộ DB
        private void SyncToDb(int productId, int quantity)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            var dbItem = db.Carts.FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);
            if (quantity <= 0)
            {
                if (dbItem != null) db.Carts.Remove(dbItem);
            }
            else
            {
                if (dbItem != null) dbItem.Quantity = quantity;
                else db.Carts.Add(new Cart { UserId = userId.Value, ProductId = productId, Quantity = quantity, CreatedAt = DateTime.Now });
            }
            db.SaveChanges();
        }

        // === ĐÂY LÀ HÀM BẠN ĐANG THIẾU ===
        // GET: /Cart/Index
        public ActionResult Index()
        {
            var cart = GetSessionCart();
            return View(cart); // Nó sẽ tìm file ở /Views/Cart/Index.cshtml
        }
        // =================================

        [HttpPost]
        public ActionResult AddToCart(int productId, int quantity = 1)
        {
            var product = db.Products.Find(productId);
            if (product == null) return Redirect(Request.UrlReferrer?.ToString() ?? "/");

            // Tính giá
            bool isVip = Session["UserType"] != null && "vip".Equals(Session["UserType"].ToString(), StringComparison.OrdinalIgnoreCase);
            decimal finalPrice = product.Price * (1m - (product.DiscountPercentage ?? 0) / 100m);
            if (isVip && product.vip_price.HasValue && product.vip_price < product.Price) finalPrice = product.vip_price.Value;

            var cart = GetSessionCart();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);
            int newQty = quantity;

            if (item != null)
            {
                item.Quantity += quantity;
                item.Price = finalPrice; // Cập nhật giá mới nhất
                newQty = item.Quantity;
            }
            else
            {
                cart.Add(new CartItem { ProductId = productId, Name = product.Name, Price = finalPrice, ImageUrl = product.ImageUrl, Quantity = quantity });
            }
            Session["Cart"] = cart;
            SyncToDb(productId, newQty);

            TempData["Message"] = "Đã thêm vào giỏ hàng!";
            // Nếu thêm từ trang chủ thì ở lại trang chủ, nếu không thì về giỏ hàng
            if (Request.UrlReferrer != null && !Request.UrlReferrer.ToString().Contains("Cart"))
                return Redirect(Request.UrlReferrer.ToString());

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = GetSessionCart();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                if (quantity > 0) item.Quantity = quantity; else cart.Remove(item);
                Session["Cart"] = cart;
                SyncToDb(productId, quantity);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveItem(int productId)
        {
            var cart = GetSessionCart();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                cart.Remove(item);
                Session["Cart"] = cart;
                SyncToDb(productId, 0);
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}