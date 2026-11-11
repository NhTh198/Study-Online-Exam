namespace Study.Models;

public class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public ICollection<Notifications> Notifications { get; set; }
    = new List<Notifications>();
    public string Password { get; set; } = string.Empty;

    public string? StudentCode { get; set; }

    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    // Quan hệ với Role
    public Role Role { get; set; } = null!;

    // Giảng viên được phân công môn học
    public ICollection<LecturerSubject> LecturerSubjects { get; set; }
        = new List<LecturerSubject>();

    // Những câu hỏi do giảng viên tạo
    public ICollection<Question> CreatedQuestions { get; set; }
        = new List<Question>();

    // Những đề thi do giảng viên tạo
    public ICollection<Exam> CreatedExams { get; set; }
        = new List<Exam>();

    // Những lịch thi do Admin tạo
    public ICollection<ExamSchedule> CreatedSchedules { get; set; }
        = new List<ExamSchedule>();

    // Những lần sinh viên làm bài
    public ICollection<ExamAttempt> ExamAttempts { get; set; }
        = new List<ExamAttempt>();
}