using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using PhoneStore_New.Models; // Đảm bảo đúng namespace
using PhoneStore_New.Models.ViewModels; // Đảm bảo đúng namespace
using System; // Cần thêm để dùng StringComparison

namespace PhoneStore_New.Controllers // Đảm bảo đúng namespace
{
    [Authorize]
    public class CartController : Controller
    {
        // === SỬA ĐỔI: Sử dụng DbContext được "tiêm" vào (nếu bạn đã làm) ===
        // Hoặc tạo mới nếu bạn chưa áp dụng Dependency Injection
        private readonly PhoneStoreDBEntities db;

        public CartController()
        {
            db = new PhoneStoreDBEntities(); // Tạo instance mới
        }
        // Nếu bạn đã làm theo bước DI của tôi, hãy xóa constructor trên và dùng code này:
        // private readonly PhoneStoreDBEntities _db;
        // public CartController(PhoneStoreDBEntities dbContext)
        // {
        //     _db = dbContext;
        // }
        // (Trong file này, tôi sẽ giả định bạn chưa dùng DI để đảm bảo nó chạy được)


        // Hàm tiện ích để lấy và lưu giỏ hàng
        private List<CartItem> GetCart()
        {
            return Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            Session.SetObject("Cart", cart);
        }

        // GET: /Cart/Index (Hiển thị giỏ hàng)
        public ActionResult Index()
        {
            return View(GetCart());
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int productId, int quantity)
        {
            var cart = GetCart();
            var product = db.Products.Find(productId);

            if (product == null || quantity <= 0)
            {
                TempData["Message"] = "Sản phẩm không hợp lệ.";
                return RedirectToAction("Index", "Home");
            }

            // === SỬA ĐỔI QUAN TRỌNG: LOGIC TÍNH GIÁ VIP KHI THÊM VÀO GIỎ ===
            bool isVip = "vip".Equals(Session["UserType"] as string, StringComparison.OrdinalIgnoreCase);
            decimal finalPrice;

            if (isVip && product.vip_price.HasValue)
            {
                finalPrice = product.vip_price.Value; // Lấy giá VIP
            }
            else
            {
                // Tính giá sale thường
                finalPrice = product.Price * (1m - (product.DiscountPercentage ?? 0) / 100m);
            }
            // === KẾT THÚC SỬA ĐỔI ===

            var existingItem = cart.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem != null)
            {
                // Nếu sản phẩm đã có, chỉ cập nhật số lượng
                existingItem.Quantity += quantity;
                // Cập nhật lại giá (phòng trường hợp giá thay đổi từ lần thêm trước)
                existingItem.Price = finalPrice;
            }
            else
            {
                // Nếu sản phẩm chưa có, tạo mới và dùng finalPrice
                var newItem = new CartItem
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    Price = finalPrice, // Sử dụng giá đã tính toán
                    ImageUrl = product.ImageUrl,
                    Quantity = quantity
                };
                cart.Add(newItem);
            }

            SaveCart(cart);
            return RedirectToAction("Index"); // Chuyển hướng đến trang Giỏ hàng
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                if (quantity > 0)
                {
                    item.Quantity = quantity;
                }
                else
                {
                    // Xóa nếu số lượng là 0 hoặc âm
                    cart.Remove(item);
                }
            }
            // Lưu ý: Chúng ta không tính lại giá ở đây, vì giá đã được "chốt" lúc thêm vào giỏ.
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // POST: /Cart/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveItem(int productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
            }

            SaveCart(cart);
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