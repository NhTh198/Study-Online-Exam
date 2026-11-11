namespace Study.Models;

public class ExamSchedule
{
    public int Id { get; set; }

    public int ExamId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int DurationMinutes { get; set; }

    public int GraceMinutes { get; set; } = 15;

    public string ExamKey { get; set; } = string.Empty;

    public DateTime KeyVisibleAt { get; set; }

    public DateTime? KeyExpiredAt { get; set; }

    public int CreatedByAdmin { get; set; }

    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    // Đề thi
    public Exam Exam { get; set; } = null!;

    // Admin tạo lịch
    public User Admin { get; set; } = null!;
}