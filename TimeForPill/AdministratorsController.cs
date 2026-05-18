using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill
{
    public class AdministratorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdministratorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var administratori = await _context.Administratori
                .AsNoTracking()
                .OrderBy(a => a.Prezime)
                .ThenBy(a => a.Ime)
                .ToListAsync();

            return View(administratori);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var administrator = await _context.Administratori
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return administrator == null ? NotFound() : View(administrator);
        }

        public IActionResult Create()
        {
            return View(new Administrator
            {
                DatumRodjenja = DateTime.Today.AddYears(-30),
                datumImenovanja = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("datumImenovanja,Ime,Prezime,Email,Lozinka,DatumRodjenja,Spol")] Administrator administrator)
        {
            if (!ModelState.IsValid)
            {
                return View(administrator);
            }

            try
            {
                _context.Add(administrator);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Administrator nije sacuvan. Provjerite podatke i vezu sa bazom.");
                return View(administrator);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var administrator = await _context.Administratori.FindAsync(id);
            return administrator == null ? NotFound() : View(administrator);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,datumImenovanja,Ime,Prezime,Email,Lozinka,DatumRodjenja,Spol")] Administrator administrator)
        {
            if (id != administrator.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(administrator);
            }

            try
            {
                _context.Update(administrator);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AdministratorExists(administrator.Id))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Izmjene nisu sacuvane. Provjerite podatke i vezu sa bazom.");
                return View(administrator);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var administrator = await _context.Administratori
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return administrator == null ? NotFound() : View(administrator);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var administrator = await _context.Administratori.FindAsync(id);
            if (administrator != null)
            {
                _context.Administratori.Remove(administrator);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AdministratorExists(int id)
        {
            return _context.Administratori.Any(e => e.Id == id);
        }
    }
}
