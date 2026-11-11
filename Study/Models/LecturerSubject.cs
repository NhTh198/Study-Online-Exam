namespace Study.Models;

public class LecturerSubject
{
    public int Id { get; set; }

    public int LecturerId { get; set; }

    public int SubjectId { get; set; }

    // Giảng viên
    public User Lecturer { get; set; } = null!;

    // Môn học
    public Subject Subject { get; set; } = null!;
}