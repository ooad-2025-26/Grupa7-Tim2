using Microsoft.AspNetCore.Mvc;

namespace TimeForPill
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
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
