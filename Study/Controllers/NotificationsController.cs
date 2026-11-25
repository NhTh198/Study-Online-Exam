using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Study.Models;

namespace Study.Controllers;

public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;
    public NotificationsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue || userId.Value <= 0) return RedirectToAction("Login", "Account");
        var items = await _db.Notifications.Where(n => n.UserId == userId.Value).OrderByDescending(n => n.CreatedAt).Take(100).ToListAsync();
        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return Forbid();
        var item = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value);
        if (item == null) return NotFound();
        item.IsRead = true; await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return Forbid();
        var unread = await _db.Notifications.Where(n => n.UserId == userId.Value && !n.IsRead).ToListAsync();
        foreach (var item in unread) item.IsRead = true;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
