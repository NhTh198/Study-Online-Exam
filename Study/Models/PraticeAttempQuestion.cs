
namespace Study.Models;

public class PracticeAttemptQuestion
{
    public int Id { get; set; }

    public int AttemptId { get; set; }

    public int QuestionId { get; set; }

    public int QuestionOrder { get; set; }

    public string? OptionOrder { get; set; }


    // Navigation
    public PracticeAttempt Attempt { get; set; } = null!;

    public Question Question { get; set; } = null!;
}
