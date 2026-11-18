using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Study.Models;
using Study.ViewModels;

namespace Study.Controllers;

public class StudentController : Controller
{
    private readonly ApplicationDbContext _context;

public StudentController(ApplicationDbContext context)
    {
        _context = context;
    }

    private bool IsStudent()
    {
        return HttpContext.Session.GetString("Role") == "Student";
    }

    private int GetStudentId()
    {
        return HttpContext.Session.GetInt32("UserId") ?? 0;
    }

    // ==============================
    // STUDENT DASHBOARD
    // ==============================

    [HttpGet]
    public async Task<IActionResult> Index()
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

        var exams = await _context.Exams
            .Where(e =>
                e.Status == "Published" &&
                e.Schedule != null &&
                e.Schedule.IsPublished)
            .Include(e => e.Subject)
            .Include(e => e.Schedule)
            .Include(e => e.ExamQuestions)
            .OrderBy(e => e.Schedule!.StartTime)
            .ToListAsync();

        return View(exams);
    }
    [HttpGet]
    public async Task<IActionResult> StartExam(int id)
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

        var exam = await _context.Exams
            .Include(e => e.Subject)
            .Include(e => e.Schedule)
            .FirstOrDefaultAsync(e =>
                e.Id == id &&
                e.Status == "Published" &&
                e.Schedule != null &&
                e.Schedule.IsPublished);

        if (exam == null)
        {
            TempData["Error"] =
                "Kỳ thi không tồn tại hoặc chưa được công bố.";

            return RedirectToAction(nameof(Index));
        }

        var schedule = exam.Schedule!;

        var existingAttempt = await _context.ExamAttempts
            .FirstOrDefaultAsync(x =>
                x.ExamId == exam.Id &&
                x.StudentId == studentId);

        if (existingAttempt != null)
        {
            if (existingAttempt.Status == "Submitted")
            {
                TempData["Error"] =
                    "Bạn đã hoàn thành kỳ thi này.";

                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(
                nameof(TakeExam),
                new { id = existingAttempt.Id });
        }

        var model = new StudentExamKeyViewModel
        {
            ExamId = exam.Id,
            ExamName = exam.Name,
            SubjectName = exam.Subject.Name,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            DurationMinutes = schedule.DurationMinutes
        };

        return View(model);


}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartExam(
    StudentExamKeyViewModel model)
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

        var exam = await _context.Exams
            .Include(e => e.Subject)
            .Include(e => e.Schedule)
            .Include(e => e.ExamQuestions)
            .FirstOrDefaultAsync(e =>
                e.Id == model.ExamId &&
                e.Status == "Published" &&
                e.Schedule != null &&
                e.Schedule.IsPublished);

        if (exam == null)
        {
            TempData["Error"] =
                "Kỳ thi không tồn tại hoặc chưa được công bố.";

            return RedirectToAction(nameof(Index));
        }

        var schedule = exam.Schedule!;

        // Kiểm tra Exam Key
        if (string.IsNullOrWhiteSpace(model.ExamKey) ||
            !string.Equals(
                model.ExamKey.Trim(),
                schedule.ExamKey,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.ExamKey),
                "Exam Key không chính xác.");

            model.ExamName = exam.Name;
            model.SubjectName = exam.Subject.Name;
            model.StartTime = schedule.StartTime;
            model.EndTime = schedule.EndTime;
            model.DurationMinutes = schedule.DurationMinutes;

            return View(model);
        }

        DateTime now = DateTime.Now;

        // Chỉ được bắt đầu từ StartTime
        if (now < schedule.StartTime)
        {
            TempData["Error"] =
                $"Kỳ thi chưa bắt đầu. Thời gian bắt đầu: " +
                $"{schedule.StartTime:dd/MM/yyyy HH:mm}.";

            return RedirectToAction(nameof(Index));
        }

        // StartTime + GraceMinutes
        DateTime latestStartTime =
            schedule.StartTime.AddMinutes(
                schedule.GraceMinutes);

        // Quá thời gian Grace
        if (now > latestStartTime)
        {
            TempData["Error"] =
                "Bạn đã quá thời gian cho phép bắt đầu kỳ thi.";

            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra đã có attempt chưa
        var existingAttempt = await _context.ExamAttempts
            .FirstOrDefaultAsync(x =>
                x.ExamId == exam.Id &&
                x.StudentId == studentId);

        if (existingAttempt != null)
        {
            if (existingAttempt.Status == "Submitted")
            {
                TempData["Error"] =
                    "Bạn đã hoàn thành kỳ thi này.";

                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(
                nameof(TakeExam),
                new { id = existingAttempt.Id });
        }

        // Phải có câu hỏi
        if (!exam.ExamQuestions.Any())
        {
            TempData["Error"] =
                "Kỳ thi chưa có câu hỏi.";

            return RedirectToAction(nameof(Index));
        }

        DateTime actualStartTime = now;

        DateTime forcedSubmitTime =
            actualStartTime.AddMinutes(
                schedule.DurationMinutes);

        var attempt = new ExamAttempt
        {
            ExamId = exam.Id,
            StudentId = studentId,
            ActualStartTime = actualStartTime,
            ForcedSubmitTime = forcedSubmitTime,
            Status = "InProgress"
        };

        _context.ExamAttempts.Add(attempt);

        await _context.SaveChangesAsync();

        return RedirectToAction(
            nameof(TakeExam),
            new { id = attempt.Id });


    }
   
[HttpGet]
public async Task<IActionResult> TakeExam(int id, int page = 1)
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

        var attempt = await _context.ExamAttempts
            .Include(x => x.Exam)
                .ThenInclude(x => x.Subject)
            .Include(x => x.Exam)
                .ThenInclude(x => x.Schedule)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.StudentId == studentId);

        if (attempt == null)
        {
            return NotFound();
        }

        if (attempt.Status == "Submitted")
        {
            TempData["Error"] = "Bạn đã hoàn thành kỳ thi này.";

            return RedirectToAction(nameof(Index));
        }

        if (attempt.Exam.Schedule == null ||
            !attempt.Exam.Schedule.IsPublished)
        {
            TempData["Error"] = "Kỳ thi chưa được công bố.";

            return RedirectToAction(nameof(Index));
        }

        /*
         * Kiểm tra thời gian.
         * Nếu đã quá ForcedSubmitTime thì tự động nộp.
         */
        if (DateTime.Now >= attempt.ForcedSubmitTime)
        {
            await SubmitExamInternal(attempt);

            TempData["Success"] =
                "Thời gian làm bài đã hết. Bài thi đã được tự động nộp.";

            return RedirectToAction(
                nameof(Result),
                new { id = attempt.Id });
        }

        /*
         * Nếu chưa có danh sách câu hỏi cho Attempt
         * thì tạo random order một lần.
         */
        var attemptQuestionsCount =
            await _context.ExamAttemptQuestions
                .CountAsync(x => x.AttemptId == attempt.Id);

        if (attemptQuestionsCount == 0)
        {
            var examQuestions = await _context.ExamQuestions
                .Where(x => x.ExamId == attempt.ExamId)
                .OrderBy(x => Guid.NewGuid())
                .ToListAsync();

            int questionOrder = 1;

            foreach (var examQuestion in examQuestions)
            {
                _context.ExamAttemptQuestions.Add(
                    new ExamAttemptQuestion
                    {
                        AttemptId = attempt.Id,
                        QuestionId = examQuestion.QuestionId,
                        QuestionOrder = questionOrder
                    });

                questionOrder++;
            }

            await _context.SaveChangesAsync();
        }

        /*
         * Nếu chưa có thứ tự đáp án thì random một lần.
         */
        var attemptQuestionList =
            await _context.ExamAttemptQuestions
                .Where(x => x.AttemptId == attempt.Id)
                .OrderBy(x => x.QuestionOrder)
                .ToListAsync();

        foreach (var attemptQuestion in attemptQuestionList)
        {
            bool hasOptions =
                await _context.ExamAttemptOptions
                    .AnyAsync(x =>
                        x.AttemptId == attempt.Id &&
                        x.QuestionId == attemptQuestion.QuestionId);

            if (hasOptions)
            {
                continue;
            }

            var options = await _context.QuestionOptions
                .Where(x =>
                    x.QuestionId == attemptQuestion.QuestionId)
                .OrderBy(x => Guid.NewGuid())
                .ToListAsync();

            int displayOrder = 1;

            foreach (var option in options)
            {
                _context.ExamAttemptOptions.Add(
                    new ExamAttemptOption
                    {
                        AttemptId = attempt.Id,
                        QuestionId = attemptQuestion.QuestionId,
                        OptionId = option.Id,
                        DisplayOrder = displayOrder
                    });

                displayOrder++;
            }
        }

        await _context.SaveChangesAsync();

        /*
         * Giới hạn 10 câu / trang.
         */
        int totalQuestions =
            attemptQuestionList.Count;

        int totalPages =
            (int)Math.Ceiling(totalQuestions / 10.0);

        if (page < 1)
        {
            page = 1;
        }

        if (page > totalPages)
        {
            page = totalPages;
        }

        var pageQuestions =
            attemptQuestionList
                .Skip((page - 1) * 10)
                .Take(10)
                .ToList();

        var questionIds =
            pageQuestions
                .Select(x => x.QuestionId)
                .ToList();

        var questions = await _context.Questions
            .Where(x => questionIds.Contains(x.Id))
            .ToListAsync();

        var attemptOptions =
            await _context.ExamAttemptOptions
                .Where(x =>
                    x.AttemptId == attempt.Id &&
                    questionIds.Contains(x.QuestionId))
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

        var optionIds =
            attemptOptions
                .Select(x => x.OptionId)
                .ToList();

        var optionsData =
            await _context.QuestionOptions
                .Where(x => optionIds.Contains(x.Id))
                .ToListAsync();

        var answers =
            await _context.StudentAnswers
                .Where(x =>
                    x.AttemptId == attempt.Id &&
                    questionIds.Contains(x.QuestionId))
                .ToListAsync();

        var model = new StudentTakeExamViewModel
        {
            AttemptId = attempt.Id,
            ExamId = attempt.ExamId,
            ExamName = attempt.Exam.Name,
            SubjectName = attempt.Exam.Subject.Name,
            ActualStartTime = attempt.ActualStartTime,
            ForcedSubmitTime = attempt.ForcedSubmitTime,
            TotalQuestions = totalQuestions,
            CurrentPage = page,
            TotalPages = totalPages
        };

        foreach (var attemptQuestion in pageQuestions)
        {
            var question =
                questions.First(x =>
                    x.Id == attemptQuestion.QuestionId);

            var answer =
                answers.FirstOrDefault(x =>
                    x.QuestionId == question.Id);

            var questionModel =
                new StudentTakeQuestionViewModel
                {
                    QuestionId = question.Id,
                    QuestionOrder =
                        attemptQuestion.QuestionOrder,
                    Content = question.Content,
                    Topic = question.Topic,
                    Difficulty = question.Difficulty,
                    SelectedOptionId =
                        answer?.SelectedOptionId,
                    IsMarked =
                        answer?.IsMarked ?? false
                };

            var questionOptions =
                attemptOptions
                    .Where(x =>
                        x.QuestionId == question.Id)
                    .OrderBy(x => x.DisplayOrder)
                    .ToList();

            foreach (var attemptOption in questionOptions)
            {
                var option =
                    optionsData.First(x =>
                        x.Id == attemptOption.OptionId);

                questionModel.Options.Add(
                    new StudentTakeOptionViewModel
                    {
                        OptionId = option.Id,
                        Content = option.Content,
                        DisplayOrder =
                            attemptOption.DisplayOrder
                    });
            }

            model.Questions.Add(questionModel);
        }

        return View(model);
    }


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

        var attempt = await _context.ExamAttempts
            .FirstOrDefaultAsync(x =>
                x.Id == attemptId &&
                x.StudentId == studentId);

        if (attempt == null)
        {
            return NotFound();
        }

        if (attempt.Status == "Submitted")
        {
            return RedirectToAction(
                nameof(Result),
                new { id = attempt.Id });
        }

        if (DateTime.Now >= attempt.ForcedSubmitTime)
        {
            await SubmitExamInternal(attempt);

            return RedirectToAction(
                nameof(Result),
                new { id = attempt.Id });
        }

        var answer = await _context.StudentAnswers
            .FirstOrDefaultAsync(x =>
                x.AttemptId == attemptId &&
                x.QuestionId == questionId);

        if (answer == null)
        {
            answer = new StudentAnswer
            {
                AttemptId = attemptId,
                QuestionId = questionId,
                SelectedOptionId = selectedOptionId,
                IsMarked = isMarked,
                AnsweredAt =
                    selectedOptionId.HasValue
                        ? DateTime.Now
                        : null
            };

            _context.StudentAnswers.Add(answer);
        }
        else
        {
            answer.SelectedOptionId = selectedOptionId;
            answer.IsMarked = isMarked;

            if (selectedOptionId.HasValue)
            {
                answer.AnsweredAt = DateTime.Now;
            }
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(
            nameof(TakeExam),
            new
            {
                id = attemptId,
                page
            });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitExam(int id)
    {
        if (!IsStudent())
        {
            return RedirectToAction("Login", "Account");
        }

        int studentId = GetStudentId();

        var attempt = await _context.ExamAttempts
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.StudentId == studentId);

        if (attempt == null)
        {
            return NotFound();
        }

        if (attempt.Status == "Submitted")
        {
            return RedirectToAction(
                nameof(Result),
                new { id });
        }

        await SubmitExamInternal(attempt);

        return RedirectToAction(
            nameof(Result),
            new { id });
    }


    private async Task SubmitExamInternal(
        ExamAttempt attempt)
    {
        if (attempt.Status == "Submitted")
        {
            return;
        }

        var attemptQuestions =
            await _context.ExamAttemptQuestions
                .Where(x => x.AttemptId == attempt.Id)
                .ToListAsync();

        var answers =
            await _context.StudentAnswers
                .Where(x => x.AttemptId == attempt.Id)
                .ToListAsync();

        int totalQuestions =
            attemptQuestions.Count;

        int correct = 0;
        int wrong = 0;
        int unanswered = 0;

        var optionIds =
            answers
                .Where(x => x.SelectedOptionId.HasValue)
                .Select(x => x.SelectedOptionId!.Value)
                .ToList();

        var options =
            await _context.QuestionOptions
                .Where(x => optionIds.Contains(x.Id))
                .ToListAsync();

        foreach (var question in attemptQuestions)
        {
            var answer =
                answers.FirstOrDefault(x =>
                    x.QuestionId == question.QuestionId);

            if (answer == null ||
                !answer.SelectedOptionId.HasValue)
            {
                unanswered++;
                continue;
            }

            var selectedOption =
                options.FirstOrDefault(x =>
                    x.Id == answer.SelectedOptionId.Value);

            if (selectedOption == null)
            {
                wrong++;
                continue;
            }

            if (selectedOption.IsCorrect)
            {
                correct++;
            }
            else
            {
                wrong++;
            }
        }

        decimal score =
            totalQuestions > 0
                ? Math.Round(
                    (decimal)correct /
                    totalQuestions * 10,
                    2)
                : 0;

        attempt.Status = "Submitted";
        attempt.SubmittedAt = DateTime.Now;

        var existingResult =
            await _context.ExamResults
                .FirstOrDefaultAsync(x =>
                    x.AttemptId == attempt.Id);

        if (existingResult == null)
        {
            _context.ExamResults.Add(
                new ExamResult
                {
                    AttemptId = attempt.Id,
                    TotalQuestions = totalQuestions,
                    CorrectAnswers = correct,
                    WrongAnswers = wrong,
                    Unanswered = unanswered,
                    Score = score,
                    GradedAt = DateTime.Now
                });
        }
        else
        {
            existingResult.TotalQuestions = totalQuestions;
            existingResult.CorrectAnswers = correct;
            existingResult.WrongAnswers = wrong;
            existingResult.Unanswered = unanswered;
            existingResult.Score = score;
            existingResult.GradedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }




 
// ==============================
// XEM KẾT QUẢ THI
// ==============================
[HttpGet]
public async Task<IActionResult> Result(int id)
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

        var attempt = await _context.ExamAttempts
            .Include(x => x.Exam)
                .ThenInclude(x => x.Subject)
            .Include(x => x.Result)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.StudentId == studentId);

        if (attempt == null)
        {
            return NotFound();
        }

        // Chỉ cho xem kết quả sau khi đã nộp bài
        if (attempt.Status != "Submitted")
        {
            TempData["Error"] =
                "Bạn chưa hoàn thành kỳ thi này.";

            return RedirectToAction(
                nameof(TakeExam),
                new { id = attempt.Id });
        }

        // Nếu chưa có kết quả
        if (attempt.Result == null)
        {
            TempData["Error"] =
                "Kết quả thi chưa được tạo.";

            return RedirectToAction(nameof(Index));
        }

        return View(attempt);
    }



}
