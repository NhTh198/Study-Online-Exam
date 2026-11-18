using System.ComponentModel.DataAnnotations;

namespace Study.ViewModels;

public class StudentExamKeyViewModel
{
    public int ExamId { get; set; }

public string ExamName { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int DurationMinutes { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Exam Key.")]
    [Display(Name = "Exam Key")]
    public string ExamKey { get; set; } = string.Empty;

}
