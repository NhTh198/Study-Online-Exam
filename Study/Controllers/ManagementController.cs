using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Study.Models;
using Study.Services;
using System.Globalization;
using System.Text;

namespace Study.Controllers;

public class ManagementController : Controller
{
    private readonly ApplicationDbContext _db;
    public ManagementController(ApplicationDbContext db) => _db = db;
    private bool Admin => HttpContext.Session.GetString("Role") == "Admin";
    private bool Lecturer => HttpContext.Session.GetString("Role") == "Lecturer";
    private int UserId => HttpContext.Session.GetInt32("UserId") ?? 0;

    [HttpGet]
    public IActionResult UserTemplate() => Admin ? File(XlsxService.Write("TaiKhoan", new[] { new[] { "FullName", "Email", "Password", "Role", "StudentCode" }, new[] { "Nguyen Van A", "a@example.com", "Matkhau123", "Student", "SV001" } }), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MauNhapTaiKhoan.xlsx") : Forbid();

    [HttpGet]
    public IActionResult QuestionTemplate(int subjectId) => Lecturer ? File(XlsxService.Write("CauHoi", new[] { new[] { "Topic", "Content", "Difficulty", "OptionA", "OptionB", "OptionC", "OptionD", "CorrectAnswer" }, new[] { "Dai so", "1 + 1 = ?", "Easy", "1", "2", "3", "4", "B" } }), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MauNhapCauHoi.xlsx") : Forbid();

    [HttpGet]
    public IActionResult ImportUsers() => Admin
        ? RedirectToAction("CreateUser", "Admin", new { tab = "excel" })
        : RedirectToAction("Login", "Account");

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ImportUsers(IFormFile file)
    {
        if (!Admin) return Forbid();
        if (file == null || file.Length == 0 || !file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        { TempData["Error"] = "Vui lòng chọn tệp .xlsx hợp lệ."; return RedirectToAction(nameof(ImportUsers)); }
        try
        {
            using var stream = file.OpenReadStream();
            var rows = XlsxService.ReadFirstSheet(stream).Skip(1).ToList();
            var roles = await _db.Roles.Where(r => r.Name == "Student" || r.Name == "Lecturer").ToDictionaryAsync(r => r.Name, r => r.Id);
            var emails = new HashSet<string>(await _db.Users.Select(u => u.Email.ToLower()).ToListAsync());
            var imported = new List<User>(); var errors = new List<string>();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i]; string Cell(int n) => n < r.Length ? (r[n] ?? "").Trim() : "";
                var name = Cell(0); var email = Cell(1).ToLowerInvariant(); var password = Cell(2); var role = Cell(3); var code = Cell(4);
                if (string.IsNullOrWhiteSpace(name) || !email.Contains('@') || password.Length < 8 || !roles.ContainsKey(role) || !emails.Add(email))
                { errors.Add($"Dòng {i + 2}: dữ liệu thiếu/sai, role phải là Student hoặc Lecturer, mật khẩu tối thiểu 8 ký tự và email không trùng."); continue; }
                imported.Add(new User { FullName = name, Email = email, Password = BCrypt.Net.BCrypt.HashPassword(password), StudentCode = string.IsNullOrWhiteSpace(code) ? null : code, RoleId = roles[role], IsActive = true, CreatedAt = DateTime.Now });
            }
            if (imported.Count > 0) { _db.Users.AddRange(imported); await _db.SaveChangesAsync(); }
            TempData["Success"] = $"Đã nhập {imported.Count} tài khoản." + (errors.Count > 0 ? $" Có {errors.Count} dòng lỗi (không nhập)." : "");
            TempData["ImportErrors"] = string.Join(" | ", errors.Take(15));
        }
        catch (Exception ex) { TempData["Error"] = "Không thể đọc Excel: " + ex.Message; }
        return RedirectToAction(nameof(ImportUsers));
    }

    [HttpGet]
    public async Task<IActionResult> ImportQuestions(int subjectId)
    {
        if (!Lecturer) return RedirectToAction("Login", "Account");
        var assigned = await _db.LecturerSubjects.AnyAsync(x => x.LecturerId == UserId && x.SubjectId == subjectId);
        if (!assigned) return Forbid();
        ViewBag.Subject = await _db.Subjects.FindAsync(subjectId);
        return View(subjectId);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> ImportQuestions(int subjectId, IFormFile file)
    {
        if (!Lecturer) return Forbid();
        if (!await _db.LecturerSubjects.AnyAsync(x => x.LecturerId == UserId && x.SubjectId == subjectId)) return Forbid();
        if (file == null || file.Length == 0 || !file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        { TempData["Error"] = "Vui lòng chọn tệp .xlsx."; return RedirectToAction(nameof(ImportQuestions), new { subjectId }); }
        try
        {
            using var stream = file.OpenReadStream(); var rows = XlsxService.ReadFirstSheet(stream).Skip(1).ToList();
            var batch = new List<Question>(); var errors = new List<string>();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i]; string C(int n) => n < r.Length ? (r[n] ?? "").Trim() : "";
                var content = C(1); var difficulty = C(2); var correct = C(7).ToUpperInvariant();
                var options = new[] { C(3), C(4), C(5), C(6) };
                if (string.IsNullOrWhiteSpace(content) || options.Any(string.IsNullOrWhiteSpace) || !new[] { "A", "B", "C", "D" }.Contains(correct) || !new[] { "Easy", "Medium", "Hard" }.Contains(difficulty, StringComparer.OrdinalIgnoreCase))
                { errors.Add($"Dòng {i + 2}: thiếu nội dung/đáp án, độ khó phải Easy/Medium/Hard, đáp án đúng A-D."); continue; }
                var q = new Question { SubjectId = subjectId, Topic = string.IsNullOrWhiteSpace(C(0)) ? null : C(0), Content = content, Difficulty = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(difficulty.ToLowerInvariant()), CreatedBy = UserId, IsActive = true, CreatedAt = DateTime.Now };
                for (int j = 0; j < 4; j++) q.Options.Add(new QuestionOption { Content = options[j], IsCorrect = correct == "ABCD"[j].ToString() });
                batch.Add(q);
            }
            if (batch.Count > 0) { _db.Questions.AddRange(batch); await _db.SaveChangesAsync(); }
            TempData["Success"] = $"Đã nhập {batch.Count} câu hỏi." + (errors.Count > 0 ? $" Có {errors.Count} dòng lỗi." : "");
            TempData["ImportErrors"] = string.Join(" | ", errors.Take(15));
        }
        catch (Exception ex) { TempData["Error"] = "Không thể đọc Excel: " + ex.Message; }
        return RedirectToAction(nameof(ImportQuestions), new { subjectId });
    }

    [HttpGet]
    public async Task<IActionResult> ExportResults(int? examId, string? search)
    {
        if (!Admin && !Lecturer) return Forbid();
        var query = _db.ExamResults.Include(r => r.Attempt).ThenInclude(a => a.Student).Include(r => r.Attempt).ThenInclude(a => a.Exam).ThenInclude(e => e.Subject).AsQueryable();
        if (Lecturer) query = query.Where(r => r.Attempt.Exam.CreatedBy == UserId);
        if (examId.HasValue) query = query.Where(r => r.Attempt.ExamId == examId.Value);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(r => r.Attempt.Student.FullName.Contains(search) || r.Attempt.Student.Email.Contains(search));
        var data = await query.OrderBy(r => r.Attempt.Exam.Name).ThenBy(r => r.Attempt.Student.FullName).ToListAsync();
        var rows = new List<string[]> { new[] { "Kỳ thi", "Môn học", "Mã sinh viên", "Sinh viên", "Email", "Tổng câu", "Đúng", "Sai", "Bỏ trống", "Điểm", "Ngày chấm" } };
        rows.AddRange(data.Select(r => new[] { r.Attempt.Exam.Name, r.Attempt.Exam.Subject.Name, r.Attempt.Student.StudentCode ?? "", r.Attempt.Student.FullName, r.Attempt.Student.Email, r.TotalQuestions.ToString(), r.CorrectAnswers.ToString(), r.WrongAnswers.ToString(), r.Unanswered.ToString(), r.Score.ToString("0.00"), r.GradedAt.ToString("dd/MM/yyyy HH:mm") }));
        return File(XlsxService.Write("KetQuaThi", rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"KetQuaThi_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportResultsPdf(int? examId)
    {
        if (!Admin && !Lecturer) return Forbid();
        var query = _db.ExamResults.Include(r => r.Attempt).ThenInclude(a => a.Student).Include(r => r.Attempt).ThenInclude(a => a.Exam).ThenInclude(e => e.Subject).AsQueryable();
        if (Lecturer) query = query.Where(r => r.Attempt.Exam.CreatedBy == UserId);
        if (examId.HasValue) query = query.Where(r => r.Attempt.ExamId == examId.Value);
        var data = await query.OrderByDescending(r => r.GradedAt).Take(500).ToListAsync();
        var lines = new List<string> { "STUDY - BAO CAO KET QUA THI", $"Ngay xuat: {DateTime.Now:dd/MM/yyyy HH:mm}", "" };
        lines.AddRange(data.Select(r => $"{r.Attempt.Exam.Name} | {r.Attempt.Student.FullName} | {r.Attempt.Student.Email} | Diem: {r.Score:0.00} | Dung: {r.CorrectAnswers}/{r.TotalQuestions}"));
        return File(SimplePdf(lines), "application/pdf", $"BaoCaoKetQua_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> ExportPracticeHistory()
    {
        if (HttpContext.Session.GetString("Role") != "Student") return Forbid();
        var studentId = UserId;
        var data = await _db.PracticeResults.Include(r => r.Attempt).ThenInclude(a => a.Subject).Where(r => r.Attempt.StudentId == studentId).OrderByDescending(r => r.GradedAt).ToListAsync();
        var rows = new List<string[]> { new[] { "Mon hoc", "Tong cau", "Dung", "Sai", "Bo trong", "Diem", "Ngay luyen" } };
        rows.AddRange(data.Select(r => new[] { r.Attempt.Subject.Name, r.TotalQuestions.ToString(), r.CorrectAnswers.ToString(), r.WrongAnswers.ToString(), r.Unanswered.ToString(), r.Score.ToString("0.00"), r.GradedAt.ToString("dd/MM/yyyy HH:mm") }));
        return File(XlsxService.Write("LichSuLuyenTap", rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"LichSuLuyenTap_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    private static byte[] SimplePdf(IEnumerable<string> lines)
    {
        static string Safe(string s) { var normalized = s.Normalize(NormalizationForm.FormD); var b = new StringBuilder(); foreach (var c in normalized) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) b.Append(c); return b.ToString().Replace("đ", "d").Replace("Đ", "D").Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)"); }
        var content = new StringBuilder("BT /F1 10 Tf 45 800 Td 13 TL ");
        foreach (var line in lines.Take(55)) content.Append('(').Append(Safe(line)).Append(") Tj T* ");
        content.Append("ET"); var stream = Encoding.ASCII.GetBytes(content.ToString());
        using var ms = new MemoryStream(); using var w = new StreamWriter(ms, Encoding.ASCII, 1024, true);
        var offsets = new List<long>(); w.Write("%PDF-1.4\n"); w.Flush();
        void Obj(int id, string body) { offsets.Add(ms.Position); w.Write($"{id} 0 obj\n{body}\nendobj\n"); w.Flush(); }
        Obj(1, "<< /Type /Catalog /Pages 2 0 R >>"); Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"); Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>"); Obj(4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        offsets.Add(ms.Position); w.Write($"5 0 obj\n<< /Length {stream.Length} >>\nstream\n"); w.Flush(); ms.Write(stream); w.Write("\nendstream\nendobj\n"); w.Flush();
        var xref = ms.Position; w.Write("xref\n0 6\n0000000000 65535 f \n"); foreach (var o in offsets) w.Write(o.ToString("D10") + " 00000 n \n"); w.Write($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"); w.Flush(); return ms.ToArray();
    }
}
