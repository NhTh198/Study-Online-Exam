using Microsoft.AspNetCore.Mvc;
using Study.Models;

namespace Study.Controllers;

// Shared controller shell. Feature actions are added in their own feature branches.
public partial class AdminSubjectController : Controller
{
    protected readonly ApplicationDbContext _context;

    public AdminSubjectController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }

    protected bool IsAdmin()
    {
        return HttpContext.Session.GetString("Role") == "Admin";
    }
}
