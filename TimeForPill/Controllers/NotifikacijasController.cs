using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill
{
    public class NotifikacijasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotifikacijasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var notifikacije = await _context.Notifikacije
                .Include(n => n.Terapija)
                .AsNoTracking()
                .OrderBy(n => n.Naziv)
                .ToListAsync();

            return View(notifikacije);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notifikacija = await _context.Notifikacije
                .Include(n => n.Terapija)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return notifikacija == null ? NotFound() : View(notifikacija);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateListsAsync();
            return View(new Notifikacija());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Naziv,Poruka,TerapijaId")] Notifikacija notifikacija)
        {
            ValidateTerapija(notifikacija);

            if (!ModelState.IsValid)
            {
                await PopulateListsAsync(notifikacija.TerapijaId);
                return View(notifikacija);
            }

            try
            {
                _context.Add(notifikacija);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Notifikacija nije sacuvana. Provjerite terapiju i vezu sa bazom.");
                await PopulateListsAsync(notifikacija.TerapijaId);
                return View(notifikacija);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notifikacija = await _context.Notifikacije.FindAsync(id);
            if (notifikacija == null)
            {
                return NotFound();
            }

            await PopulateListsAsync(notifikacija.TerapijaId);
            return View(notifikacija);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Naziv,Poruka,TerapijaId")] Notifikacija notifikacija)
        {
            if (id != notifikacija.Id)
            {
                return NotFound();
            }

            ValidateTerapija(notifikacija);

            if (!ModelState.IsValid)
            {
                await PopulateListsAsync(notifikacija.TerapijaId);
                return View(notifikacija);
            }

            try
            {
                _context.Update(notifikacija);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NotifikacijaExists(notifikacija.Id))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Izmjene nisu sacuvane. Provjerite terapiju i vezu sa bazom.");
                await PopulateListsAsync(notifikacija.TerapijaId);
                return View(notifikacija);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notifikacija = await _context.Notifikacije
                .Include(n => n.Terapija)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return notifikacija == null ? NotFound() : View(notifikacija);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var notifikacija = await _context.Notifikacije.FindAsync(id);
            if (notifikacija != null)
            {
                _context.Notifikacije.Remove(notifikacija);
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

        private void ValidateTerapija(Notifikacija notifikacija)
        {
            if (!notifikacija.TerapijaId.HasValue)
            {
                ModelState.AddModelError(nameof(Notifikacija.TerapijaId), "Odaberite terapiju.");
            }
        }

        private bool NotifikacijaExists(int id)
        {
            return _context.Notifikacije.Any(e => e.Id == id);
        }
    }
}
