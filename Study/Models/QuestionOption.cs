namespace Study.Models;

public class QuestionOption
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public string Content { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    // Câu hỏi chứa đáp án này
    public Question Question { get; set; } = null!;

    // Đáp án được random trong bài thi
    public ICollection<ExamAttemptOption> AttemptOptions { get; set; }
        = new List<ExamAttemptOption>();

    // Sinh viên chọn đáp án này
    public ICollection<StudentAnswer> StudentAnswers { get; set; }
        = new List<StudentAnswer>();
}