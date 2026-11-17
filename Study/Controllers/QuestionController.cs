using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Study.Models;

namespace Study.Controllers
{
    public class QuestionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuestionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // SESSION / AUTHORIZATION
        // ==========================================

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

        private string? CurrentRole => HttpContext.Session.GetString("Role");

        private bool IsAdmin => CurrentRole == "Admin";

        private bool IsLecturer => CurrentRole == "Lecturer";

        private IActionResult? CheckLogin()
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            if (!IsAdmin && !IsLecturer)
                return Forbid();

            return null;
        }

        // Kiểm tra giảng viên có được phân công môn học không
        private async Task<bool> CanAccessSubjectAsync(int subjectId)
        {
            if (IsAdmin)
                return await _context.Subjects
                    .AnyAsync(s => s.Id == subjectId);

            int lecturerId = CurrentUserId!.Value;

            return await _context.LecturerSubjects
                .AnyAsync(ls =>
                    ls.LecturerId == lecturerId &&
                    ls.SubjectId == subjectId);
        }

        // Lấy danh sách môn học theo quyền
        private async Task LoadSubjectsAsync(int? selectedSubjectId = null)
        {
            IQueryable<Subject> query = _context.Subjects;

            if (IsLecturer)
            {
                int lecturerId = CurrentUserId!.Value;

                query = query.Where(s =>
                    s.LecturerSubjects.Any(ls =>
                        ls.LecturerId == lecturerId));
            }

            var subjects = await query
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewBag.Subjects = new SelectList(
                subjects,
                "Id",
                "Name",
                selectedSubjectId
            );
        }

        // ==========================================
        // INDEX - DANH SÁCH NGÂN HÀNG CÂU HỎI
        // ==========================================

        public async Task<IActionResult> Index(
            int? subjectId,
            string? search,
            string? difficulty)
        {
            var auth = CheckLogin();
            if (auth != null)
                return auth;

            var query = _context.Questions
                .Include(q => q.Subject)
                .Include(q => q.Options)
                .AsQueryable();

            // Lecturer chỉ xem câu hỏi thuộc môn được phân công
            if (IsLecturer)
            {
                int lecturerId = CurrentUserId!.Value;

                query = query.Where(q =>
                    _context.LecturerSubjects.Any(ls =>
                        ls.LecturerId == lecturerId &&
                        ls.SubjectId == q.SubjectId));
            }

            // Lọc môn học
            if (subjectId.HasValue)
            {
                bool allowed = await CanAccessSubjectAsync(
                    subjectId.Value
                );

                if (!allowed)
                    return Forbid();

                query = query.Where(q =>
                    q.SubjectId == subjectId.Value);
            }

            // Tìm kiếm nội dung hoặc chủ đề
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(q =>
                    q.Content.Contains(search) ||
                    (q.Topic != null && q.Topic.Contains(search)));
            }

            // Lọc độ khó
            if (!string.IsNullOrWhiteSpace(difficulty))
            {
                query = query.Where(q =>
                    q.Difficulty == difficulty);
            }

            var questions = await query
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            await LoadSubjectsAsync(subjectId);

            ViewBag.Search = search;
            ViewBag.Difficulty = difficulty;

            return View(questions);
        }

        // ==========================================
        // CREATE - GET
        // ==========================================

        public async Task<IActionResult> Create()
        {
            var auth = CheckLogin();
            if (auth != null)
                return auth;

            await LoadSubjectsAsync();

            return View(new QuestionFormViewModel
            {
                Difficulty = "Easy",
                Options = new List<QuestionOptionFormModel>
                {
                    new QuestionOptionFormModel(),
                    new QuestionOptionFormModel(),
                    new QuestionOptionFormModel(),
                    new QuestionOptionFormModel()
                }
            });
        }

        // ==========================================
        // CREATE - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            QuestionFormViewModel model)
        {
            var auth = CheckLogin();
            if (auth != null)
                return auth;

            if (!await CanAccessSubjectAsync(model.SubjectId))
                return Forbid();

            // Kiểm tra nội dung đáp án
            var options = model.Options?
                .Where(o => !string.IsNullOrWhiteSpace(o.Content))
                .ToList() ?? new List<QuestionOptionFormModel>();

            if (options.Count < 2)
            {
                ModelState.AddModelError(
                    "Options",
                    "Câu hỏi phải có ít nhất 2 đáp án."
                );
            }

            if (options.Count(o => o.IsCorrect) != 1)
            {
                ModelState.AddModelError(
                    "Options",
                    "Phải chọn chính xác 1 đáp án đúng."
                );
            }

            if (!ModelState.IsValid)
            {
                await LoadSubjectsAsync(model.SubjectId);
                return View(model);
            }

            var question = new Question
            {
                SubjectId = model.SubjectId,
                Topic = model.Topic?.Trim(),
                Content = model.Content.Trim(),
                Difficulty = model.Difficulty,
                CreatedBy = CurrentUserId,
                IsActive = true,
                CreatedAt = DateTime.Now,

                Options = options.Select(o => new QuestionOption
                {
                    Content = o.Content.Trim(),
                    IsCorrect = o.IsCorrect
                }).ToList()
            };

            _context.Questions.Add(question);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm câu hỏi thành công!";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // EDIT - GET
        // ==========================================

        public async Task<IActionResult> Edit(int id)
        {
            var auth = CheckLogin();
            if (auth != null)
                return auth;

            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return NotFound();

            if (!await CanAccessSubjectAsync(question.SubjectId))
                return Forbid();

            var model = new QuestionFormViewModel
            {
                Id = question.Id,
                SubjectId = question.SubjectId,
                Topic = question.Topic,
                Content = question.Content,
                Difficulty = question.Difficulty,

                Options = question.Options
                    .Select(o => new QuestionOptionFormModel
                    {
                        Id = o.Id,
                        Content = o.Content,
                        IsCorrect = o.IsCorrect
                    }).ToList()
            };

            await LoadSubjectsAsync(question.SubjectId);

            return View(model);
        }

        // ==========================================
        // EDIT - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            QuestionFormViewModel model)
        {
            var auth = CheckLogin();
            if (auth != null)
                return auth;

            if (id != model.Id)
                return BadRequest();

            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return NotFound();

            // Kiểm tra quyền đối với môn hiện tại
            if (!await CanAccessSubjectAsync(question.SubjectId))
                return Forbid();

            // Kiểm tra quyền đối với môn mới
            if (!await CanAccessSubjectAsync(model.SubjectId))
                return Forbid();

            var options = model.Options?
                .Where(o => !string.IsNullOrWhiteSpace(o.Content))
                .ToList() ?? new List<QuestionOptionFormModel>();

            if (options.Count < 2)
            {
                ModelState.AddModelError(
                    "Options",
                    "Câu hỏi phải có ít nhất 2 đáp án."
                );
            }

            if (options.Count(o => o.IsCorrect) != 1)
            {
                ModelState.AddModelError(
                    "Options",
                    "Phải chọn chính xác 1 đáp án đúng."
                );
            }

            // Không cho phép bỏ qua đáp án cũ hoặc gửi ID đáp án
            // thuộc câu hỏi khác.
            var existingIds = question.Options
                .Select(o => o.Id)
                .ToHashSet();

            var submittedIds = options
                .Where(o => o.Id > 0)
                .Select(o => o.Id)
                .ToList();

            if (submittedIds.Distinct().Count() != submittedIds.Count ||
                submittedIds.Any(optionId => !existingIds.Contains(optionId)) ||
                existingIds.Any(optionId => !submittedIds.Contains(optionId)))
            {
                ModelState.AddModelError(
                    "Options",
                    "Danh sách đáp án không hợp lệ."
                );
            }

            if (!ModelState.IsValid)
            {
                await LoadSubjectsAsync(model.SubjectId);
                return View(model);
            }

            question.SubjectId = model.SubjectId;
            question.Topic = model.Topic?.Trim();
            question.Content = model.Content.Trim();
            question.Difficulty = model.Difficulty;

            foreach (var optionModel in options)
            {
                if (optionModel.Id > 0)
                {
                    var existingOption = question.Options
                        .First(o => o.Id == optionModel.Id);

                    existingOption.Content = optionModel.Content.Trim();
                    existingOption.IsCorrect = optionModel.IsCorrect;
                }
                else
                {
                    question.Options.Add(new QuestionOption
                    {
                        Content = optionModel.Content.Trim(),
                        IsCorrect = optionModel.IsCorrect
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật câu hỏi thành công!";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // DELETE - GET
        // ==========================================

        public async Task<IActionResult> Delete(int id)
        {
            var auth = CheckLogin();
            if (auth != null)
                return auth;

            var question = await _context.Questions
                .Include(q => q.Subject)
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return NotFound();

            if (!await CanAccessSubjectAsync(question.SubjectId))
                return Forbid();

            return View(question);
        }

        // ==========================================
        // DELETE - POST
        // ==========================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var auth = CheckLogin();
            if (auth != null)
                return auth;

            var question = await _context.Questions
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return NotFound();

            if (!await CanAccessSubjectAsync(question.SubjectId))
                return Forbid();

            // Xóa mềm để không ảnh hưởng đến đề thi,
            // lịch sử làm bài và kết quả đã lưu.
            question.IsActive = false;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã ngừng sử dụng câu hỏi.";

            return RedirectToAction(nameof(Index));
        }
    }

    // ==========================================
    // QUESTION FORM VIEW MODELS
    // ==========================================

    public class QuestionFormViewModel
    {
        public int Id { get; set; }

        public int SubjectId { get; set; }

        public string? Topic { get; set; }

        public string Content { get; set; } = string.Empty;

        public string Difficulty { get; set; } = "Easy";

        public List<QuestionOptionFormModel> Options { get; set; }
            = new List<QuestionOptionFormModel>();
    }

    public class QuestionOptionFormModel
    {
        public int Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }
}