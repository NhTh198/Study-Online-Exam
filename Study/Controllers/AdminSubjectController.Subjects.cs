using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Study.Models;
using Study.ViewModels;

namespace Study.Controllers;

public partial class AdminSubjectController
{
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
}
