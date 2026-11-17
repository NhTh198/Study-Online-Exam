using System.ComponentModel.DataAnnotations;

namespace Study.ViewModels;

public class LecturerQuestionViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn môn học.")]
    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Chủ đề")]
    public string? Topic { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung câu hỏi.")]
    [Display(Name = "Nội dung câu hỏi")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn độ khó.")]
    [Display(Name = "Độ khó")]
    public string Difficulty { get; set; } = "Easy";

    public List<LecturerOptionViewModel> Options { get; set; }
        = new List<LecturerOptionViewModel>();
}

public class LecturerOptionViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung đáp án.")]
    public string Content { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}