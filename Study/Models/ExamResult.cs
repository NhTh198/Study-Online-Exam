namespace Study.Models;

public class ExamResult
{
    public int Id { get; set; }

    public int AttemptId { get; set; }

    public int TotalQuestions { get; set; }

    public int CorrectAnswers { get; set; }

    public int WrongAnswers { get; set; }

    public int Unanswered { get; set; }

    public decimal Score { get; set; }

    public DateTime GradedAt { get; set; }

    // Lần làm bài
    public ExamAttempt Attempt { get; set; } = null!;
}