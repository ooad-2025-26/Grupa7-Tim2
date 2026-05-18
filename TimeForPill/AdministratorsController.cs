using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill
{
    public class AdministratorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdministratorsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

        public async Task<IActionResult> Details(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var administrator = await _context.Administratori
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            return administrator == null
                ? NotFound()
                : View(administrator);
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
        public async Task<IActionResult> Create(
            [Bind("Ime,Prezime,Email,DatumRodjenja,datumImenovanja,Spol")]
            Administrator administrator,
            string password)
        {
            if (!ModelState.IsValid)
            {
                return View(administrator);
            }

            administrator.UserName = administrator.Email;

            var result = await _userManager.CreateAsync(
                administrator,
                password);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(administrator);
        }

        public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var administrator = await _context.Administratori
                .FindAsync(id);

            return administrator == null
                ? NotFound()
                : View(administrator);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            [Bind("Id,Ime,Prezime,Email,DatumRodjenja,datumImenovanja,Spol")]
            Administrator administrator)
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
                var existingAdministrator =
                    await _context.Administratori
                        .FirstOrDefaultAsync(a => a.Id == id);

                if (existingAdministrator == null)
                {
                    return NotFound();
                }

                existingAdministrator.Ime = administrator.Ime;
                existingAdministrator.Prezime = administrator.Prezime;
                existingAdministrator.Email = administrator.Email;
                existingAdministrator.UserName = administrator.Email;
                existingAdministrator.DatumRodjenja = administrator.DatumRodjenja;
                existingAdministrator.datumImenovanja = administrator.datumImenovanja;
                existingAdministrator.Spol = administrator.Spol;

                await _userManager.UpdateAsync(existingAdministrator);

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
        }

        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var administrator = await _context.Administratori
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            return administrator == null
                ? NotFound()
                : View(administrator);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var administrator = await _userManager.FindByIdAsync(id);

            if (administrator != null)
            {
                await _userManager.DeleteAsync(administrator);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AdministratorExists(string id)
        {
            return _context.Administratori
                .Any(a => a.Id == id);
        }
    }
}