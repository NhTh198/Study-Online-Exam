<<<<<<< Updated upstream
﻿
=======

>>>>>>> Stashed changes
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Study.Models;
using Study.ViewModels;

namespace Study.Controllers;

public class PracticeController : Controller
{
    private readonly ApplicationDbContext _context;

    private const int QuestionsPerPractice = 40;
    private const int PracticeDurationMinutes = 60;
    private const int QuestionsPerPage = 10;

    public PracticeController(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // KIỂM TRA STUDENT
    // =========================================================

    private bool IsStudent()
    {
        return HttpContext.Session.GetString("Role") == "Student";
    }

    private int GetStudentId()
    {
        return HttpContext.Session.GetInt32("UserId") ?? 0;
    }

    // =========================================================
    // TRANG CHỌN MÔN
    // GET: /Practice
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsStudent())
        {
            return RedirectToAction("Login", "Account");
        }

        var subjects = await _context.Subjects
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new PracticeSubjectViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,

                QuestionCount = _context.Questions
                    .Count(q =>
                        q.SubjectId == s.Id &&
                        q.IsActive)
            })
            .ToListAsync();

        var model = new PracticeStartViewModel
        {
            Subjects = subjects
        };

        return View(model);
    }

    // =========================================================
    // TẠO ATTEMPT MỚI
    // Dùng chung cho Start và Retry
    // =========================================================

    private async Task<int?> CreatePracticeAttempt(
        int studentId,
        int subjectId)
    {
        // -----------------------------------------------------
        // Kiểm tra môn học
        // -----------------------------------------------------

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s =>
                s.Id == subjectId &&
                s.IsActive);

        if (subject == null)
        {
            TempData["Error"] =
                "Môn học không tồn tại hoặc đã bị khóa.";

            return null;
        }

        // -----------------------------------------------------
        // Lấy câu hỏi đang hoạt động
        // -----------------------------------------------------

        var questionIds = await _context.Questions
            .Where(q =>
                q.SubjectId == subjectId &&
                q.IsActive)
            .Select(q => q.Id)
            .ToListAsync();

        // -----------------------------------------------------
        // Phải có ít nhất 40 câu
        // -----------------------------------------------------

        if (questionIds.Count < QuestionsPerPractice)
        {
            TempData["Error"] =
                $"Môn {subject.Name} hiện chỉ có " +
                $"{questionIds.Count} câu hỏi. " +
                $"Cần ít nhất {QuestionsPerPractice} câu để bắt đầu.";

            return null;
        }

        // -----------------------------------------------------
        // Random 40 câu
        // -----------------------------------------------------

        var selectedQuestionIds = questionIds
            .OrderBy(x => Guid.NewGuid())
            .Take(QuestionsPerPractice)
            .ToList();

        // -----------------------------------------------------
        // Tạo Attempt
        // -----------------------------------------------------

        DateTime now = DateTime.Now;

        var attempt = new PracticeAttempt
        {
            StudentId = studentId,
            SubjectId = subjectId,
            StartedAt = now,
            ForcedSubmitTime =
                now.AddMinutes(PracticeDurationMinutes),
            Status = "InProgress"
        };

        _context.PracticeAttempts.Add(attempt);

        await _context.SaveChangesAsync();

        // -----------------------------------------------------
        // Lưu 40 câu + thứ tự đáp án
        // -----------------------------------------------------

        var attemptQuestions =
            new List<PracticeAttemptQuestion>();

        foreach (var questionId in selectedQuestionIds)
        {
            var optionIds = await _context.QuestionOptions
                .Where(o => o.QuestionId == questionId)
                .Select(o => o.Id)
                .ToListAsync();

            var randomOptionIds = optionIds
                .OrderBy(x => Guid.NewGuid())
                .ToList();

            string optionOrder =
                string.Join(",", randomOptionIds);

            attemptQuestions.Add(
                new PracticeAttemptQuestion
                {
                    AttemptId = attempt.Id,

                    QuestionId = questionId,

                    QuestionOrder =
                        attemptQuestions.Count + 1,

                    OptionOrder =
                        optionOrder
                });
        }

        _context.PracticeAttemptQuestions.AddRange(
            attemptQuestions);

        await _context.SaveChangesAsync();

        return attempt.Id;
    }

    // =========================================================
    // BẮT ĐẦU LUYỆN
    // POST: /Practice/Start
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(
        PracticeStartViewModel model)
    {
        if (!IsStudent())
        {
            return RedirectToAction("Login", "Account");
        }

        int studentId = GetStudentId();

        if (studentId == 0)
        {
            return RedirectToAction("Login", "Account");
        }

        if (model.SubjectId <= 0)
        {
            TempData["Error"] =
                "Vui lòng chọn môn học.";

            return RedirectToAction(nameof(Index));
        }

        // -----------------------------------------------------
        // Nếu Student đang có bài InProgress chưa hết giờ
        // thì tiếp tục bài đó
        // -----------------------------------------------------

        var existingAttempt =
            await _context.PracticeAttempts
                .FirstOrDefaultAsync(x =>
                    x.StudentId == studentId &&
                    x.Status == "InProgress" &&
                    x.ForcedSubmitTime > DateTime.Now);

        if (existingAttempt != null)
        {
            return RedirectToAction(
                nameof(Take),
                new
                {
                    id = existingAttempt.Id
                });
        }

        // -----------------------------------------------------
        // Tạo bài mới
        // -----------------------------------------------------

        int? attemptId =
            await CreatePracticeAttempt(
                studentId,
                model.SubjectId);

        if (!attemptId.HasValue)
        {
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(
            nameof(Take),
            new
            {
                id = attemptId.Value
            });
    }

    // =========================================================
    // LÀM BÀI
    // GET: /Practice/Take/5
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Take(
        int id,
        int page = 1)
    {
        if (!IsStudent())
        {
            return RedirectToAction("Login", "Account");
        }

        int studentId = GetStudentId();

        if (studentId == 0)
        {
            return RedirectToAction("Login", "Account");
        }

        // -----------------------------------------------------
        // Lấy Attempt của chính Student
        // -----------------------------------------------------

        var attempt = await _context.PracticeAttempts

            .Include(x => x.Subject)

            .Include(x => x.PracticeAttemptQuestions)
                .ThenInclude(x => x.Question)
                    .ThenInclude(x => x.Options)

            .Include(x => x.PracticeAnswers)

            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.StudentId == studentId);

        if (attempt == null)
        {
            return NotFound();
        }

        // -----------------------------------------------------
        // Nếu đã nộp → chuyển Result
        // -----------------------------------------------------

        if (attempt.Status == "Submitted")
        {
            return RedirectToAction(
                nameof(Result),
                new
                {
                    id = attempt.Id
                });
        }

        // -----------------------------------------------------
        // Hết giờ → tự động nộp
        // -----------------------------------------------------

        if (DateTime.Now >= attempt.ForcedSubmitTime)
        {
            await SubmitPracticeInternal(attempt);

            return RedirectToAction(
                nameof(Result),
                new
                {
                    id = attempt.Id
                });
        }

        // -----------------------------------------------------
        // Sắp xếp câu theo QuestionOrder
        // -----------------------------------------------------

        var attemptQuestions =
            attempt.PracticeAttemptQuestions
                .OrderBy(x => x.QuestionOrder)
                .ToList();

        int totalQuestions =
            attemptQuestions.Count;

        int totalPages =
            (int)Math.Ceiling(
                totalQuestions /
                (double)QuestionsPerPage);

        // -----------------------------------------------------
        // Kiểm tra page
        // -----------------------------------------------------

        if (page < 1)
        {
            page = 1;
        }

        if (page > totalPages)
        {
            page = totalPages;
        }

        // -----------------------------------------------------
        // Lấy 10 câu của trang
        // -----------------------------------------------------

        var pageQuestions =
            attemptQuestions
                .Skip(
                    (page - 1) *
                    QuestionsPerPage)
                .Take(QuestionsPerPage)
                .ToList();

        // -----------------------------------------------------
        // ViewModel
        // -----------------------------------------------------

        var model = new PracticeTakeViewModel
        {
            AttemptId = attempt.Id,

            SubjectName =
                attempt.Subject.Name,

            StartedAt =
                attempt.StartedAt,

            ForcedSubmitTime =
                attempt.ForcedSubmitTime,

            TotalQuestions =
                totalQuestions,

            CurrentPage =
                page,

            TotalPages =
                totalPages
        };

        // -----------------------------------------------------
        // Tạo Question ViewModel
        // -----------------------------------------------------

        foreach (var item in pageQuestions)
        {
            var savedAnswer =
                attempt.PracticeAnswers
                    .FirstOrDefault(x =>
                        x.QuestionId ==
                        item.QuestionId);

            // -------------------------------------------------
            // Lấy thứ tự đáp án đã lưu
            // -------------------------------------------------

            var optionOrderIds =
                string.IsNullOrWhiteSpace(
                    item.OptionOrder)

                    ? new List<int>()

                    : item.OptionOrder
                        .Split(
                            ',',
                            StringSplitOptions
                                .RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToList();

            // -------------------------------------------------
            // Dictionary đáp án
            // -------------------------------------------------

            var optionDictionary =
                item.Question.Options
                    .ToDictionary(x => x.Id);

            var options =
                new List<PracticeOptionViewModel>();

            // -------------------------------------------------
            // Dùng thứ tự đã lưu
            // -------------------------------------------------

            if (optionOrderIds.Any())
            {
                for (
                    int i = 0;
                    i < optionOrderIds.Count;
                    i++)
                {
                    int optionId =
                        optionOrderIds[i];

                    if (!optionDictionary.TryGetValue(
                            optionId,
                            out var option))
                    {
                        continue;
                    }

                    options.Add(
                        new PracticeOptionViewModel
                        {
                            OptionId =
                                option.Id,

                            Content =
                                option.Content,

                            DisplayOrder =
                                i + 1
                        });
                }
            }
            else
            {
                // -------------------------------------------------
                // Dữ liệu cũ chưa có OptionOrder
                // -------------------------------------------------

                options =
                    item.Question.Options
                        .OrderBy(
                            x => Guid.NewGuid())
                        .Select(
                            (option, index) =>
                                new PracticeOptionViewModel
                                {
                                    OptionId =
                                        option.Id,

                                    Content =
                                        option.Content,

                                    DisplayOrder =
                                        index + 1
                                })
                        .ToList();
            }

            // -------------------------------------------------
            // Thêm câu hỏi vào ViewModel
            // -------------------------------------------------

            model.Questions.Add(
                new PracticeQuestionViewModel
                {
                    QuestionId =
                        item.QuestionId,

                    QuestionOrder =
                        item.QuestionOrder,

                    Content =
                        item.Question.Content,

                    Topic =
                        item.Question.Topic,

                    Difficulty =
                        item.Question.Difficulty,

                    SelectedOptionId =
                        savedAnswer?.SelectedOptionId,

                    IsMarked =
                        savedAnswer?.IsMarked ?? false,

                    Options =
                        options
                });
        }

        return View(model);
    }

    // =========================================================
    // LƯU ĐÁP ÁN
    // POST: /Practice/SaveAnswer
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAnswer(
        int attemptId,
        int questionId,
        int? selectedOptionId,
        bool isMarked,
        int page = 1)
    {
        if (!IsStudent())
        {
            return RedirectToAction("Login", "Account");
        }

        int studentId = GetStudentId();

        var attempt =
            await _context.PracticeAttempts
                .FirstOrDefaultAsync(x =>
                    x.Id == attemptId &&
                    x.StudentId == studentId);

        if (attempt == null)
        {
            return NotFound();
        }

        // -----------------------------------------------------
        // Nếu đã nộp
        // -----------------------------------------------------

        if (attempt.Status == "Submitted")
        {
            return RedirectToAction(
                nameof(Result),
                new
                {
                    id = attemptId
                });
        }

        // -----------------------------------------------------
        // Nếu hết giờ
        // -----------------------------------------------------

        if (DateTime.Now >=
            attempt.ForcedSubmitTime)
        {
            await SubmitPracticeInternal(attempt);

            return RedirectToAction(
                nameof(Result),
                new
                {
                    id = attemptId
                });
        }

        // -----------------------------------------------------
        // Kiểm tra question thuộc attempt
        // -----------------------------------------------------

        bool questionExists =
            await _context.PracticeAttemptQuestions
                .AnyAsync(x =>
                    x.AttemptId == attemptId &&
                    x.QuestionId == questionId);

        if (!questionExists)
        {
            return BadRequest();
        }

        // -----------------------------------------------------
        // Kiểm tra option thuộc question
        // -----------------------------------------------------

        if (selectedOptionId.HasValue)
        {
            bool optionExists =
                await _context.QuestionOptions
                    .AnyAsync(x =>
                        x.Id ==
                        selectedOptionId.Value &&

                        x.QuestionId ==
                        questionId);

            if (!optionExists)
            {
                return BadRequest();
            }
        }

        // -----------------------------------------------------
        // Tìm answer
        // -----------------------------------------------------

        var answer =
            await _context.PracticeAnswers
                .FirstOrDefaultAsync(x =>
                    x.AttemptId == attemptId &&
                    x.QuestionId == questionId);

        if (answer == null)
        {
            answer = new PracticeAnswer
            {
                AttemptId =
                    attemptId,

                QuestionId =
                    questionId,

                SelectedOptionId =
                    selectedOptionId,

                IsMarked =
                    isMarked,

                AnsweredAt =
                    selectedOptionId.HasValue
                        ? DateTime.Now
                        : null
            };

            _context.PracticeAnswers.Add(answer);
        }
        else
        {
            answer.SelectedOptionId =
                selectedOptionId;

            answer.IsMarked =
                isMarked;

            if (selectedOptionId.HasValue)
            {
                answer.AnsweredAt =
                    DateTime.Now;
            }
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(
            nameof(Take),
            new
            {
                id = attemptId,
                page = page
            });
    }

    // =========================================================
    // NỘP BÀI
    // POST: /Practice/Submit
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        if (!IsStudent())
        {
            return RedirectToAction("Login", "Account");
        }

        int studentId = GetStudentId();

        var attempt =
            await _context.PracticeAttempts
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.StudentId == studentId);

        if (attempt == null)
        {
            return NotFound();
        }

        // -----------------------------------------------------
        // Nếu đã nộp rồi
        // -----------------------------------------------------

        if (attempt.Status == "Submitted")
        {
            return RedirectToAction(
                nameof(Result),
                new
                {
                    id = attempt.Id
                });
        }

        // -----------------------------------------------------
        // Chấm bài
        // -----------------------------------------------------

        await SubmitPracticeInternal(attempt);

        // -----------------------------------------------------
        // Sang trang Result
        // -----------------------------------------------------

        return RedirectToAction(
            nameof(Result),
            new
            {
                id = attempt.Id
            });
    }

    // =========================================================
    // CHẤM BÀI
    // =========================================================

    private async Task SubmitPracticeInternal(
        PracticeAttempt attempt)
    {
        if (attempt.Status == "Submitted")
        {
            return;
        }

        // -----------------------------------------------------
        // Lấy câu hỏi
        // -----------------------------------------------------

        var attemptQuestions =
            await _context.PracticeAttemptQuestions
                .Where(x =>
                    x.AttemptId ==
                    attempt.Id)

                .Include(x => x.Question)
                    .ThenInclude(x => x.Options)

                .ToListAsync();

        // -----------------------------------------------------
        // Lấy đáp án Student
        // -----------------------------------------------------

        var answers =
            await _context.PracticeAnswers
                .Where(x =>
                    x.AttemptId ==
                    attempt.Id)
                .ToListAsync();

        int totalQuestions =
            attemptQuestions.Count;

        int correct = 0;

        int wrong = 0;

        int unanswered = 0;

        // -----------------------------------------------------
        // Chấm từng câu
        // -----------------------------------------------------

        foreach (var question in attemptQuestions)
        {
            var answer =
                answers.FirstOrDefault(x =>
                    x.QuestionId ==
                    question.QuestionId);

            // Chưa trả lời
            if (answer == null ||
                !answer.SelectedOptionId.HasValue)
            {
                unanswered++;

                continue;
            }

            // Tìm đáp án đúng
            var correctOption =
                question.Question.Options
                    .FirstOrDefault(x =>
                        x.IsCorrect);

            // Đúng
            if (correctOption != null &&
                answer.SelectedOptionId.Value ==
                correctOption.Id)
            {
                correct++;
            }
            // Sai
            else
            {
                wrong++;
            }
        }

        // -----------------------------------------------------
        // Tính điểm /10
        // -----------------------------------------------------

        decimal score =
            totalQuestions > 0

                ? Math.Round(
                    (decimal)correct /
                    totalQuestions *
                    10,
                    2)

                : 0;

        // -----------------------------------------------------
        // Cập nhật Attempt
        // -----------------------------------------------------

        attempt.Status =
            "Submitted";

        attempt.SubmittedAt =
            DateTime.Now;

        // -----------------------------------------------------
        // Tìm Result hiện tại
        // -----------------------------------------------------

        var result =
            await _context.PracticeResults
                .FirstOrDefaultAsync(x =>
                    x.AttemptId ==
                    attempt.Id);

        // -----------------------------------------------------
        // Tạo Result
        // -----------------------------------------------------

        if (result == null)
        {
            result = new PracticeResult
            {
                AttemptId =
                    attempt.Id,

                TotalQuestions =
                    totalQuestions,

                CorrectAnswers =
                    correct,

                WrongAnswers =
                    wrong,

                Unanswered =
                    unanswered,

                Score =
                    score,

                GradedAt =
                    DateTime.Now
            };

            _context.PracticeResults.Add(result);
        }
        else
        {
            result.TotalQuestions =
                totalQuestions;

            result.CorrectAnswers =
                correct;

            result.WrongAnswers =
                wrong;

            result.Unanswered =
                unanswered;

            result.Score =
                score;

            result.GradedAt =
                DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    // =========================================================
    // KẾT QUẢ
    // GET: /Practice/Result/5
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Result(int id)
    {
        if (!IsStudent())
        {
            return RedirectToAction("Login", "Account");
        }

        int studentId = GetStudentId();

        // -----------------------------------------------------
        // QUAN TRỌNG:
        // Lấy PracticeResult
        // Include Attempt
        // Include Subject
        // -----------------------------------------------------

        var result =
            await _context.PracticeResults

                .Include(x => x.Attempt)
                    .ThenInclude(x => x.Subject)

                .FirstOrDefaultAsync(x =>
                    x.AttemptId == id &&
                    x.Attempt.StudentId == studentId);

        if (result == null)
        {
            return NotFound();
        }

        // -----------------------------------------------------
        // Chưa nộp thì quay lại làm
        // -----------------------------------------------------

        if (result.Attempt.Status != "Submitted")
        {
            return RedirectToAction(
                nameof(Take),
                new
                {
                    id = id
                });
        }

<<<<<<< Updated upstream
        // -----------------------------------------------------
        // ĐÚNG:
        // View Result.cshtml cần PracticeResult
        // -----------------------------------------------------

=======
        // Tạo phần ôn tập câu sai/chưa trả lời cho đúng lần luyện hiện tại.
        var attemptQuestions = await _context.PracticeAttemptQuestions
            .Where(x => x.AttemptId == id)
            .Include(x => x.Question).ThenInclude(q => q.Options)
            .OrderBy(x => x.QuestionOrder).ToListAsync();
        var answers = await _context.PracticeAnswers
            .Where(x => x.AttemptId == id)
            .Include(x => x.SelectedOption).ToListAsync();
        var wrongQuestions = new List<PracticeWrongQuestionViewModel>();
        foreach (var item in attemptQuestions)
        {
            var answer = answers.FirstOrDefault(a => a.QuestionId == item.QuestionId);
            var correct = item.Question.Options.FirstOrDefault(o => o.IsCorrect);
            if (correct == null || (answer?.SelectedOptionId == correct.Id)) continue;
            wrongQuestions.Add(new PracticeWrongQuestionViewModel
            {
                QuestionOrder = item.QuestionOrder,
                Content = item.Question.Content,
                SelectedAnswer = answer?.SelectedOption?.Content,
                CorrectAnswer = correct.Content,
                Options = item.Question.Options.Select(o => o.Content).ToList()
            });
        }
        ViewBag.WrongQuestions = wrongQuestions;
>>>>>>> Stashed changes
        return View(result);
    }

    // =========================================================
    // LUYỆN LẠI
    // POST: /Practice/Retry
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(
        int subjectId)
    {
        if (!IsStudent())
        {
            return RedirectToAction("Login", "Account");
        }

        int studentId = GetStudentId();

        if (studentId == 0)
        {
            return RedirectToAction("Login", "Account");
        }

        // -----------------------------------------------------
        // Nếu đang có bài khác chưa hoàn thành
        // -----------------------------------------------------

        var existingAttempt =
            await _context.PracticeAttempts
                .FirstOrDefaultAsync(x =>
                    x.StudentId == studentId &&
                    x.Status == "InProgress" &&
                    x.ForcedSubmitTime > DateTime.Now);

        if (existingAttempt != null)
        {
            return RedirectToAction(
                nameof(Take),
                new
                {
                    id = existingAttempt.Id
                });
        }

        // -----------------------------------------------------
        // Tạo một attempt hoàn toàn mới
        // -----------------------------------------------------

        int? attemptId =
            await CreatePracticeAttempt(
                studentId,
                subjectId);

        if (!attemptId.HasValue)
        {
            return RedirectToAction(nameof(Index));
        }

        // -----------------------------------------------------
        // Sang bài luyện mới
        // -----------------------------------------------------

        return RedirectToAction(
            nameof(Take),
            new
            {
                id = attemptId.Value
            });
    }
// =========================================================
// LỊCH SỬ TỰ LUYỆN
// GET: /Practice/History
// =========================================================

[HttpGet]
public async Task<IActionResult> History()
    {
        if (!IsStudent())
        {
            return RedirectToAction("Login", "Account");
        }

        int studentId = GetStudentId();

        if (studentId == 0)
        {
            return RedirectToAction("Login", "Account");
        }

        var history = await _context.PracticeResults
            .Include(x => x.Attempt)
                .ThenInclude(x => x.Subject)

            .Where(x =>
                x.Attempt.StudentId == studentId &&
                x.Attempt.Status == "Submitted")

            .OrderByDescending(x =>
                x.Attempt.SubmittedAt)

            .Select(x => new PracticeHistoryViewModel
            {
                AttemptId = x.AttemptId,

                SubjectName =
                    x.Attempt.Subject.Name,

                StartedAt =
                    x.Attempt.StartedAt,

                SubmittedAt =
                    x.Attempt.SubmittedAt,

                TotalQuestions =
                    x.TotalQuestions,

                CorrectAnswers =
                    x.CorrectAnswers,

                WrongAnswers =
                    x.WrongAnswers,

                Unanswered =
                    x.Unanswered,

                Score =
                    x.Score
            })

            .ToListAsync();

        return View(history);
    }

<<<<<<< Updated upstream
=======



>>>>>>> Stashed changes
}
