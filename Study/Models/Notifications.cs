namespace Study.Models;

public class Notifications
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Type { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public int? ExamId { get; set; }

    public User User { get; set; } = null!;

    public Exam? Exam { get; set; }
}