namespace Study.ViewModels;

public class AdminExamStudentListViewModel
{
    public int ExamId { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public List<AdminExamStudentRowViewModel> Students { get; set; } = new();
}

public class AdminExamStudentRowViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
