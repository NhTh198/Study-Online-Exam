namespace Study.Models;

public class StudentAnswer
{
    public int Id { get; set; }

    public int AttemptId { get; set; }

    public int QuestionId { get; set; }

    public int? SelectedOptionId { get; set; }

    public bool IsMarked { get; set; } = false;

    public DateTime? AnsweredAt { get; set; }

    // Lần làm bài
    public ExamAttempt Attempt { get; set; } = null!;

    // Câu hỏi
    public Question Question { get; set; } = null!;

    // Đáp án sinh viên chọn
    public QuestionOption? SelectedOption { get; set; }
}