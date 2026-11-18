
using System.ComponentModel.DataAnnotations;

namespace Study.ViewModels;

public class PracticeStartViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn môn học.")]
    public int SubjectId { get; set; }

    public List<PracticeSubjectViewModel> Subjects { get; set; }
        = new List<PracticeSubjectViewModel>();
}

public class PracticeSubjectViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public int QuestionCount { get; set; }
}


public class PracticeTakeViewModel
{
    public int AttemptId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }

    public DateTime ForcedSubmitTime { get; set; }

    public int TotalQuestions { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public List<PracticeQuestionViewModel> Questions { get; set; }
        = new List<PracticeQuestionViewModel>();
}


public class PracticeQuestionViewModel
{
    public int QuestionId { get; set; }

    public int QuestionOrder { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? Topic { get; set; }

    public string Difficulty { get; set; } = string.Empty;

    public int? SelectedOptionId { get; set; }

    public bool IsMarked { get; set; }

    public List<PracticeOptionViewModel> Options { get; set; }
        = new List<PracticeOptionViewModel>();
}


public class PracticeOptionViewModel
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
