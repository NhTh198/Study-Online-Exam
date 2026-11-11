namespace Study.Models;

public class ExamAttemptQuestion
{
    public int Id { get; set; }

    public int AttemptId { get; set; }

    public int QuestionId { get; set; }

    public int QuestionOrder { get; set; }

    // Lần làm bài
    public ExamAttempt Attempt { get; set; } = null!;

    // Câu hỏi
    public Question Question { get; set; } = null!;
}