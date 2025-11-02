using System.ComponentModel.DataAnnotations;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập.")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; }
}               