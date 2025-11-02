using System.Collections.Generic;
using System.Security.Principal;
using System.Web;
using System.Web.Script.Serialization; // Cần thiết cho SetObject/GetObject

namespace PhoneStore_New // Giữ nguyên namespace gốc của project
{
    // === 1. DICTIONARY EXTENSIONS ===
    public static class DictionaryExtensions
    {
        // Hàm mở rộng để truy xuất giá trị từ Dictionary một cách an toàn (dùng cho Settings)
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
    }

    // === 2. IDENTITY EXTENSIONS ===
    public static class IdentityExtensions
    {
        // Hàm mở rộng để lấy User ID từ FormsAuthentication Ticket
        public static string GetUserId(this IIdentity identity)
        {
            // Trả về Name, mà đã được lưu là User.UserId khi đăng nhập
            return HttpContext.Current?.User?.Identity?.Name;
        }
    }

    // === 3. SESSION EXTENSIONS (BẮT BUỘC CHO GIỎ HÀNG) ===
    public static class SessionExtensions
    {
        private static JavaScriptSerializer serializer = new JavaScriptSerializer();

        // Hàm mở rộng để lưu trữ một đối tượng phức tạp (như List<CartItem>) vào Session
        public static void SetObject<T>(this HttpSessionStateBase session, string key, T value)
        {
            session[key] = serializer.Serialize(value);
        }

        // Hàm mở rộng để lấy một đối tượng phức tạp từ Session
        public static T GetObject<T>(this HttpSessionStateBase session, string key) where T : new()
        {
            var value = session[key] as string;
            if (string.IsNullOrEmpty(value))
                return new T(); // Trả về đối tượng mới (List rỗng) nếu Session chưa có
            return serializer.Deserialize<T>(value);
        }
    }
}