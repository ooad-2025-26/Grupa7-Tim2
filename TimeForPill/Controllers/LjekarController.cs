using Microsoft.AspNetCore.Mvc;
using TimeForPill.Data;
using TimeForPill.Models;
using Microsoft.EntityFrameworkCore;

namespace TimeForPill.Controllers
{
    public class LjekarController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LjekarController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Zahtjevi()
        {
            return View(_context.Zahtjevi.ToList());
        }

        public IActionResult Obradi(int id)
        {
            var z = _context.Zahtjevi.Find(id);
            if (z != null)
            {
                z.Status = StatusZahtjeva.Obraden;
                _context.SaveChanges();
            }

            return RedirectToAction("Zahtjevi");
        }
    }
}
