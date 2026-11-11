
namespace Study.Models;

public class PracticeAnswer
{
    public int Id { get; set; }

    public int AttemptId { get; set; }

    public int QuestionId { get; set; }

    public int? SelectedOptionId { get; set; }

    public bool IsMarked { get; set; } = false;

    public DateTime? AnsweredAt { get; set; }


    // Navigation
    public PracticeAttempt Attempt { get; set; } = null!;

    public Question Question { get; set; } = null!;

    public QuestionOption? SelectedOption { get; set; }
}
