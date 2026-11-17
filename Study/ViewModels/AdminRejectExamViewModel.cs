using System.ComponentModel.DataAnnotations;

namespace Study.ViewModels;

public class AdminRejectExamViewModel
{
    public int ExamId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do từ chối.")]
    [StringLength(
        500,
        ErrorMessage = "Lý do không được quá 500 ký tự.")]
    public string RejectReason { get; set; } = string.Empty;
}