using Microsoft.AspNetCore.Mvc;
using TimeForPill.Data;
using TimeForPill.Models;
using Microsoft.EntityFrameworkCore;

namespace TimeForPill.Controllers
{
    public class PacijentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PacijentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var pacijenti = _context.Pacijenti.ToList();
            return View(pacijenti);
        }

        public IActionResult Terapije(int id)
        {
            var terapije = _context.Terapije
                .Where(t => t.PacijentId == id)
                .ToList();

            return View(terapije);
        }

        public IActionResult Kreiraj() => View();

        [HttpPost]
        public IActionResult Kreiraj(Pacijent p)
        {
            _context.Pacijenti.Add(p);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult PosaljiZahtjev(int terapijaId)
        {
            var z = new Zahtjev
            {
                Naziv = "Zahtjev",
                Sadrzaj = "Potrebna izmjena terapije",
                TerapijaId = terapijaId,
                Status = StatusZahtjeva.Neobraden
            };

            _context.Zahtjevi.Add(z);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
