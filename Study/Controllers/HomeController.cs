using Microsoft.AspNetCore.Mvc;

namespace Study.Controllers;

public class HomeController : Controller
{
    // Issue #1: Trang chủ
    public IActionResult Index() => View();
}
