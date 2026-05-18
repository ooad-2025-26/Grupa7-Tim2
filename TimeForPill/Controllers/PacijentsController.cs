using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill
{
    public class PacijentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PacijentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var pacijenti = await _context.Pacijenti
                .Include(p => p.Ljekar)
                .Include(p => p.KontaktOsoba)
                .AsNoTracking()
                .OrderBy(p => p.Prezime)
                .ThenBy(p => p.Ime)
                .ToListAsync();

            return View(pacijenti);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pacijent = await _context.Pacijenti
                .Include(p => p.Ljekar)
                .Include(p => p.KontaktOsoba)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return pacijent == null ? NotFound() : View(pacijent);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateListsAsync();

            return View(new Pacijent
            {
                DatumRodjenja = DateTime.Today.AddYears(-18),
                KontaktOsoba = new KontaktOsoba()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pacijent pacijent)
        {
            pacijent.KontaktOsoba ??= new KontaktOsoba();

            if (!ModelState.IsValid)
            {
                await PopulateListsAsync(pacijent.LjekarId);
                return View(pacijent);
            }

            try
            {
                _context.Add(pacijent);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Pacijent nije sacuvan. Provjerite podatke i vezu sa bazom.");
                await PopulateListsAsync(pacijent.LjekarId);
                return View(pacijent);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pacijent = await _context.Pacijenti
                .Include(p => p.KontaktOsoba)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pacijent == null)
            {
                return NotFound();
            }

            pacijent.KontaktOsoba ??= new KontaktOsoba();
            await PopulateListsAsync(pacijent.LjekarId);
            return View(pacijent);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Pacijent pacijent)
        {
            pacijent.KontaktOsoba ??= new KontaktOsoba();

            if (id != pacijent.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PopulateListsAsync(pacijent.LjekarId);
                return View(pacijent);
            }

            var postojeciPacijent = await _context.Pacijenti
                .Include(p => p.KontaktOsoba)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (postojeciPacijent == null)
            {
                return NotFound();
            }

            postojeciPacijent.Ime = pacijent.Ime;
            postojeciPacijent.Prezime = pacijent.Prezime;
            postojeciPacijent.Email = pacijent.Email;
            postojeciPacijent.Lozinka = pacijent.Lozinka;
            postojeciPacijent.DatumRodjenja = pacijent.DatumRodjenja;
            postojeciPacijent.Spol = pacijent.Spol;
            postojeciPacijent.LjekarId = pacijent.LjekarId;

            postojeciPacijent.KontaktOsoba ??= new KontaktOsoba();
            postojeciPacijent.KontaktOsoba.Ime = pacijent.KontaktOsoba.Ime;
            postojeciPacijent.KontaktOsoba.Prezime = pacijent.KontaktOsoba.Prezime;
            postojeciPacijent.KontaktOsoba.Email = pacijent.KontaktOsoba.Email;
            postojeciPacijent.KontaktOsoba.BrojTelefona = pacijent.KontaktOsoba.BrojTelefona;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PacijentExists(pacijent.Id))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Izmjene nisu sacuvane. Provjerite podatke i vezu sa bazom.");
                await PopulateListsAsync(pacijent.LjekarId);
                return View(pacijent);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pacijent = await _context.Pacijenti
                .Include(p => p.Ljekar)
                .Include(p => p.KontaktOsoba)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return pacijent == null ? NotFound() : View(pacijent);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pacijent = await _context.Pacijenti
                .Include(p => p.KontaktOsoba)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pacijent != null)
            {
                if (pacijent.KontaktOsoba != null)
                {
                    _context.KontaktOsobe.Remove(pacijent.KontaktOsoba);
                }

                _context.Pacijenti.Remove(pacijent);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateListsAsync(int? selectedLjekarId = null)
        {
            var ljekari = await _context.Ljekari
                .AsNoTracking()
                .OrderBy(l => l.Prezime)
                .ThenBy(l => l.Ime)
                .Select(l => new { l.Id, Naziv = l.Ime + " " + l.Prezime })
                .ToListAsync();

            ViewData["LjekarId"] = new SelectList(ljekari, "Id", "Naziv", selectedLjekarId);
        }

        private bool PacijentExists(int id)
        {
            return _context.Pacijenti.Any(e => e.Id == id);
        }
    }
}
