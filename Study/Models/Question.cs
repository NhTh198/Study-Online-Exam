
namespace Study.Models;

public class Question
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public string? Topic { get; set; }

    public string Content { get; set; } = string.Empty;

    public string Difficulty { get; set; } = string.Empty;

    // Practice không cần giảng viên nên CreatedBy có thể NULL
    public int? CreatedBy { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public Subject Subject { get; set; } = null!;

    // Có thể NULL vì Practice question không có người tạo
    public User? Creator { get; set; }

    public ICollection<QuestionOption> Options { get; set; }
        = new List<QuestionOption>();

    public ICollection<ExamQuestion> ExamQuestions { get; set; }
        = new List<ExamQuestion>();

    public ICollection<ExamAttemptQuestion> AttemptQuestions { get; set; }
        = new List<ExamAttemptQuestion>();

    public ICollection<ExamAttemptOption> AttemptOptions { get; set; }
        = new List<ExamAttemptOption>();

    public ICollection<StudentAnswer> StudentAnswers { get; set; }
        = new List<StudentAnswer>();
}
