namespace Study.ViewModels;

public class AdminReviewExamViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public string SubjectCode { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string LecturerName { get; set; } = string.Empty;

    public int QuestionCount { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string? RejectReason { get; set; }
}