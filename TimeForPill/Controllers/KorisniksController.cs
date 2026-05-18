using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill
{
    public class KorisniksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KorisniksController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var korisnici = await _context.Korisnici
                .AsNoTracking()
                .OrderBy(k => k.Prezime)
                .ThenBy(k => k.Ime)
                .ToListAsync();

            return View(korisnici);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var korisnik = await _context.Korisnici
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return korisnik == null ? NotFound() : View(korisnik);
        }

        public IActionResult Create()
        {
            return RedirectToAction("Create", "Pacijents");
        }

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePost()
        {
            return RedirectToAction("Create", "Pacijents");
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var korisnik = await _context.Korisnici.FindAsync(id);
            if (korisnik == null)
            {
                return NotFound();
            }

            return korisnik switch
            {
                Pacijent => RedirectToAction("Edit", "Pacijents", new { id }),
                Ljekar => RedirectToAction("Edit", "Ljekars", new { id }),
                Administrator => RedirectToAction("Edit", "Administrators", new { id }),
                _ => RedirectToAction(nameof(Index))
            };
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var korisnik = await _context.Korisnici
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return korisnik == null ? NotFound() : View(korisnik);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var korisnik = await _context.Korisnici.FindAsync(id);
            if (korisnik != null)
            {
                _context.Korisnici.Remove(korisnik);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
