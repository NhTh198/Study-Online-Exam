using System.ComponentModel.DataAnnotations;

namespace Study.ViewModels;

public class AdminEditUserViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    public string? StudentCode { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    public int RoleId { get; set; }
}