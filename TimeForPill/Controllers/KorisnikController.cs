using Microsoft.AspNetCore.Mvc;
using TimeForPill.Data;
using TimeForPill.Models;
using Microsoft.EntityFrameworkCore;

namespace TimeForPill.Controllers
{
    public class KorisnikController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KorisnikController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string email, string lozinka)
        {
            var korisnik = _context.Korisnici
                .FirstOrDefault(k => k.Email == email && k.Lozinka == lozinka);

            if (korisnik != null)
                return RedirectToAction("Index", "Home");

            ViewBag.Greska = "Pogrešni podaci";
            return View();
        }

        public IActionResult PromijeniLozinku() => View();

        [HttpPost]
        public IActionResult PromijeniLozinku(string stara, string nova)
        {
            var korisnik = _context.Korisnici.FirstOrDefault();
            if (korisnik != null && korisnik.Lozinka == stara)
            {
                korisnik.Lozinka = nova;
                _context.SaveChanges();
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
    }
}
