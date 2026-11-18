
using System.ComponentModel.DataAnnotations;

namespace Study.ViewModels;

public class PracticeHistoryViewModel
{
    public int AttemptId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int TotalQuestions { get; set; }

    public int CorrectAnswers { get; set; }

    public int WrongAnswers { get; set; }

    public int Unanswered { get; set; }

    public decimal Score { get; set; }
}
