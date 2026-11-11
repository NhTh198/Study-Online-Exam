
namespace Study.Models;

public class PracticeAttempt
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int SubjectId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime ForcedSubmitTime { get; set; }

    public string Status { get; set; } = "InProgress";

    // Student thực hiện bài Practice
    public User Student { get; set; } = null!;

    // Môn học của bài Practice
    public Subject Subject { get; set; } = null!;

    public ICollection<PracticeAttemptQuestion> PracticeAttemptQuestions { get; set; }
        = new List<PracticeAttemptQuestion>();

    public ICollection<PracticeAnswer> PracticeAnswers { get; set; }
        = new List<PracticeAnswer>();

    public PracticeResult? Result { get; set; }
}
