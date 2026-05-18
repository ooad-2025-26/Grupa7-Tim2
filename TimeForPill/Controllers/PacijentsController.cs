using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill.Controllers
{
    public class PacijentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public PacijentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Pacijents
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

        // GET: Pacijents/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var pacijent = await _context.Pacijenti
                .Include(p => p.Ljekar)
                .Include(p => p.KontaktOsoba)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pacijent == null)
            {
                return NotFound();
            }

            return View(pacijent);
        }

        // GET: Pacijents/Create
        public async Task<IActionResult> Create()
        {
            await PopulateListsAsync();

            return View(new Pacijent
            {
                DatumRodjenja = DateTime.Today.AddYears(-18),
                KontaktOsoba = new KontaktOsoba()
            });
        }

        // POST: Pacijents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Pacijent pacijent,
            string password)
        {
            pacijent.KontaktOsoba ??= new KontaktOsoba();

            if (!ModelState.IsValid)
            {
                await PopulateListsAsync(pacijent.LjekarId);
                return View(pacijent);
            }

            try
            {
                pacijent.UserName = pacijent.Email;

                var result = await _userManager.CreateAsync(
                    pacijent,
                    password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(
                        pacijent,
                        "Pacijent");

                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                await PopulateListsAsync(pacijent.LjekarId);

                return View(pacijent);
            }
            catch
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Pacijent nije sacuvan.");

                await PopulateListsAsync(pacijent.LjekarId);

                return View(pacijent);
            }
        }

        // GET: Pacijents/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrEmpty(id))
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

        // POST: Pacijents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            Pacijent pacijent)
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
            postojeciPacijent.UserName = pacijent.Email;
            postojeciPacijent.DatumRodjenja = pacijent.DatumRodjenja;
            postojeciPacijent.Spol = pacijent.Spol;
            postojeciPacijent.LjekarId = pacijent.LjekarId;

            postojeciPacijent.KontaktOsoba ??= new KontaktOsoba();

            postojeciPacijent.KontaktOsoba.Ime =
                pacijent.KontaktOsoba.Ime;

            postojeciPacijent.KontaktOsoba.Prezime =
                pacijent.KontaktOsoba.Prezime;

            postojeciPacijent.KontaktOsoba.Email =
                pacijent.KontaktOsoba.Email;

            postojeciPacijent.KontaktOsoba.BrojTelefona =
                pacijent.KontaktOsoba.BrojTelefona;

            try
            {
                await _userManager.UpdateAsync(postojeciPacijent);

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
        }

        // GET: Pacijents/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var pacijent = await _context.Pacijenti
                .Include(p => p.Ljekar)
                .Include(p => p.KontaktOsoba)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pacijent == null)
            {
                return NotFound();
            }

            return View(pacijent);
        }

        // POST: Pacijents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var pacijent = await _context.Pacijenti
                .Include(p => p.KontaktOsoba)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pacijent != null)
            {
                if (pacijent.KontaktOsoba != null)
                {
                    _context.KontaktOsobe.Remove(
                        pacijent.KontaktOsoba);
                }

                await _userManager.DeleteAsync(pacijent);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateListsAsync(
            string? selectedLjekarId = null)
        {
            var ljekari = await _context.Ljekari
                .AsNoTracking()
                .OrderBy(l => l.Prezime)
                .ThenBy(l => l.Ime)
                .Select(l => new
                {
                    l.Id,
                    Naziv = l.Ime + " " + l.Prezime
                })
                .ToListAsync();

            ViewData["LjekarId"] =
                new SelectList(
                    ljekari,
                    "Id",
                    "Naziv",
                    selectedLjekarId);
        }

        private bool PacijentExists(string id)
        {
            return _context.Pacijenti.Any(e => e.Id == id);
        }
    }
}