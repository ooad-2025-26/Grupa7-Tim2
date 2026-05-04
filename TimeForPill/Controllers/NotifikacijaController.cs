using Microsoft.AspNetCore.Mvc;
using TimeForPill.Data;
using TimeForPill.Models;
using Microsoft.EntityFrameworkCore;

namespace TimeForPill.Controllers
{
    public class NotifikacijaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotifikacijaController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Posalji(int terapijaId)
        {
            var n = new Notifikacija
            {
                Naziv = "Podsjetnik",
                Poruka = "Vrijeme za lijek",
                TerapijaId = terapijaId
            };

            _context.Notifikacije.Add(n);
            _context.SaveChanges();

            return RedirectToAction("Index", "Terapija");
        }
    }
}
