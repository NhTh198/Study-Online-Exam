using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Study.Models;
using Study.ViewModels;

namespace Study.Controllers;

/// <summary>
/// Chỉ chứa các chức năng Admin thuộc Issue #5 và #6.
/// Không chứa quản lý tài khoản, quản lý Lecturer, duyệt/xếp lịch đề thi.
/// </summary>
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Trang điều hướng cho hai nhóm chức năng Admin trong phạm vi bản này.
    [HttpGet]
    public IActionResult Index()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");
        return View();
    }

    // =====================================================
    // ISSUE #5 - QUẢN LÝ MÔN HỌC
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Subject()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        var subjects = await _context.Subjects
            .OrderBy(s => s.Code)
            .ToListAsync();

        return View(subjects);
    }

    [HttpGet]
    public IActionResult CreateSubject()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubject(AdminSubjectViewModel model)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid) return View(model);

        var code = model.Code.Trim();
        var name = model.Name.Trim();

        if (await _context.Subjects.AnyAsync(s => s.Code == code))
            ModelState.AddModelError(nameof(model.Code), "Mã môn học này đã tồn tại.");

        if (await _context.Subjects.AnyAsync(s => s.Name == name))
            ModelState.AddModelError(nameof(model.Name), "Tên môn học này đã tồn tại.");

        if (!ModelState.IsValid) return View(model);

        var subject = new Subject
        {
            Name = name,
            Code = code,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            IsActive = true
        };

        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã thêm môn học {subject.Name} thành công.";
        return RedirectToAction(nameof(Subject));
    }

    [HttpGet]
    public async Task<IActionResult> EditSubject(int id)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == id);
        if (subject == null) return NotFound();

        return View(new AdminSubjectViewModel
        {
            Id = subject.Id,
            Name = subject.Name,
            Code = subject.Code,
            Description = subject.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSubject(AdminSubjectViewModel model)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == model.Id);
        if (subject == null) return NotFound();

        if (!ModelState.IsValid) return View(model);

        var code = model.Code.Trim();
        var name = model.Name.Trim();

        if (await _context.Subjects.AnyAsync(s => s.Code == code && s.Id != model.Id))
            ModelState.AddModelError(nameof(model.Code), "Mã môn học này đã được sử dụng.");

        if (await _context.Subjects.AnyAsync(s => s.Name == name && s.Id != model.Id))
            ModelState.AddModelError(nameof(model.Name), "Tên môn học này đã được sử dụng.");

        if (!ModelState.IsValid) return View(model);

        subject.Name = name;
        subject.Code = code;
        subject.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật môn học thành công.";
        return RedirectToAction(nameof(Subject));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSubjectActive(int id)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == id);
        if (subject == null) return NotFound();

        subject.IsActive = !subject.IsActive;
        await _context.SaveChangesAsync();

        TempData["Success"] = subject.IsActive
            ? $"Đã mở khóa môn {subject.Name}."
            : $"Đã khóa môn {subject.Name}.";
        return RedirectToAction(nameof(Subject));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == id);
        if (subject == null) return NotFound();

        // Không xóa môn đã được phân công hoặc đã được dùng trong câu hỏi/đề thi.
        // Các DbSet liên quan được giữ trong DbContext để tương thích database gốc;
        // các chức năng quản lý câu hỏi/đề thi không được expose trong bản thu gọn này.
        var hasLecturer = await _context.LecturerSubjects.AnyAsync(ls => ls.SubjectId == id);
        var hasQuestions = await _context.Questions.AnyAsync(q => q.SubjectId == id);
        var hasExams = await _context.Exams.AnyAsync(e => e.SubjectId == id);

        if (hasLecturer || hasQuestions || hasExams)
        {
            TempData["Error"] = "Không thể xóa môn học này vì môn đã được sử dụng. Bạn có thể khóa môn học thay vì xóa.";
            return RedirectToAction(nameof(Subject));
        }

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa môn học {subject.Name}.";
        return RedirectToAction(nameof(Subject));
    }

    // =====================================================
    // ISSUE #6 - PHÂN CÔNG MÔN HỌC CHO GIẢNG VIÊN
    // Danh sách Lecturer chỉ dùng để chọn người được phân công.
    // Không có tạo/sửa/khóa/xóa/quản lý tài khoản Lecturer ở đây.
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Lecturers()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        var lecturers = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.Role.Name == "Lecturer")
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return View(lecturers);
    }

    [HttpGet]
    public async Task<IActionResult> AssignSubjects(int lecturerId)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        var lecturer = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == lecturerId && u.Role.Name == "Lecturer");

        if (lecturer == null) return NotFound();
        if (!lecturer.IsActive)
        {
            TempData["Error"] = "Không thể phân công môn học cho Lecturer đã bị khóa.";
            return RedirectToAction(nameof(Lecturers));
        }

        var subjects = await _context.Subjects
            .Where(s => s.IsActive)
            .OrderBy(s => s.Code)
            .ToListAsync();

        var assignedSubjectIds = await _context.LecturerSubjects
            .Where(ls => ls.LecturerId == lecturerId)
            .Select(ls => ls.SubjectId)
            .ToListAsync();

        ViewBag.Lecturer = lecturer;
        ViewBag.AssignedSubjectIds = assignedSubjectIds;
        return View(subjects);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSubject(AdminAssignSubjectViewModel model)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        var lecturer = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == model.LecturerId && u.Role.Name == "Lecturer");

        if (lecturer == null) return NotFound();
        if (!lecturer.IsActive)
        {
            TempData["Error"] = "Không thể phân công môn học cho Lecturer đã bị khóa.";
            return RedirectToAction(nameof(Lecturers));
        }

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == model.SubjectId && s.IsActive);

        if (subject == null)
        {
            TempData["Error"] = "Môn học không tồn tại hoặc đã bị khóa.";
            return RedirectToAction(nameof(AssignSubjects), new { lecturerId = model.LecturerId });
        }

        var exists = await _context.LecturerSubjects
            .AnyAsync(ls => ls.LecturerId == model.LecturerId && ls.SubjectId == model.SubjectId);

        if (exists)
        {
            TempData["Error"] = "Lecturer này đã được phân công môn học.";
            return RedirectToAction(nameof(AssignSubjects), new { lecturerId = model.LecturerId });
        }

        _context.LecturerSubjects.Add(new LecturerSubject
        {
            LecturerId = model.LecturerId,
            SubjectId = model.SubjectId
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Đã phân công môn {subject.Name} cho {lecturer.FullName}.";
        return RedirectToAction(nameof(AssignSubjects), new { lecturerId = model.LecturerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSubjectAssignment(int lecturerId, int subjectId)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        var assignment = await _context.LecturerSubjects
            .Include(ls => ls.Lecturer)
            .Include(ls => ls.Subject)
            .FirstOrDefaultAsync(ls => ls.LecturerId == lecturerId && ls.SubjectId == subjectId);

        if (assignment == null)
        {
            TempData["Error"] = "Phân công không tồn tại.";
            return RedirectToAction(nameof(AssignSubjects), new { lecturerId });
        }

        var lecturerName = assignment.Lecturer.FullName;
        var subjectName = assignment.Subject.Name;
        _context.LecturerSubjects.Remove(assignment);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã bỏ phân công môn {subjectName} khỏi {lecturerName}.";
        return RedirectToAction(nameof(AssignSubjects), new { lecturerId });
    }

    [HttpGet]
    public async Task<IActionResult> LecturerAssignments()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");

        var assignments = await _context.LecturerSubjects
            .Include(ls => ls.Lecturer)
            .Include(ls => ls.Subject)
            .OrderBy(ls => ls.Lecturer.FullName)
            .ThenBy(ls => ls.Subject.Code)
            .ToListAsync();

        return View(assignments);
    }

    private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin";
}
