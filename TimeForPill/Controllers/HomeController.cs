using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using TimeForPill.Models;

namespace TimeForPill
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.GetUserAsync(User);

            return user switch
            {
                Pacijent => RedirectToAction("Home", "Pacijent"),
                Ljekar => RedirectToAction("Home", "Ljekar"),
                Administrator => RedirectToAction("Home", "Admin"),
                _ => RedirectToAction("Login", "Account")
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View(new Models.ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }
}
