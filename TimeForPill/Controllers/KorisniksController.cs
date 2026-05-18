using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill.Controllers
{
    public class KorisniksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KorisniksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Korisniks
        public async Task<IActionResult> Index()
        {
            var korisnici = await _context.Users
                .AsNoTracking()
                .OrderBy(k => k.Prezime)
                .ThenBy(k => k.Ime)
                .ToListAsync();

            return View(korisnici);
        }

        // GET: Korisniks/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var korisnik = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == id);

            if (korisnik == null)
            {
                return NotFound();
            }

            return View(korisnik);
        }

        // GET: Korisniks/Create
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

        // GET: Korisniks/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var korisnik = await _context.Users.FindAsync(id);

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

        // GET: Korisniks/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var korisnik = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == id);

            if (korisnik == null)
            {
                return NotFound();
            }

            return View(korisnik);
        }

        // POST: Korisniks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var korisnik = await _context.Users.FindAsync(id);

            if (korisnik != null)
            {
                _context.Users.Remove(korisnik);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}