using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Study.Models;

namespace Study.Controllers;

/// <summary>
/// Các tiện ích nhập/xuất dữ liệu quản trị. Mọi thao tác đều kiểm tra vai trò
/// ở server; không dựa vào việc ẩn/hiện nút trên giao diện.
/// </summary>
public class AdminToolsController : Controller
{
    private readonly ApplicationDbContext _context;
    private const long MaxUploadBytes = 8 * 1024 * 1024;

    public AdminToolsController(ApplicationDbContext context) => _context = context;

    private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin";

    [HttpGet]
    public IActionResult Index()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");
        return RedirectToAction("Exams", "Admin");
    }

    [HttpGet]
    public async Task<IActionResult> StudentList(int examId)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Account");
        var exam = await _context.Exams.AsNoTracking()
            .Include(e => e.Subject)
            .FirstOrDefaultAsync(e => e.Id == examId);
        if (exam == null) return NotFound();

        var students = await _context.ExamAttempts.AsNoTracking()
            .Where(a => a.ExamId == examId)
            .OrderByDescending(a => a.SubmittedAt ?? a.ActualStartTime)
            .Select(a => new Study.ViewModels.AdminExamStudentRowViewModel
            {
                FullName = a.Student.FullName,
                Email = a.Student.Email,
                StudentCode = a.Student.StudentCode ?? "—",
                Status = a.Status,
                Score = a.Result == null ? null : a.Result.Score,
                SubmittedAt = a.SubmittedAt
            }).ToListAsync();

        return View(new Study.ViewModels.AdminExamStudentListViewModel
        {
            ExamId = exam.Id,
            ExamName = exam.Name,
            SubjectName = exam.Subject.Name,
            Students = students
        });
    }

    [HttpGet]
    public IActionResult DownloadTemplate(string type)
    {
        if (!IsAdmin()) return Forbid();
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Template");
        var headers = type switch
        {
            "users" => new[] { "FullName", "Email", "Password", "Role", "StudentCode" },
            _ => Array.Empty<string>()
        };
        if (headers.Length == 0) return BadRequest();
        for (int i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream(); workbook.SaveAs(stream);
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Study_Template_{type}.xlsx");
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> ImportUsers(IFormFile? file)
    {
        if (!IsAdmin()) return Forbid();
        if (!ValidWorkbook(file)) return ImportUserError("Vui lòng chọn file .xlsx hợp lệ, dung lượng tối đa 8 MB.");

        using var workbook = TryOpenWorkbook(file!);
        if (workbook == null) return ImportUserError("Không thể đọc file Excel. Hãy kiểm tra định dạng .xlsx.");
        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet == null) return ImportUserError("File Excel không có trang tính.");

        // Cột: FullName | Email | Password | Role (Student/Lecturer) | StudentCode
        var rows = sheet.RowsUsed().Skip(1).Take(1001).ToList();
        if (rows.Count > 1000) return ImportUserError("Mỗi lần chỉ nhập tối đa 1.000 tài khoản.");

        var roles = await _context.Roles.Where(r => r.Name == "Student" || r.Name == "Lecturer")
            .ToDictionaryAsync(r => r.Name, r => r.Id);
        var emails = rows.Select(r => r.Cell(2).GetString().Trim().ToLowerInvariant())
            .Where(x => x.Length > 0).Distinct().ToList();
        var existing = await _context.Users.Where(u => emails.Contains(u.Email.ToLower()))
            .Select(u => u.Email).ToListAsync();
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        int imported = 0, skipped = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            var name = row.Cell(1).GetString().Trim();
            var email = row.Cell(2).GetString().Trim();
            var password = row.Cell(3).GetString();
            var roleName = row.Cell(4).GetString().Trim();
            var studentCode = row.Cell(5).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name) || !email.Contains('@') ||
                password.Length < 8 || !roles.TryGetValue(roleName, out var roleId))
            {
                skipped++; errors.Add($"Dòng {row.RowNumber()}: dữ liệu thiếu/sai (mật khẩu tối thiểu 8 ký tự, Role là Student hoặc Lecturer).");
                continue;
            }
            if (!existingSet.Add(email))
            {
                skipped++; errors.Add($"Dòng {row.RowNumber()}: email {email} đã tồn tại hoặc bị trùng trong file.");
                continue;
            }
            _context.Users.Add(new User
            {
                FullName = name, Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                RoleId = roleId, StudentCode = roleName == "Student" && studentCode.Length > 0 ? studentCode : null,
                IsActive = true, CreatedAt = DateTime.Now
            });
            imported++;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Đã nhập {imported} tài khoản; bỏ qua {skipped} dòng.";
        TempData["ImportErrors"] = errors.Count > 0 ? string.Join("\n", errors.Take(20)) : null;
        return RedirectToAction("CreateUser", "Admin", new { tab = "excel" });
    }

    [HttpGet]
    public async Task<IActionResult> ExportResultsExcel()
    {
        if (!IsAdmin()) return Forbid();
        var rows = await ResultRows();
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("KetQuaThi");
        var headers = new[] { "Mã lần thi", "Sinh viên", "Email", "Mã SV", "Đề thi", "Môn học", "Điểm", "Đúng", "Sai", "Chưa trả lời", "Nộp lúc" };
        for (int c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i]; int n = i + 2;
            ws.Cell(n, 1).Value = r.AttemptId; ws.Cell(n, 2).Value = r.StudentName;
            ws.Cell(n, 3).Value = r.Email; ws.Cell(n, 4).Value = r.StudentCode;
            ws.Cell(n, 5).Value = r.ExamName; ws.Cell(n, 6).Value = r.SubjectName;
            ws.Cell(n, 7).Value = r.Score; ws.Cell(n, 8).Value = r.Correct;
            ws.Cell(n, 9).Value = r.Wrong; ws.Cell(n, 10).Value = r.Unanswered;
            if (r.SubmittedAt.HasValue) ws.Cell(n, 11).Value = r.SubmittedAt.Value;
        }
        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EEFF");
        ws.SheetView.FreezeRows(1); ws.Columns().AdjustToContents();

        var stats = workbook.Worksheets.Add("ThongKe");
        var statHeaders = new[] { "Đề thi", "Môn học", "Số lượt thi", "Điểm trung bình", "Điểm cao nhất", "Điểm thấp nhất", "Đạt từ 5 điểm" };
        for (int c = 0; c < statHeaders.Length; c++) stats.Cell(1, c + 1).Value = statHeaders[c];
        var groups = rows.GroupBy(r => new { r.ExamName, r.SubjectName }).ToList();
        for (int i = 0; i < groups.Count; i++)
        {
            var g = groups[i]; var values = g.Select(x => x.Score).ToList(); int n = i + 2;
            stats.Cell(n, 1).Value = g.Key.ExamName; stats.Cell(n, 2).Value = g.Key.SubjectName;
            stats.Cell(n, 3).Value = values.Count; stats.Cell(n, 4).Value = values.Average();
            stats.Cell(n, 5).Value = values.Max(); stats.Cell(n, 6).Value = values.Min();
            stats.Cell(n, 7).Value = values.Count(x => x >= 5);
        }
        stats.Row(1).Style.Font.Bold = true;
        stats.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EEFF");
        stats.Columns().AdjustToContents();
        using var stream = new MemoryStream(); workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Study_KetQuaThi_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportResultsPdf()
    {
        if (!IsAdmin()) return Forbid();
        var rows = await ResultRows();
        QuestPDF.Settings.License = LicenseType.Community;
        var bytes = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape()); page.Margin(24);
            page.DefaultTextStyle(x => x.FontSize(8));
            page.Header().Column(c =>
            {
                c.Item().Text("STUDY - BÁO CÁO KẾT QUẢ THI").FontSize(16).Bold();
                c.Item().Text($"Xuất ngày {DateTime.Now:dd/MM/yyyy HH:mm} | Tổng lượt thi: {rows.Count} | Điểm TB: {(rows.Count == 0 ? 0 : rows.Average(x => x.Score)):0.00} | Tỷ lệ đạt: {(rows.Count == 0 ? 0 : rows.Count(x => x.Score >= 5) * 100.0 / rows.Count):0.0}%");
            });
            page.Content().PaddingTop(12).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(30); cols.RelativeColumn(1.5f); cols.RelativeColumn(1.4f);
                    cols.RelativeColumn(1.7f); cols.RelativeColumn(1.5f); cols.ConstantColumn(38);
                    cols.ConstantColumn(28); cols.ConstantColumn(28); cols.ConstantColumn(34);
                });
                table.Header(h =>
                {
                    foreach (var title in new[] { "ID", "Sinh viên", "Email", "Đề thi", "Môn học", "Điểm", "Đúng", "Sai", "Bỏ trống" })
                        h.Cell().Background(Colors.Indigo.Lighten4).Padding(4).Text(title).Bold();
                });
                foreach (var r in rows)
                {
                    table.Cell().Padding(3).Text(r.AttemptId.ToString());
                    table.Cell().Padding(3).Text(r.StudentName);
                    table.Cell().Padding(3).Text(r.Email);
                    table.Cell().Padding(3).Text(r.ExamName);
                    table.Cell().Padding(3).Text(r.SubjectName);
                    table.Cell().Padding(3).Text(r.Score.ToString("0.00"));
                    table.Cell().Padding(3).Text(r.Correct.ToString());
                    table.Cell().Padding(3).Text(r.Wrong.ToString());
                    table.Cell().Padding(3).Text(r.Unanswered.ToString());
                }
            });
            page.Footer().AlignRight().Text(x => { x.Span("Trang "); x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
        })).GeneratePdf();
        return File(bytes, "application/pdf", $"Study_KetQuaThi_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    private async Task<List<ResultExportRow>> ResultRows() =>
        await _context.ExamAttempts.AsNoTracking()
            .Where(a => a.Status == "Submitted" && a.Result != null)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new ResultExportRow(a.Id, a.Student.FullName, a.Student.Email,
                a.Student.StudentCode ?? "", a.Exam.Name, a.Exam.Subject.Name,
                a.Result!.Score, a.Result.CorrectAnswers, a.Result.WrongAnswers,
                a.Result.Unanswered, a.SubmittedAt))
            .ToListAsync();

    private static XLWorkbook? TryOpenWorkbook(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            return new XLWorkbook(stream);
        }
        catch
        {
            return null;
        }
    }

    private bool ValidWorkbook(IFormFile? file) =>
        file != null && file.Length > 0 && file.Length <= MaxUploadBytes &&
        Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase);

    private IActionResult ImportUserError(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction("CreateUser", "Admin", new { tab = "excel" });
    }



    private sealed record ResultExportRow(int AttemptId, string StudentName, string Email,
        string StudentCode, string ExamName, string SubjectName, decimal Score,
        int Correct, int Wrong, int Unanswered, DateTime? SubmittedAt);
}
