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
    public async Task<IActionResult> Questions(int? subjectId, string? search, int page = 1)
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

        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(q => q.Content.Contains(search) || (q.Topic != null && q.Topic.Contains(search)));
        const int pageSize = 12;
        var totalCount = await query.CountAsync();
        page = Math.Max(1, page);
        var questions = await query.OrderByDescending(q => q.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Subjects = subjects;
        ViewBag.SelectedSubjectId = subjectId;
        ViewBag.Search = search; ViewBag.Page = page; ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

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
    // IMPORT QUESTION BANK FROM EXCEL
    // Workbook columns: SubjectCode, Topic, Content, Difficulty,
    // OptionA, OptionB, OptionC, OptionD, CorrectOption (A/B/C/D).
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> ImportQuestions()
    {
        if (!IsLecturer()) return RedirectToAction("Login", "Account");
        var lecturerId = GetLecturerId();
        ViewBag.Subjects = await _context.LecturerSubjects
            .Where(x => x.LecturerId == lecturerId)
            .Select(x => x.Subject).Where(x => x.IsActive)
            .OrderBy(x => x.Name).ToListAsync();
        return View();
    }

    [HttpGet]
    public IActionResult DownloadQuestionTemplate()
    {
        if (!IsLecturer()) return Forbid();
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var sheet = workbook.Worksheets.Add("Questions");
        var headers = new[] { "SubjectCode", "Topic", "Content", "Difficulty", "OptionA", "OptionB", "OptionC", "OptionD", "CorrectOption" };
        for (int i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Cell(2, 1).Value = "CS101"; sheet.Cell(2, 2).Value = "Chương 1";
        sheet.Cell(2, 3).Value = "Nội dung câu hỏi mẫu?"; sheet.Cell(2, 4).Value = "Easy";
        sheet.Cell(2, 5).Value = "Đáp án A"; sheet.Cell(2, 6).Value = "Đáp án B";
        sheet.Cell(2, 7).Value = "Đáp án C"; sheet.Cell(2, 8).Value = "Đáp án D";
        sheet.Cell(2, 9).Value = "A";
        sheet.Row(1).Style.Font.Bold = true; sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream(); workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Study_Template_Questions.xlsx");
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> ImportQuestions(IFormFile? file)
    {
        if (!IsLecturer()) return Forbid();
        if (file == null || file.Length == 0 || file.Length > 8 * 1024 * 1024 ||
            !Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Chọn file .xlsx hợp lệ, dung lượng tối đa 8 MB.";
            return RedirectToAction(nameof(ImportQuestions));
        }

        var lecturerId = GetLecturerId();
        var assigned = await _context.LecturerSubjects
            .Where(x => x.LecturerId == lecturerId)
            .Select(x => x.Subject).Where(x => x.IsActive)
            .ToListAsync();
        var subjectMap = assigned.ToDictionary(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase);

        using var workbook = TryOpenQuestionWorkbook(file);
        if (workbook == null)
        {
            TempData["Error"] = "Không thể đọc file Excel. Hãy kiểm tra định dạng .xlsx.";
            return RedirectToAction(nameof(ImportQuestions));
        }
        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet == null)
        {
            TempData["Error"] = "File không có trang tính.";
            return RedirectToAction(nameof(ImportQuestions));
        }

        var rows = sheet.RowsUsed().Skip(1).Take(1001).ToList();
        if (rows.Count > 1000)
        {
            TempData["Error"] = "Mỗi lần chỉ nhập tối đa 1.000 câu hỏi.";
            return RedirectToAction(nameof(ImportQuestions));
        }

        int imported = 0, skipped = 0;
        var errors = new List<string>();
        foreach (var row in rows)
        {
            var code = row.Cell(1).GetString().Trim();
            var topic = row.Cell(2).GetString().Trim();
            var content = row.Cell(3).GetString().Trim();
            var difficulty = row.Cell(4).GetString().Trim();
            var options = Enumerable.Range(5, 4).Select(c => row.Cell(c).GetString().Trim()).ToList();
            var correct = row.Cell(9).GetString().Trim().ToUpperInvariant();

            if (!subjectMap.TryGetValue(code, out var subjectId) ||
                content.Length == 0 || options.Any(x => x.Length == 0) ||
                !new[] { "A", "B", "C", "D" }.Contains(correct) ||
                !new[] { "Easy", "Medium", "Hard", "Dễ", "Trung bình", "Khó" }
                    .Contains(difficulty, StringComparer.OrdinalIgnoreCase))
            {
                skipped++;
                errors.Add($"Dòng {row.RowNumber()}: môn học chưa được phân công hoặc dữ liệu câu hỏi/độ khó/đáp án không hợp lệ.");
                continue;
            }

            var normalizedDifficulty = difficulty.Equals("Dễ", StringComparison.OrdinalIgnoreCase) ? "Easy"
                : difficulty.Equals("Trung bình", StringComparison.OrdinalIgnoreCase) ? "Medium"
                : difficulty.Equals("Khó", StringComparison.OrdinalIgnoreCase) ? "Hard" : difficulty;
            var question = new Question
            {
                SubjectId = subjectId, Topic = string.IsNullOrWhiteSpace(topic) ? null : topic,
                Content = content, Difficulty = normalizedDifficulty, CreatedBy = lecturerId,
                IsActive = true, CreatedAt = DateTime.Now
            };
            for (int index = 0; index < 4; index++)
                question.Options.Add(new QuestionOption
                {
                    Content = options[index], IsCorrect = correct == "ABCD"[index].ToString()
                });
            _context.Questions.Add(question);
            imported++;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Đã nhập {imported} câu hỏi; bỏ qua {skipped} dòng.";
        TempData["ImportErrors"] = string.Join("\n", errors.Take(20));
        return RedirectToAction(nameof(Questions));
    }

    private static ClosedXML.Excel.XLWorkbook? TryOpenQuestionWorkbook(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            return new ClosedXML.Excel.XLWorkbook(stream);
        }
        catch
        {
            return null;
        }
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
        var adminIds = await _context.Users.Where(u => u.Role.Name == "Admin" && u.IsActive).Select(u => u.Id).ToListAsync();
        foreach (var adminId in adminIds) _context.Notifications.Add(new Notifications { UserId = adminId, ExamId = exam.Id, Title = "Đề thi cần duyệt", Message = $"Giảng viên {HttpContext.Session.GetString("FullName")} đã gửi đề '{exam.Name}' chờ duyệt.", Type = "ExamReview", IsRead = false, CreatedAt = DateTime.Now });

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Đã gửi đề thi cho Admin. Vui lòng chờ phê duyệt.";

        return RedirectToAction(nameof(MyExams));
    }


    [HttpGet]
    public async Task<IActionResult> GenerateExam()
    {
        if (!IsLecturer()) return RedirectToAction("Login", "Account");
        ViewBag.Subjects = await _context.LecturerSubjects.Where(x => x.LecturerId == GetLecturerId()).Select(x => x.Subject).Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateExam(string name, int subjectId, int easyCount, int mediumCount, int hardCount, string? description)
    {
        if (!IsLecturer()) return Forbid();
        int lecturerId = GetLecturerId();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200 || easyCount < 0 || mediumCount < 0 || hardCount < 0 || easyCount + mediumCount + hardCount < 1 || easyCount + mediumCount + hardCount > 200)
        { TempData["Error"] = "Tên đề không hợp lệ; tổng số câu phải từ 1 đến 200 và số câu từng mức không âm."; return RedirectToAction(nameof(GenerateExam)); }
        bool assigned = await _context.LecturerSubjects.AnyAsync(x => x.LecturerId == lecturerId && x.SubjectId == subjectId);
        if (!assigned) return Forbid();
        var counts = await _context.Questions.Where(q => q.SubjectId == subjectId && q.CreatedBy == lecturerId && q.IsActive)
            .GroupBy(q => q.Difficulty).Select(g => new { Difficulty = g.Key, Count = g.Count() }).ToListAsync();
        int availableEasy = counts.Where(x => x.Difficulty.ToLower() == "easy").Sum(x => x.Count);
        int availableMedium = counts.Where(x => x.Difficulty.ToLower() == "medium").Sum(x => x.Count);
        int availableHard = counts.Where(x => x.Difficulty.ToLower() == "hard").Sum(x => x.Count);
        if (easyCount > availableEasy || mediumCount > availableMedium || hardCount > availableHard)
        { TempData["Error"] = $"Ngân hàng chưa đủ câu. Hiện có: Dễ {availableEasy}, Trung bình {availableMedium}, Khó {availableHard}."; return RedirectToAction(nameof(GenerateExam)); }
        var selected = new List<Question>();
        foreach (var (difficulty, count) in new[] { ("Easy", easyCount), ("Medium", mediumCount), ("Hard", hardCount) })
        {
            if (count == 0) continue;
            var ids = await _context.Questions.Where(q => q.SubjectId == subjectId && q.CreatedBy == lecturerId && q.IsActive && q.Difficulty.ToLower() == difficulty.ToLower()).Select(q => q.Id).ToListAsync();
            var picked = ids.OrderBy(_ => Guid.NewGuid()).Take(count).ToList();
            selected.AddRange(await _context.Questions.Where(q => picked.Contains(q.Id)).ToListAsync());
        }
        selected = selected.OrderBy(_ => Guid.NewGuid()).ToList();
        var exam = new Exam { Name = name.Trim(), SubjectId = subjectId, Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(), CreatedBy = lecturerId, Status = "Draft", CreatedAt = DateTime.Now };
        for (int i = 0; i < selected.Count; i++) exam.ExamQuestions.Add(new ExamQuestion { QuestionId = selected[i].Id, QuestionOrder = i + 1 });
        _context.Exams.Add(exam); await _context.SaveChangesAsync();
        TempData["Success"] = $"Đã tạo đề thi tự động gồm {selected.Count} câu (Dễ {easyCount}, Trung bình {mediumCount}, Khó {hardCount}). Đề ở trạng thái Draft.";
        return RedirectToAction(nameof(MyExams));
    }

}