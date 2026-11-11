namespace Study.Models;

public class Exam
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SubjectId { get; set; }

    public string? Description { get; set; }

    public int CreatedBy { get; set; }

    public string Status { get; set; } = "Draft";

    public DateTime CreatedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public ICollection<Notifications> Notifications { get; set; }
    = new List<Notifications>();
    public DateTime? ReviewedAt { get; set; }

    public string? RejectReason { get; set; }

    // Môn học
    public Subject Subject { get; set; } = null!;

    // Giảng viên tạo đề
    public User Creator { get; set; } = null!;

    // Danh sách câu hỏi trong đề
    public ICollection<ExamQuestion> ExamQuestions { get; set; }
        = new List<ExamQuestion>();

    // Lịch thi
    public ExamSchedule? Schedule { get; set; }

    // Các lần sinh viên làm bài
    public ICollection<ExamAttempt> Attempts { get; set; }
        = new List<ExamAttempt>();
}