namespace Study.Models;

public class Subject
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    // Giảng viên được phân công môn
    public ICollection<LecturerSubject> LecturerSubjects { get; set; }
        = new List<LecturerSubject>();

    // Các câu hỏi thuộc môn
    public ICollection<Question> Questions { get; set; }
        = new List<Question>();

    // Các đề thi thuộc môn
    public ICollection<Exam> Exams { get; set; }
        = new List<Exam>();
}