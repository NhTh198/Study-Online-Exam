using System.ComponentModel.DataAnnotations;

namespace Study.ViewModels;

public class LecturerCreateExamViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên kỳ thi.")]
    [StringLength(
        200,
        ErrorMessage = "Tên kỳ thi không được quá 200 ký tự.")]
    [Display(Name = "Tên kỳ thi")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn môn học.")]
    [Display(Name = "Môn học")]
    public int SubjectId { get; set; }

    [StringLength(
        1000,
        ErrorMessage = "Mô tả không được quá 1000 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    // Các câu hỏi được chọn
    public List<int> SelectedQuestionIds { get; set; }
        = new List<int>();
}