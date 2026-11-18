using System.ComponentModel.DataAnnotations;

namespace Study.ViewModels;

public class AdminScheduleExamViewModel
{
    public int ExamId { get; set; }

    public string ExamName { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu.")]
    [Display(Name = "Thời gian bắt đầu")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc.")]
    [Display(Name = "Thời gian kết thúc")]
    public DateTime EndTime { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập thời lượng.")]
    [Range(1, 600, ErrorMessage = "Thời lượng phải từ 1 đến 600 phút.")]
    [Display(Name = "Thời lượng (phút)")]
    public int DurationMinutes { get; set; }

    [Display(Name = "Grace Period (phút)")]
    public int GraceMinutes { get; set; } = 15;

    [Display(Name = "Exam Key")]
    public string? ExamKey { get; set; }
}