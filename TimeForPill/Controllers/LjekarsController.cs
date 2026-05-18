using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill
{
    public class LjekarsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LjekarsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var ljekari = await _context.Ljekari
                .AsNoTracking()
                .OrderBy(l => l.Prezime)
                .ThenBy(l => l.Ime)
                .ToListAsync();

            return View(ljekari);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ljekar = await _context.Ljekari
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return ljekar == null ? NotFound() : View(ljekar);
        }

        public IActionResult Create()
        {
            return View(new Ljekar { DatumRodjenja = DateTime.Today.AddYears(-35) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Specijalizacija,Ime,Prezime,Email,Lozinka,DatumRodjenja,Spol")] Ljekar ljekar)
        {
            if (!ModelState.IsValid)
            {
                return View(ljekar);
            }

            try
            {
                _context.Add(ljekar);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Ljekar nije sacuvan. Provjerite podatke i vezu sa bazom.");
                return View(ljekar);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ljekar = await _context.Ljekari.FindAsync(id);
            return ljekar == null ? NotFound() : View(ljekar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Specijalizacija,Ime,Prezime,Email,Lozinka,DatumRodjenja,Spol")] Ljekar ljekar)
        {
            if (id != ljekar.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(ljekar);
            }

            try
            {
                _context.Update(ljekar);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LjekarExists(ljekar.Id))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Izmjene nisu sacuvane. Provjerite podatke i vezu sa bazom.");
                return View(ljekar);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ljekar = await _context.Ljekari
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return ljekar == null ? NotFound() : View(ljekar);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ljekar = await _context.Ljekari.FindAsync(id);
            if (ljekar != null)
            {
                _context.Ljekari.Remove(ljekar);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool LjekarExists(int id)
        {
            return _context.Ljekari.Any(e => e.Id == id);
        }
    }
}
