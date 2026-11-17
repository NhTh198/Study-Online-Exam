using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Study.Models;
using Study.ViewModels;

namespace Study.Controllers;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================
    // DANH SÁCH TÀI KHOẢN
    // =========================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var users = await _context.Users
            .Include(u => u.Role)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return View(users);
    }

    // =========================
    // TẠO TÀI KHOẢN - GET
    // =========================
    [HttpGet]
    public async Task<IActionResult> CreateUser()
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.Roles = await _context.Roles
            .Where(r => r.Name == "Lecturer" || r.Name == "Student")
            .ToListAsync();

        return View();
    }

    // =========================
    // TẠO TÀI KHOẢN - POST
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(
        AdminCreateUserViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        // Chỉ cho tạo Lecturer hoặc Student
        var role = await _context.Roles
            .FirstOrDefaultAsync(r =>
                r.Id == model.RoleId &&
                (r.Name == "Lecturer" || r.Name == "Student"));

        if (role == null)
        {
            ModelState.AddModelError(
                "RoleId",
                "Vai trò không hợp lệ."
            );
        }

        // Kiểm tra email trùng
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == model.Email.Trim());

        if (emailExists)
        {
            ModelState.AddModelError(
                "Email",
                "Email này đã tồn tại."
            );
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _context.Roles
                .Where(r =>
                    r.Name == "Lecturer" ||
                    r.Name == "Student")
                .ToListAsync();

            return View(model);
        }

        // =========================
        // TỰ ĐỘNG HASH PASSWORD
        // =========================
        string hashedPassword =
            BCrypt.Net.BCrypt.HashPassword(model.Password);

        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            Password = hashedPassword,
            StudentCode = string.IsNullOrWhiteSpace(model.StudentCode)
                ? null
                : model.StudentCode.Trim(),
            RoleId = model.RoleId,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Tạo tài khoản {role.Name} thành công.";

        return RedirectToAction(nameof(Index));
    }

    // =========================
    // KHÓA / MỞ KHÓA
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        // Không cho khóa Admin
        if (user.Role.Name == "Admin")
        {
            TempData["Error"] =
                "Không thể khóa tài khoản Admin.";

            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;

        await _context.SaveChangesAsync();

        TempData["Success"] = user.IsActive
            ? "Đã mở khóa tài khoản."
            : "Đã khóa tài khoản.";

        return RedirectToAction(nameof(Index));
    }

    // =========================
    // XÓA TÀI KHOẢN
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        // Không cho xóa Admin
        if (user.Role.Name == "Admin")
        {
            TempData["Error"] =
                "Không thể xóa tài khoản Admin.";

            return RedirectToAction(nameof(Index));
        }

        _context.Users.Remove(user);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Đã xóa tài khoản thành công.";

        return RedirectToAction(nameof(Index));
    }

    // =========================
    // RESET MẬT KHẨU
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        int id,
        string newPassword)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(newPassword) ||
            newPassword.Length < 6)
        {
            TempData["Error"] =
                "Mật khẩu mới phải có ít nhất 6 ký tự.";

            return RedirectToAction(nameof(Index));
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        // Không reset Admin ở chức năng này
        if (user.Role.Name == "Admin")
        {
            TempData["Error"] =
                "Không thể reset mật khẩu Admin tại đây.";

            return RedirectToAction(nameof(Index));
        }

        // Tự hash mật khẩu mới
        user.Password =
            BCrypt.Net.BCrypt.HashPassword(newPassword);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Đã reset mật khẩu thành công.";

        return RedirectToAction(nameof(Index));
    }

    // =========================
    // CHECK ADMIN
    // =========================
    private bool IsAdmin()
    {
        var role = HttpContext.Session.GetString("Role");

        return role == "Admin";
    }
    // =========================
    // SỬA TÀI KHOẢN - GET
    // =========================
    [HttpGet]
    public async Task<IActionResult> EditUser(int id)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        // Không cho sửa Admin
        if (user.Role.Name == "Admin")
        {
            TempData["Error"] =
                "Không thể sửa tài khoản Admin.";

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Roles = await _context.Roles
            .Where(r =>
                r.Name == "Lecturer" ||
                r.Name == "Student")
            .ToListAsync();

        var model = new AdminEditUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            StudentCode = user.StudentCode,
            RoleId = user.RoleId
        };

        return View(model);
    }


    // =========================
    // SỬA TÀI KHOẢN - POST
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(
        AdminEditUserViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == model.Id);

        if (user == null)
        {
            return NotFound();
        }

        // Không cho sửa Admin
        if (user.Role.Name == "Admin")
        {
            TempData["Error"] =
                "Không thể sửa tài khoản Admin.";

            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra Role
        var role = await _context.Roles
            .FirstOrDefaultAsync(r =>
                r.Id == model.RoleId &&
                (r.Name == "Lecturer" ||
                 r.Name == "Student"));

        if (role == null)
        {
            ModelState.AddModelError(
                "RoleId",
                "Vai trò không hợp lệ.");
        }

        // Kiểm tra email trùng với tài khoản khác
        var emailExists = await _context.Users
            .AnyAsync(u =>
                u.Email == model.Email.Trim() &&
                u.Id != model.Id);

        if (emailExists)
        {
            ModelState.AddModelError(
                "Email",
                "Email này đã được sử dụng.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _context.Roles
                .Where(r =>
                    r.Name == "Lecturer" ||
                    r.Name == "Student")
                .ToListAsync();

            return View(model);
        }

        // Cập nhật thông tin
        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.StudentCode =
            string.IsNullOrWhiteSpace(model.StudentCode)
                ? null
                : model.StudentCode.Trim();

        user.RoleId = model.RoleId;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Cập nhật tài khoản thành công.";

        return RedirectToAction(nameof(Index));
    }
    // ==============================
    // QUẢN LÝ MÔN HỌC
    // ==============================

    [HttpGet]
    public async Task<IActionResult> Subject()
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var subjects = await _context.Subjects
            .OrderBy(s => s.Code)
            .ToListAsync();

        return View(subjects);
    }


    // ==============================
    // THÊM MÔN HỌC
    // ==============================

    [HttpGet]
    public IActionResult CreateSubject()
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubject(
        AdminSubjectViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        string code = model.Code.Trim();
        string name = model.Name.Trim();

        // Kiểm tra mã môn đã tồn tại
        bool codeExists = await _context.Subjects
            .AnyAsync(s => s.Code == code);

        if (codeExists)
        {
            ModelState.AddModelError(
                "Code",
                "Mã môn học này đã tồn tại.");
        }

        // Kiểm tra tên môn đã tồn tại
        bool nameExists = await _context.Subjects
            .AnyAsync(s => s.Name == name);

        if (nameExists)
        {
            ModelState.AddModelError(
                "Name",
                "Tên môn học này đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var subject = new Subject
        {
            Name = name,
            Code = code,
            Description = string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim(),
            IsActive = true
        };

        _context.Subjects.Add(subject);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Đã thêm môn học {subject.Name} thành công.";

        return RedirectToAction(nameof(Subject));
    }


    // ==============================
    // SỬA MÔN HỌC
    // ==============================

    [HttpGet]
    public async Task<IActionResult> EditSubject(int id)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subject == null)
        {
            return NotFound();
        }

        var model = new AdminSubjectViewModel
        {
            Id = subject.Id,
            Name = subject.Name,
            Code = subject.Code,
            Description = subject.Description
        };

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSubject(
        AdminSubjectViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == model.Id);

        if (subject == null)
        {
            return NotFound();
        }

        string code = model.Code.Trim();
        string name = model.Name.Trim();

        // Kiểm tra mã môn trùng với môn khác
        bool codeExists = await _context.Subjects
            .AnyAsync(s =>
                s.Code == code &&
                s.Id != model.Id);

        if (codeExists)
        {
            ModelState.AddModelError(
                "Code",
                "Mã môn học này đã được sử dụng.");
        }

        // Kiểm tra tên môn trùng với môn khác
        bool nameExists = await _context.Subjects
            .AnyAsync(s =>
                s.Name == name &&
                s.Id != model.Id);

        if (nameExists)
        {
            ModelState.AddModelError(
                "Name",
                "Tên môn học này đã được sử dụng.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        subject.Name = name;
        subject.Code = code;
        subject.Description =
            string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim();

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Cập nhật môn học thành công.";

        return RedirectToAction(nameof(Subject));
    }


    // ==============================
    // KHÓA / MỞ KHÓA MÔN HỌC
    // ==============================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSubjectActive(int id)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subject == null)
        {
            return NotFound();
        }

        subject.IsActive = !subject.IsActive;

        await _context.SaveChangesAsync();

        TempData["Success"] = subject.IsActive
            ? $"Đã mở khóa môn {subject.Name}."
            : $"Đã khóa môn {subject.Name}.";

        return RedirectToAction(nameof(Subject));
    }


    // ==============================
    // XÓA MÔN HỌC
    // ==============================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subject == null)
        {
            return NotFound();
        }

        // Không cho xóa nếu môn đã được phân công Lecturer
        bool hasLecturer = await _context.LecturerSubjects
            .AnyAsync(ls => ls.SubjectId == id);

        // Không cho xóa nếu môn đã có câu hỏi
        bool hasQuestions = await _context.Questions
            .AnyAsync(q => q.SubjectId == id);

        // Không cho xóa nếu môn đã có kỳ thi
        bool hasExams = await _context.Exams
            .AnyAsync(e => e.SubjectId == id);

        if (hasLecturer || hasQuestions || hasExams)
        {
            TempData["Error"] =
                "Không thể xóa môn học này vì môn đã được sử dụng. " +
                "Bạn có thể khóa môn học thay vì xóa.";

            return RedirectToAction(nameof(Subject));
        }

        _context.Subjects.Remove(subject);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Đã xóa môn học {subject.Name}.";

        return RedirectToAction(nameof(Subject));
    }
    // =====================================================
    // QUẢN LÝ LECTURER
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Lecturers()
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var lecturers = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.Role.Name == "Lecturer")
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return View(lecturers);
    }


    // =====================================================
    // PHÂN CÔNG LECTURER - MÔN HỌC
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> AssignSubjects(int lecturerId)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var lecturer = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.Id == lecturerId &&
                u.Role.Name == "Lecturer");

        if (lecturer == null)
        {
            return NotFound();
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


    // =====================================================
    // THÊM PHÂN CÔNG
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSubject(
        AdminAssignSubjectViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var lecturer = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.Id == model.LecturerId &&
                u.Role.Name == "Lecturer");

        if (lecturer == null)
        {
            return NotFound();
        }

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s =>
                s.Id == model.SubjectId &&
                s.IsActive);

        if (subject == null)
        {
            TempData["Error"] =
                "Môn học không tồn tại hoặc đã bị khóa.";

            return RedirectToAction(
                nameof(AssignSubjects),
                new { lecturerId = model.LecturerId });
        }

        // Kiểm tra đã phân công chưa
        bool exists = await _context.LecturerSubjects
            .AnyAsync(ls =>
                ls.LecturerId == model.LecturerId &&
                ls.SubjectId == model.SubjectId);

        if (exists)
        {
            TempData["Error"] =
                "Lecturer này đã được phân công môn học.";

            return RedirectToAction(
                nameof(AssignSubjects),
                new { lecturerId = model.LecturerId });
        }

        var assignment = new LecturerSubject
        {
            LecturerId = model.LecturerId,
            SubjectId = model.SubjectId
        };

        _context.LecturerSubjects.Add(assignment);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Đã phân công môn {subject.Name} cho {lecturer.FullName}.";

        return RedirectToAction(
            nameof(AssignSubjects),
            new { lecturerId = model.LecturerId });
    }


    // =====================================================
    // XÓA PHÂN CÔNG
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSubjectAssignment(
        int lecturerId,
        int subjectId)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var assignment = await _context.LecturerSubjects
            .Include(ls => ls.Lecturer)
            .Include(ls => ls.Subject)
            .FirstOrDefaultAsync(ls =>
                ls.LecturerId == lecturerId &&
                ls.SubjectId == subjectId);

        if (assignment == null)
        {
            TempData["Error"] =
                "Phân công không tồn tại.";

            return RedirectToAction(
                nameof(AssignSubjects),
                new { lecturerId });
        }

        string lecturerName = assignment.Lecturer.FullName;
        string subjectName = assignment.Subject.Name;

        _context.LecturerSubjects.Remove(assignment);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Đã bỏ phân công môn {subjectName} khỏi {lecturerName}.";

        return RedirectToAction(
            nameof(AssignSubjects),
            new { lecturerId });
    }


    // =====================================================
    // XEM TOÀN BỘ PHÂN CÔNG
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> LecturerAssignments()
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var assignments = await _context.LecturerSubjects
            .Include(ls => ls.Lecturer)
            .Include(ls => ls.Subject)
            .OrderBy(ls => ls.Lecturer.FullName)
            .ThenBy(ls => ls.Subject.Code)
            .ToListAsync();

        return View(assignments);
    }
    // =====================================================
    // QUẢN LÝ / DUYỆT KỲ THI
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Exams()
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var exams = await _context.Exams
            .Include(e => e.Subject)
            .Include(e => e.Creator)
            .Include(e => e.ExamQuestions)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var result = exams.Select(e => new AdminReviewExamViewModel
        {
            Id = e.Id,
            Name = e.Name,
            SubjectName = e.Subject.Name,
            SubjectCode = e.Subject.Code,
            Description = e.Description,
            LecturerName = e.Creator.FullName,
            QuestionCount = e.ExamQuestions.Count,
            Status = e.Status,
            CreatedAt = e.CreatedAt,
            SubmittedAt = e.SubmittedAt,
            RejectReason = e.RejectReason
        }).ToList();

        return View(result);
    }
    [HttpGet]
    public async Task<IActionResult> ReviewExam(int id)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var exam = await _context.Exams
            .Include(e => e.Subject)
            .Include(e => e.Creator)
            .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.Question)
                    .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam == null)
        {
            return NotFound();
        }

        return View(exam);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveExam(int id)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var exam = await _context.Exams
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam == null)
        {
            return NotFound();
        }

        if (exam.Status != "PendingApproval")
        {
            TempData["Error"] =
                "Chỉ có thể duyệt kỳ thi đang chờ duyệt.";

            return RedirectToAction(nameof(Exams));
        }

        exam.Status = "Approved";
        exam.ReviewedAt = DateTime.Now;
        exam.RejectReason = null;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Đã duyệt kỳ thi thành công.";

        return RedirectToAction(nameof(Exams));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectExam(
    AdminRejectExamViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var exam = await _context.Exams
            .FirstOrDefaultAsync(e => e.Id == model.ExamId);

        if (exam == null)
        {
            return NotFound();
        }

        if (exam.Status != "PendingApproval")
        {
            TempData["Error"] =
                "Chỉ có thể từ chối kỳ thi đang chờ duyệt.";

            return RedirectToAction(nameof(Exams));
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] =
                "Vui lòng nhập lý do từ chối.";

            return RedirectToAction(
                nameof(ReviewExam),
                new { id = model.ExamId });
        }

        exam.Status = "Rejected";
        exam.RejectReason = model.RejectReason.Trim();
        exam.ReviewedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Đã từ chối kỳ thi.";

        return RedirectToAction(nameof(Exams));
    }
    [HttpGet]
    public async Task<IActionResult> ScheduleExam(int id)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var exam = await _context.Exams
            .Include(e => e.Subject)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam == null)
        {
            return NotFound();
        }

        if (exam.Status != "Approved")
        {
            TempData["Error"] =
                "Chỉ có thể xếp lịch cho đề thi đã được Admin duyệt.";

            return RedirectToAction(nameof(Exams));
        }

        var schedule = await _context.ExamSchedules
            .FirstOrDefaultAsync(x => x.ExamId == id);

        var model = new AdminScheduleExamViewModel
        {
            ExamId = exam.Id,
            ExamName = exam.Name,
            SubjectName = exam.Subject.Name,
            StartTime = schedule?.StartTime ?? DateTime.Now.AddHours(1),
            EndTime = schedule?.EndTime ?? DateTime.Now.AddHours(2),
            DurationMinutes = schedule?.DurationMinutes ?? 60,
            GraceMinutes = 15,
            ExamKey = schedule?.ExamKey
        };

        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScheduleExam(
    AdminScheduleExamViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var exam = await _context.Exams
            .Include(e => e.Subject)
            .FirstOrDefaultAsync(e => e.Id == model.ExamId);

        if (exam == null)
        {
            return NotFound();
        }

        if (exam.Status != "Approved" &&
            exam.Status != "Scheduled")
        {
            TempData["Error"] =
                "Đề thi chưa ở trạng thái có thể xếp lịch.";

            return RedirectToAction(nameof(Exams));
        }

        if (model.StartTime >= model.EndTime)
        {
            ModelState.AddModelError(
                "EndTime",
                "Thời gian kết thúc phải sau thời gian bắt đầu.");
        }

        if (model.DurationMinutes <= 0)
        {
            ModelState.AddModelError(
                "DurationMinutes",
                "Thời lượng phải lớn hơn 0 phút.");
        }

        if (!ModelState.IsValid)
        {
            model.ExamName = exam.Name;
            model.SubjectName = exam.Subject.Name;
            model.GraceMinutes = 15;

            return View(model);
        }

        var schedule = await _context.ExamSchedules
            .FirstOrDefaultAsync(x => x.ExamId == model.ExamId);

        if (schedule == null)
        {
            schedule = new ExamSchedule
            {
                ExamId = exam.Id,
                CreatedByAdmin = int.Parse(
                    HttpContext.Session.GetInt32("UserId")!.Value.ToString()),
                CreatedAt = DateTime.Now
            };

            _context.ExamSchedules.Add(schedule);
        }

        schedule.StartTime = model.StartTime;
        schedule.EndTime = model.EndTime;
        schedule.DurationMinutes = model.DurationMinutes;

        // Theo thiết kế: Grace Period cố định 15 phút
        schedule.GraceMinutes = 15;

        // Admin nhập Key thì giữ lại.
        // Nếu bỏ trống thì hệ thống tự tạo.
        if (string.IsNullOrWhiteSpace(model.ExamKey))
        {
            schedule.ExamKey = GenerateExamKey();
        }
        else
        {
            schedule.ExamKey = model.ExamKey.Trim().ToUpper();
        }

        // Admin được xem Key trước giờ thi 10 phút
        schedule.KeyVisibleAt =
            model.StartTime.AddMinutes(-10);

        // Key hết hạn khi phiên thi chính thức kết thúc
        schedule.KeyExpiredAt =
            model.EndTime;

        schedule.IsPublished = false;

        exam.Status = "Scheduled";

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Xếp lịch thi và tạo Exam Key thành công.";

        return RedirectToAction(nameof(Exams));
    }
    private string GenerateExamKey()
    {
        const string chars =
            "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        var random = new Random();

        return new string(
            Enumerable.Range(0, 8)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishExam(int id)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var exam = await _context.Exams
            .Include(e => e.Schedule)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam == null)
        {
            return NotFound();
        }

        if (exam.Status != "Scheduled")
        {
            TempData["Error"] =
                "Chỉ có thể công bố đề thi đã được xếp lịch.";

            return RedirectToAction(nameof(Exams));
        }

        if (exam.Schedule == null)
        {
            TempData["Error"] =
                "Đề thi chưa được xếp lịch.";

            return RedirectToAction(nameof(Exams));
        }

        var schedule = exam.Schedule;

        if (string.IsNullOrWhiteSpace(schedule.ExamKey))
        {
            schedule.ExamKey = GenerateExamKey();
        }

        schedule.IsPublished = true;

        exam.Status = "Published";

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Đề thi đã được công bố thành công.";

        return RedirectToAction(nameof(Exams));
    }

}