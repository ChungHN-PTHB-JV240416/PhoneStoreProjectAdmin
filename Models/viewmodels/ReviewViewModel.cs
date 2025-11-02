// Trong Models/ViewModels/ReviewViewModel.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace PhoneStore_New.Models.ViewModels
{
    public class ReviewViewModel
    {
        // Input từ form
        public int ProductId { get; set; }
        public string CommentText { get; set; }
        public int Rating { get; set; }

        // Output hiển thị
        public string FirstName { get; set; } // Tên người dùng bình luận
        public DateTime CreatedAt { get; set; }
    }
}