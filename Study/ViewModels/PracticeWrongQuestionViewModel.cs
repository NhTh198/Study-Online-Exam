namespace Study.ViewModels;

public class PracticeWrongQuestionViewModel
{
    public int QuestionOrder { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? SelectedAnswer { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
}
