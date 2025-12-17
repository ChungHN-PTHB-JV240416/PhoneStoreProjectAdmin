using System.Collections.Generic;
using PhoneStore_New.Models.ViewModels; // Cần để dùng ProductCardViewModel
using PagedList; // Cần để dùng IPagedList

namespace PhoneStore_New.Areas.Admin.Models.ViewModels
{
    public class ProductCollectionEditViewModel
    {
        /// <summary>
        /// Thông tin cơ bản của Bộ sưu tập đang sửa.
        /// </summary>
        public ProductCollectionViewModel Collection { get; set; }

        /// <summary>
        /// Danh sách các sản phẩm ĐÃ CÓ trong bộ sưu tập này.
        /// </summary>
        public List<ProductCardViewModel> ProductsInCollection { get; set; }

        /// <summary>
        /// Danh sách (đã phân trang) các sản phẩm CHƯA CÓ trong bộ sưu tập
        /// (để admin tìm và thêm vào).
        /// </summary>
        public IPagedList<ProductCardViewModel> ProductsNotInCollection { get; set; }

        /// <summary>
        /// Dùng để giữ lại từ khóa tìm kiếm khi phân trang.
        /// </summary>
        public string SearchKeyword { get; set; }
    }
}