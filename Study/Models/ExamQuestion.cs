namespace Study.Models;

public class ExamQuestion
{
    public int Id { get; set; }

    public int ExamId { get; set; }

    public int QuestionId { get; set; }

    public int QuestionOrder { get; set; }

    // Đề thi
    public Exam Exam { get; set; } = null!;

    // Câu hỏi
    public Question Question { get; set; } = null!;
}