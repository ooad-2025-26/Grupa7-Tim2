using Microsoft.AspNetCore.Mvc;
using TimeForPill.Data;
using TimeForPill.Models;
using Microsoft.EntityFrameworkCore;

namespace TimeForPill.Controllers
{
    public class LijekController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LijekController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View(_context.Lijekovi.ToList());
        }

        public IActionResult Dodaj() => View();

        [HttpPost]
        public IActionResult Dodaj(Lijek l)
        {
            _context.Lijekovi.Add(l);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
