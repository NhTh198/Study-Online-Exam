using System.ComponentModel.DataAnnotations;

namespace Study.ViewModels;

public class AdminSubjectViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên môn học.")]
    [StringLength(100, ErrorMessage = "Tên môn học không được quá 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã môn học.")]
    [StringLength(30, ErrorMessage = "Mã môn học không được quá 30 ký tự.")]
    public string Code { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả không được quá 500 ký tự.")]
    public string? Description { get; set; }
}