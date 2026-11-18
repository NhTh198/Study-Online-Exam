using System.ComponentModel.DataAnnotations;

namespace Study.ViewModels;

public class AdminAssignSubjectViewModel
{
    [Required]
    public int LecturerId { get; set; }

    [Required]
    public int SubjectId { get; set; }
}