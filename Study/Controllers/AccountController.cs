using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Study.Models;
using Study.ViewModels;

namespace Study.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================
    // LOGIN - GET
    // =========================
    [HttpGet]
    public IActionResult Login()
    {
        var role = HttpContext.Session.GetString("Role");

        if (!string.IsNullOrEmpty(role))
        {
            return RedirectByRole(role);
        }

        return View();
    }

    // =========================
    // LOGIN - POST
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.Email == model.Email.Trim());

        if (user == null)
        {
            ModelState.AddModelError(
                "",
                "Email hoặc mật khẩu không chính xác."
            );

            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(
                "",
                "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên."
            );

            return View(model);
        }

        bool passwordValid;

        try
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(
                model.Password,
                user.Password
            );
        }
        catch
        {
            passwordValid = false;
        }

        if (!passwordValid)
        {
            ModelState.AddModelError(
                "",
                "Email hoặc mật khẩu không chính xác."
            );

            return View(model);
        }

        // Lưu thông tin đăng nhập vào Session
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("Role", user.Role.Name);

        return RedirectByRole(user.Role.Name);
    }

    // =========================
    // LOGOUT
    // =========================
    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return RedirectToAction("Index", "Home");
    }

    // =========================
    // TẠO ADMIN BAN ĐẦU
    // CHỈ DÙNG 1 LẦN
    // =========================


    // =========================
    // REDIRECT THEO ROLE
    // =========================
    private IActionResult RedirectByRole(string role)
    {
        switch (role)
        {
            case "Admin":
                return RedirectToAction("Index", "Admin");

            case "Lecturer":
                return RedirectToAction("Index", "Lecturer");

            case "Student":
                return RedirectToAction("Index", "Student");

            default:
                HttpContext.Session.Clear();

                return RedirectToAction("Login", "Account");
        }
    }

    // ==============================
    // REGISTER STUDENT
    // ==============================

    [HttpGet]
    public IActionResult Register()
    {
        var role = HttpContext.Session.GetString("Role");

        if (!string.IsNullOrEmpty(role))
        {
            return RedirectByRole(role);
        }

        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string email = model.Email.Trim().ToLower();

        // Kiểm tra email đã tồn tại
        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email);

        if (emailExists)
        {
            ModelState.AddModelError(
                "Email",
                "Email này đã được sử dụng.");

            return View(model);
        }

        // Tạo StudentCode duy nhất
        string studentCode;

        do
        {
            studentCode = "SV" + Random.Shared.Next(100000, 1000000);
        }
        while (await _context.Users.AnyAsync(
            u => u.StudentCode == studentCode));

        // Hash password
        string hashedPassword =
            BCrypt.Net.BCrypt.HashPassword(model.Password);

        var student = new User
        {
            FullName = model.FullName.Trim(),
            Email = email,
            Password = hashedPassword,

            // Tự động tạo mã Student
            StudentCode = studentCode,

            // RoleId = 3 -> Student
            RoleId = 3,

            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(student);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Đăng ký tài khoản thành công. Vui lòng đăng nhập.";

        return RedirectToAction(nameof(Login));
    }


}