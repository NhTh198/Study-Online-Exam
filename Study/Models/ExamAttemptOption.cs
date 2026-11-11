namespace Study.Models;

public class ExamAttemptOption
{
    public int Id { get; set; }

    public int AttemptId { get; set; }

    public int QuestionId { get; set; }

    public int OptionId { get; set; }

    public int DisplayOrder { get; set; }

    // Lần làm bài
    public ExamAttempt Attempt { get; set; } = null!;

    // Câu hỏi
    public Question Question { get; set; } = null!;

    // Đáp án thực tế
    public QuestionOption Option { get; set; } = null!;
}