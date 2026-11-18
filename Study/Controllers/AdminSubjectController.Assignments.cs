using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Study.Models;
using Study.ViewModels;

namespace Study.Controllers;

public partial class AdminSubjectController
{
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
}
