using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Study.Models;
using Study.ViewModels;

namespace Study.Controllers;

public class LecturerController : Controller
{
    private readonly ApplicationDbContext _context;

    public LecturerController(ApplicationDbContext context)
    {
        _context = context;
    }

    private bool IsLecturer()
    {
        var role = HttpContext.Session.GetString("Role");

        return role == "Lecturer";
    }

    private int GetLecturerId()
    {
        return HttpContext.Session.GetInt32("UserId") ?? 0;
    }

    // ==========================================
    // DASHBOARD
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsLecturer())
        {
            return RedirectToAction("Login", "Account");
        }

        int lecturerId = GetLecturerId();

        var subjects = await _context.LecturerSubjects
            .Where(x => x.LecturerId == lecturerId)
            .Include(x => x.Subject)
            .Select(x => x.Subject)
            .ToListAsync();

        return View(subjects);
    }


    // ==========================================
    // QUESTION BANK
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Questions(int? subjectId)
    {
        if (!IsLecturer())
        {
            return RedirectToAction("Login", "Account");
        }

        int lecturerId = GetLecturerId();

        var assignedSubjectIds = await _context.LecturerSubjects
            .Where(x => x.LecturerId == lecturerId)
            .Select(x => x.SubjectId)
            .ToListAsync();

        var subjects = await _context.Subjects
            .Where(x =>
                assignedSubjectIds.Contains(x.Id) &&
                x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var query = _context.Questions
            .Include(q => q.Subject)
            .Include(q => q.Options)
            .Where(q =>
                q.CreatedBy == lecturerId &&
                assignedSubjectIds.Contains(q.SubjectId));

        if (subjectId.HasValue)
        {
            query = query.Where(q =>
                q.SubjectId == subjectId.Value);
        }

        var questions = await query
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        ViewBag.Subjects = subjects;
        ViewBag.SelectedSubjectId = subjectId;

        return View(questions);
    }


    // ==========================================
    // CREATE QUESTION - GET
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> CreateQuestion(int subjectId)
    {
        if (!IsLecturer())
        {
            return RedirectToAction("Login", "Account");
        }

        int lecturerId = GetLecturerId();

        bool assigned = await _context.LecturerSubjects
            .AnyAsync(x =>
                x.LecturerId == lecturerId &&
                x.SubjectId == subjectId);

        if (!assigned)
        {
            TempData["Error"] =
                "Bạn không được phân công môn học này.";

            return RedirectToAction(nameof(Questions));
        }

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(x =>
                x.Id == subjectId &&
                x.IsActive);

        if (subject == null)
        {
            TempData["Error"] =
                "Môn học không tồn tại hoặc đã bị khóa.";

            return RedirectToAction(nameof(Questions));
        }

        var model = new LecturerQuestionViewModel
        {
            SubjectId = subject.Id,
            SubjectName = subject.Name,
            Difficulty = "Easy",

            Options = new List<LecturerOptionViewModel>
            {
                new() { Content = "", IsCorrect = false },
                new() { Content = "", IsCorrect = false },
                new() { Content = "", IsCorrect = false },
                new() { Content = "", IsCorrect = false }
            }
        };

        return View(model);
    }


    // ==========================================
    // CREATE QUESTION - POST
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuestion(
        LecturerQuestionViewModel model)
    {
        if (!IsLecturer())
        {
            return RedirectToAction("Login", "Account");
        }

        int lecturerId = GetLecturerId();

        bool assigned = await _context.LecturerSubjects
            .AnyAsync(x =>
                x.LecturerId == lecturerId &&
                x.SubjectId == model.SubjectId);

        if (!assigned)
        {
            TempData["Error"] =
                "Bạn không được phân công môn học này.";

            return RedirectToAction(nameof(Questions));
        }

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(x =>
                x.Id == model.SubjectId &&
                x.IsActive);

        if (subject == null)
        {
            TempData["Error"] =
                "Môn học không tồn tại hoặc đã bị khóa.";

            return RedirectToAction(nameof(Questions));
        }

        if (model.Options == null ||
            model.Options.Count != 4)
        {
            ModelState.AddModelError(
                "",
                "Câu hỏi phải có đúng 4 đáp án.");
        }
        else
        {
            int correctCount = model.Options
                .Count(x => x.IsCorrect);

            if (correctCount != 1)
            {
                ModelState.AddModelError(
                    "",
                    "Phải chọn đúng 1 đáp án đúng.");
            }

            foreach (var option in model.Options)
            {
                if (string.IsNullOrWhiteSpace(option.Content))
                {
                    ModelState.AddModelError(
                        "",
                        "Không được để trống đáp án.");
                    break;
                }
            }
        }

        if (!ModelState.IsValid)
        {
            model.SubjectName = subject.Name;

            return View(model);
        }

        var question = new Question
        {
            SubjectId = model.SubjectId,
            Topic = string.IsNullOrWhiteSpace(model.Topic)
                ? null
                : model.Topic.Trim(),
            Content = model.Content.Trim(),
            Difficulty = model.Difficulty,
            CreatedBy = lecturerId,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        foreach (var option in model.Options)
        {
            question.Options.Add(
                new QuestionOption
                {
                    Content = option.Content.Trim(),
                    IsCorrect = option.IsCorrect
                });
        }

        _context.Questions.Add(question);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Thêm câu hỏi thành công.";

        return RedirectToAction(
            nameof(Questions),
            new { subjectId = model.SubjectId });
    }
    // ==========================================
    // EDIT QUESTION - GET
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> EditQuestion(int id)
    {
        if (!IsLecturer())
        {
            return RedirectToAction("Login", "Account");
        }

        int lecturerId = GetLecturerId();

        var question = await _context.Questions
            .Include(q => q.Subject)
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q =>
                q.Id == id &&
                q.CreatedBy == lecturerId);

        if (question == null)
        {
            return NotFound();
        }

        bool assigned = await _context.LecturerSubjects
            .AnyAsync(x =>
                x.LecturerId == lecturerId &&
                x.SubjectId == question.SubjectId);

        if (!assigned)
        {
            TempData["Error"] =
                "Bạn không được phân công môn học này.";

            return RedirectToAction(nameof(Questions));
        }

        var model = new LecturerQuestionViewModel
        {
            Id = question.Id,
            SubjectId = question.SubjectId,
            SubjectName = question.Subject.Name,
            Topic = question.Topic,
            Content = question.Content,
            Difficulty = question.Difficulty,

            Options = question.Options
                .OrderBy(x => x.Id)
                .Select(x => new LecturerOptionViewModel
                {
                    Id = x.Id,
                    Content = x.Content,
                    IsCorrect = x.IsCorrect
                })
                .ToList()
        };

        // Đảm bảo luôn có đúng 4 ô đáp án
        while (model.Options.Count < 4)
        {
            model.Options.Add(
                new LecturerOptionViewModel());
        }

        if (model.Options.Count > 4)
        {
            model.Options = model.Options
                .Take(4)
                .ToList();
        }

        return View(model);
    }


    // ==========================================
    // EDIT QUESTION - POST
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditQuestion(
        LecturerQuestionViewModel model)
    {
        if (!IsLecturer())
        {
            return RedirectToAction("Login", "Account");
        }

        int lecturerId = GetLecturerId();

        var question = await _context.Questions
            .Include(q => q.Subject)
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q =>
                q.Id == model.Id &&
                q.CreatedBy == lecturerId);

        if (question == null)
        {
            return NotFound();
        }

        bool assigned = await _context.LecturerSubjects
            .AnyAsync(x =>
                x.LecturerId == lecturerId &&
                x.SubjectId == question.SubjectId);

        if (!assigned)
        {
            TempData["Error"] =
                "Bạn không được phân công môn học này.";

            return RedirectToAction(nameof(Questions));
        }

        if (model.Options == null ||
            model.Options.Count != 4)
        {
            ModelState.AddModelError(
                "",
                "Câu hỏi phải có đúng 4 đáp án.");
        }
        else
        {
            int correctCount = model.Options
                .Count(x => x.IsCorrect);

            if (correctCount != 1)
            {
                ModelState.AddModelError(
                    "",
                    "Phải chọn đúng 1 đáp án đúng.");
            }

            foreach (var option in model.Options)
            {
                if (string.IsNullOrWhiteSpace(option.Content))
                {
                    ModelState.AddModelError(
                        "",
                        "Không được để trống đáp án.");

                    break;
                }
            }
        }

        if (!ModelState.IsValid)
        {
            model.SubjectName = question.Subject.Name;

            return View(model);
        }

        // Cập nhật câu hỏi
        question.Topic =
            string.IsNullOrWhiteSpace(model.Topic)
                ? null
                : model.Topic.Trim();

        question.Content =
            model.Content.Trim();

        question.Difficulty =
            model.Difficulty;


        // Cập nhật 4 đáp án
        var existingOptions = question.Options
            .OrderBy(x => x.Id)
            .ToList();

        for (int i = 0; i < 4; i++)
        {
            existingOptions[i].Content =
                model.Options[i].Content.Trim();

            existingOptions[i].IsCorrect =
                model.Options[i].IsCorrect;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Cập nhật câu hỏi thành công.";

        return RedirectToAction(
            nameof(Questions),
            new { subjectId = question.SubjectId });
    }


    // ==========================================
    // DELETE QUESTION
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        if (!IsLecturer())
        {
            return RedirectToAction("Login", "Account");
        }

        int lecturerId = GetLecturerId();

        var question = await _context.Questions
            .FirstOrDefaultAsync(q =>
                q.Id == id &&
                q.CreatedBy == lecturerId);

        if (question == null)
        {
            return NotFound();
        }

        // Không cho xóa nếu câu hỏi đã được sử dụng trong đề thi
        bool usedInExam = await _context.ExamQuestions
            .AnyAsync(x => x.QuestionId == id);

        if (usedInExam)
        {
            TempData["Error"] =
                "Không thể xóa câu hỏi vì câu hỏi đã được sử dụng trong đề thi. " +
                "Bạn nên chỉnh trạng thái câu hỏi thay vì xóa.";

            return RedirectToAction(
                nameof(Questions),
                new { subjectId = question.SubjectId });
        }

        _context.Questions.Remove(question);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Xóa câu hỏi thành công.";

        return RedirectToAction(
            nameof(Questions),
            new { subjectId = question.SubjectId });
    }
    // ==========================================
    // CREATE EXAM - GET
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> CreateExam()
    {
        if (!IsLecturer())
        {
            return RedirectToAction("Login", "Account");
        }

        int lecturerId = GetLecturerId();

        var subjects = await _context.LecturerSubjects
            .Where(x =>
                x.LecturerId == lecturerId &&
                x.Subject.IsActive)
            .Include(x => x.Subject)
            .Select(x => x.Subject)
            .OrderBy(x => x.Name)
            .ToListAsync();

        if (!subjects.Any())
        {
            TempData["Error"] =
                "Bạn chưa được Admin phân công môn học nào.";

            return RedirectToAction(nameof(Index));
        }

        var model = new LecturerCreateExamViewModel
        {
            SubjectId = subjects.First().Id
        };

        ViewBag.Subjects = subjects;

        ViewBag.Questions = await _context.Questions
            .Where(q =>
                q.CreatedBy == lecturerId &&
                q.SubjectId == model.SubjectId &&
                q.IsActive)
            .Include(q => q.Subject)
            .Include(q => q.Options)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return View(model);
    }


    // ==========================================
    // CREATE EXAM - POST
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExam(
        LecturerCreateExamViewModel model)
    {
        if (!IsLecturer())
        {
            return RedirectToAction("Login", "Account");
        }

        int lecturerId = GetLecturerId();

        // Kiểm tra Lecturer có được phân công môn này không
        bool assigned = await _context.LecturerSubjects
            .AnyAsync(x =>
                x.LecturerId == lecturerId &&
                x.SubjectId == model.SubjectId &&
                x.Subject.IsActive);

        if (!assigned)
        {
            TempData["Error"] =
                "Bạn không được phân công môn học này.";

            return RedirectToAction(nameof(Index));
        }

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(x =>
                x.Id == model.SubjectId &&
                x.IsActive);

        if (subject == null)
        {
            TempData["Error"] =
                "Môn học không tồn tại hoặc đã bị khóa.";

            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra câu hỏi
        if (model.SelectedQuestionIds == null ||
            !model.SelectedQuestionIds.Any())
        {
            ModelState.AddModelError(
                "",
                "Vui lòng chọn ít nhất 1 câu hỏi.");
        }

        // Lấy các câu hỏi Lecturer thực sự sở hữu
        var validQuestions = await _context.Questions
            .Where(q =>
                q.SubjectId == model.SubjectId &&
                q.CreatedBy == lecturerId &&
                q.IsActive &&
                model.SelectedQuestionIds.Contains(q.Id))
            .ToListAsync();

        if (model.SelectedQuestionIds != null)
        {
            if (validQuestions.Count !=
                model.SelectedQuestionIds.Distinct().Count())
            {
                ModelState.AddModelError(
                    "",
                    "Có câu hỏi không thuộc Question Bank của bạn.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadCreateExamData(
                model,
                lecturerId);

            return View(model);
        }

        // Tạo Exam
        var exam = new Exam
        {
            Name = model.Name.Trim(),
            SubjectId = model.SubjectId,
            Description =
                string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim(),

            CreatedBy = lecturerId,

            Status = "Draft",

            CreatedAt = DateTime.Now
        };

        // Loại câu hỏi trùng
        var questionIds =
            model.SelectedQuestionIds
                .Distinct()
                .ToList();

        int order = 1;

        foreach (var questionId in questionIds)
        {
            exam.ExamQuestions.Add(
                new ExamQuestion
                {
                    QuestionId = questionId,
                    QuestionOrder = order
                });

            order++;
        }

        _context.Exams.Add(exam);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Tạo đề thi thành công. Đề đang ở trạng thái Draft.";

        return RedirectToAction(nameof(MyExams));
    }


    // ==========================================
    // LOAD DATA FOR CREATE EXAM
    // ==========================================

    private async Task LoadCreateExamData(
        LecturerCreateExamViewModel model,
        int lecturerId)
    {
        ViewBag.Subjects = await _context.LecturerSubjects
            .Where(x =>
                x.LecturerId == lecturerId &&
                x.Subject.IsActive)
            .Include(x => x.Subject)
            .Select(x => x.Subject)
            .OrderBy(x => x.Name)
            .ToListAsync();

        ViewBag.Questions = await _context.Questions
            .Where(q =>
                q.CreatedBy == lecturerId &&
                q.SubjectId == model.SubjectId &&
                q.IsActive)
            .Include(q => q.Subject)
            .Include(q => q.Options)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }
    // ==========================================
    // MY EXAMS
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> MyExams()
    {
        if (!IsLecturer())
        {
            return RedirectToAction("Login", "Account");
        }

        int lecturerId = GetLecturerId();

        var exams = await _context.Exams
            .Where(e => e.CreatedBy == lecturerId)
            .Include(e => e.Subject)
            .Include(e => e.ExamQuestions)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return View(exams);
    }
    // ==========================================
    // LECTURER DASHBOARD
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> ReviewExam(int id)
    {
        if (!IsLecturer())
        {
            return RedirectToAction("Login", "Account");
        }

        int lecturerId = GetLecturerId();

        var exam = await _context.Exams
            .Where(e =>
                e.Id == id &&
                e.CreatedBy == lecturerId)
            .Include(e => e.Subject)
            .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.Question)
                    .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync();

        if (exam == null)
        {
            return NotFound();
        }

        exam.ExamQuestions =
            exam.ExamQuestions
                .OrderBy(x => x.QuestionOrder)
                .ToList();

        return View(exam);
    }
    // ==========================================
    // EDIT EXAM - GET
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> EditExam(int id)
    {
        if (!IsLecturer())
        {
            return RedirectToAction("Login", "Account");
        }

        int lecturerId = GetLecturerId();

        var exam = await _context.Exams
            .Where(e =>
                e.Id == id &&
                e.CreatedBy == lecturerId)
            .Include(e => e.Subject)
            .Include(e => e.ExamQuestions)
            .FirstOrDefaultAsync();

        if (exam == null)
        {
            return NotFound();
        }

        // Chỉ được sửa Draft hoặc Rejected
        if (exam.Status != "Draft" &&
            exam.Status != "Rejected")
        {
            TempData["Error"] =
                "Chỉ có thể sửa đề thi ở trạng thái Draft hoặc Rejected.";

            return RedirectToAction(nameof(MyExams));
        }

        var model = new LecturerCreateExamViewModel
        {
            Id = exam.Id,
            Name = exam.Name,
            SubjectId = exam.SubjectId,
            Description = exam.Description,

            SelectedQuestionIds = exam.ExamQuestions
                .OrderBy(x => x.QuestionOrder)
                .Select(x => x.QuestionId)
                .ToList()
        };

        await LoadCreateExamData(model, lecturerId);

        return View(model);
    }


    // ==========================================
    // EDIT EXAM - POST
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditExam(
        LecturerCreateExamViewModel model)
    {
        if (!IsLecturer())
        {
            return RedirectToAction("Login", "Account");
        }

        int lecturerId = GetLecturerId();

        var exam = await _context.Exams
            .Where(e =>
                e.Id == model.Id &&
                e.CreatedBy == lecturerId)
            .Include(e => e.ExamQuestions)
            .FirstOrDefaultAsync();

        if (exam == null)
        {
            return NotFound();
        }

        // Chỉ Draft / Rejected mới được sửa
        if (exam.Status != "Draft" &&
            exam.Status != "Rejected")
        {
            TempData["Error"] =
                "Đề thi này không còn được phép chỉnh sửa.";

            return RedirectToAction(nameof(MyExams));
        }

        // Kiểm tra Lecturer có được phân công môn này không
        bool assigned = await _context.LecturerSubjects
            .AnyAsync(x =>
                x.LecturerId == lecturerId &&
                x.SubjectId == model.SubjectId &&
                x.Subject.IsActive);

        if (!assigned)
        {
            TempData["Error"] =
                "Bạn không được phân công môn học này.";

            return RedirectToAction(nameof(MyExams));
        }

        // Kiểm tra môn học
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(x =>
                x.Id == model.SubjectId &&
                x.IsActive);

        if (subject == null)
        {
            TempData["Error"] =
                "Môn học không tồn tại hoặc đã bị khóa.";

            return RedirectToAction(nameof(MyExams));
        }

        // Kiểm tra phải có câu hỏi
        if (model.SelectedQuestionIds == null ||
            !model.SelectedQuestionIds.Any())
        {
            ModelState.AddModelError(
                "",
                "Vui lòng chọn ít nhất 1 câu hỏi.");
        }

        // Lấy câu hỏi hợp lệ của Lecturer
        var selectedIds =
            model.SelectedQuestionIds?
                .Distinct()
                .ToList()
            ?? new List<int>();

        var validQuestions = await _context.Questions
            .Where(q =>
                q.SubjectId == model.SubjectId &&
                q.CreatedBy == lecturerId &&
                q.IsActive &&
                selectedIds.Contains(q.Id))
            .ToListAsync();

        if (validQuestions.Count != selectedIds.Count)
        {
            ModelState.AddModelError(
                "",
                "Có câu hỏi không thuộc Question Bank của bạn.");
        }

        if (!ModelState.IsValid)
        {
            await LoadCreateExamData(
                model,
                lecturerId);

            return View(model);
        }

        // Cập nhật thông tin Exam
        exam.Name = model.Name.Trim();

        exam.SubjectId = model.SubjectId;

        exam.Description =
            string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim();

        // Nếu sửa đề bị từ chối thì đưa về Draft
        // để Lecturer kiểm tra lại trước khi gửi
        // Admin lần nữa.
        if (exam.Status == "Rejected")
        {
            exam.Status = "Draft";
            exam.RejectReason = null;
            exam.ReviewedAt = null;
        }

        // Xóa danh sách câu hỏi cũ
        _context.ExamQuestions.RemoveRange(
            exam.ExamQuestions);

        // Thêm danh sách câu hỏi mới
        int order = 1;

        foreach (var questionId in selectedIds)
        {
            exam.ExamQuestions.Add(
                new ExamQuestion
                {
                    ExamId = exam.Id,
                    QuestionId = questionId,
                    QuestionOrder = order
                });

            order++;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Cập nhật đề thi thành công.";

        return RedirectToAction(
            nameof(ReviewExam),
            new { id = exam.Id });
    }
    // ==========================================
    // SUBMIT EXAM FOR ADMIN APPROVAL
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitExam(int id)
    {
        if (!IsLecturer())
        {
            return RedirectToAction("Login", "Account");
        }

        int lecturerId = GetLecturerId();

        var exam = await _context.Exams
            .Where(e =>
                e.Id == id &&
                e.CreatedBy == lecturerId)
            .Include(e => e.ExamQuestions)
            .FirstOrDefaultAsync();

        if (exam == null)
        {
            return NotFound();
        }

        // Chỉ Draft mới được gửi duyệt
        if (exam.Status != "Draft")
        {
            TempData["Error"] =
                "Chỉ đề thi ở trạng thái Draft mới có thể gửi Admin duyệt.";

            return RedirectToAction(
                nameof(ReviewExam),
                new { id });
        }

        // Kiểm tra phải có câu hỏi
        if (!exam.ExamQuestions.Any())
        {
            TempData["Error"] =
                "Đề thi phải có ít nhất 1 câu hỏi trước khi gửi Admin.";

            return RedirectToAction(
                nameof(ReviewExam),
                new { id });
        }

        // Chuyển sang chờ Admin duyệt
        exam.Status = "PendingApproval";

        exam.SubmittedAt = DateTime.Now;

        exam.ReviewedAt = null;

        exam.RejectReason = null;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Đã gửi đề thi cho Admin. Vui lòng chờ phê duyệt.";

        return RedirectToAction(nameof(MyExams));
    }

}