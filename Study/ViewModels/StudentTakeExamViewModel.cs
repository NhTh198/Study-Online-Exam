
namespace Study.ViewModels;

public class StudentTakeExamViewModel
{
    public int AttemptId { get; set; }

    public int ExamId { get; set; }

    public string ExamName { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public DateTime ?ActualStartTime { get; set; }

    public DateTime ? ForcedSubmitTime { get; set; }

    public int TotalQuestions { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public List<StudentTakeQuestionViewModel> Questions { get; set; }
        = new List<StudentTakeQuestionViewModel>();
}

public class StudentTakeQuestionViewModel
{
    public int QuestionId { get; set; }

    public int QuestionOrder { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? Topic { get; set; }

    public string Difficulty { get; set; } = string.Empty;

    public int? SelectedOptionId { get; set; }

    public bool IsMarked { get; set; }

    public List<StudentTakeOptionViewModel> Options { get; set; }
        = new List<StudentTakeOptionViewModel>();
}

public class StudentTakeOptionViewModel
{
    public int OptionId { get; set; }

    public string Content { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public string Letter
    {
        get
        {
            return DisplayOrder switch
            {
                1 => "A",
                2 => "B",
                3 => "C",
                4 => "D",
                _ => DisplayOrder.ToString()
            };
        }
    }
}
