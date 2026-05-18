using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill
{
    public class ZahtjevsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ZahtjevsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var zahtjevi = await _context.Zahtjevi
                .Include(z => z.Terapija)
                .AsNoTracking()
                .OrderBy(z => z.Status)
                .ThenBy(z => z.Naziv)
                .ToListAsync();

            return View(zahtjevi);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var zahtjev = await _context.Zahtjevi
                .Include(z => z.Terapija)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return zahtjev == null ? NotFound() : View(zahtjev);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateListsAsync();
            return View(new Zahtjev { Status = StatusZahtjeva.Neobraden });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Naziv,Sadrzaj,TerapijaId,Status")] Zahtjev zahtjev)
        {
            ValidateTerapija(zahtjev);

            if (!ModelState.IsValid)
            {
                await PopulateListsAsync(zahtjev.TerapijaId);
                return View(zahtjev);
            }

            try
            {
                _context.Add(zahtjev);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Zahtjev nije sacuvan. Provjerite terapiju i vezu sa bazom.");
                await PopulateListsAsync(zahtjev.TerapijaId);
                return View(zahtjev);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var zahtjev = await _context.Zahtjevi.FindAsync(id);
            if (zahtjev == null)
            {
                return NotFound();
            }

            await PopulateListsAsync(zahtjev.TerapijaId);
            return View(zahtjev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Naziv,Sadrzaj,TerapijaId,Status")] Zahtjev zahtjev)
        {
            if (id != zahtjev.Id)
            {
                return NotFound();
            }

            ValidateTerapija(zahtjev);

            if (!ModelState.IsValid)
            {
                await PopulateListsAsync(zahtjev.TerapijaId);
                return View(zahtjev);
            }

            try
            {
                _context.Update(zahtjev);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ZahtjevExists(zahtjev.Id))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Izmjene nisu sacuvane. Provjerite terapiju i vezu sa bazom.");
                await PopulateListsAsync(zahtjev.TerapijaId);
                return View(zahtjev);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var zahtjev = await _context.Zahtjevi
                .Include(z => z.Terapija)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return zahtjev == null ? NotFound() : View(zahtjev);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var zahtjev = await _context.Zahtjevi.FindAsync(id);
            if (zahtjev != null)
            {
                _context.Zahtjevi.Remove(zahtjev);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateListsAsync(int? selectedTerapijaId = null)
        {
            var terapije = await _context.Terapije
                .AsNoTracking()
                .OrderBy(t => t.Naziv)
                .Select(t => new { t.Id, t.Naziv })
                .ToListAsync();

            ViewData["TerapijaId"] = new SelectList(terapije, "Id", "Naziv", selectedTerapijaId);
        }

        private void ValidateTerapija(Zahtjev zahtjev)
        {
            if (!zahtjev.TerapijaId.HasValue)
            {
                ModelState.AddModelError(nameof(Zahtjev.TerapijaId), "Odaberite terapiju.");
            }
        }

        private bool ZahtjevExists(int id)
        {
            return _context.Zahtjevi.Any(e => e.Id == id);
        }
    }
}
