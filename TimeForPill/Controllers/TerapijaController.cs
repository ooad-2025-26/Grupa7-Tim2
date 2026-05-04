using Microsoft.AspNetCore.Mvc;
using TimeForPill.Data;
using TimeForPill.Models;
using Microsoft.EntityFrameworkCore;

namespace TimeForPill.Controllers
{
    public class TerapijaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TerapijaController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View(_context.Terapije.ToList());
        }

        public IActionResult Kreiraj() => View();

        [HttpPost]
        public IActionResult Kreiraj(Terapija t)
        {
            t.Status = StatusTerapije.Cekanje;
            _context.Terapije.Add(t);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        public IActionResult Deaktiviraj(int id)
        {
            var t = _context.Terapije.Find(id);
            if (t != null)
            {
                t.Status = StatusTerapije.Propusteno;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
