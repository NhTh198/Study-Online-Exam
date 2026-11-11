namespace Study.Models;

public class ExamAttempt
{
    public int Id { get; set; }

    public int ExamId { get; set; }

    public int StudentId { get; set; }

    public DateTime? ActualStartTime { get; set; }

    public DateTime? ForcedSubmitTime { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string Status { get; set; } = "InProgress";

    // Đề thi
    public Exam Exam { get; set; } = null!;

    // Sinh viên
    public User Student { get; set; } = null!;

    // Danh sách câu hỏi đã random
    public ICollection<ExamAttemptQuestion> ExamAttemptQuestions { get; set; }
        = new List<ExamAttemptQuestion>();

    // Danh sách đáp án đã random
    public ICollection<ExamAttemptOption> ExamAttemptOptions { get; set; }
        = new List<ExamAttemptOption>();

    // Câu trả lời của sinh viên
    public ICollection<StudentAnswer> StudentAnswers { get; set; }
        = new List<StudentAnswer>();

    // Kết quả
    public ExamResult? Result { get; set; }
}