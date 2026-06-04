using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill.Controllers
{
    public class LjekarsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LjekarsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Ljekars
        public async Task<IActionResult> Index()
        {
            var ljekari = await _context.Ljekari
                .AsNoTracking()
                .OrderBy(l => l.Prezime)
                .ThenBy(l => l.Ime)
                .ToListAsync();

            return View(ljekari);
        }

        // GET: Ljekars/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var ljekar = await _context.Ljekari
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ljekar == null)
            {
                return NotFound();
            }

            return View(ljekar);
        }

        // GET: Ljekars/Create
        public IActionResult Create()
        {
            return View(new Ljekar
            {
                DatumRodjenja = DateTime.Today.AddYears(-35)
            });
        }

        // POST: Ljekars/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Specijalizacija,Ime,Prezime,Email,DatumRodjenja,Spol")]
            Ljekar ljekar,

            string password)
        {
            if (!ModelState.IsValid)
            {
                return View(ljekar);
            }

            try
            {
                ljekar.UserName = ljekar.Email;

                var result = await _userManager.CreateAsync(
                    ljekar,
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

                return View(ljekar);
            }
            catch
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Ljekar nije sacuvan.");

                return View(ljekar);
            }
        }

        // GET: Ljekars/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var ljekar = await _context.Ljekari.FindAsync(id);

            if (ljekar == null)
            {
                return NotFound();
            }

            return View(ljekar);
        }

        // POST: Ljekars/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,

            [Bind("Id,Specijalizacija,Ime,Prezime,Email,DatumRodjenja,Spol")]
            Ljekar ljekar)
        {
            if (id != ljekar.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(ljekar);
            }

            var postojeciLjekar =
                await _context.Ljekari.FindAsync(id);

            if (postojeciLjekar == null)
            {
                return NotFound();
            }

            postojeciLjekar.Ime = ljekar.Ime;
            postojeciLjekar.Prezime = ljekar.Prezime;
            postojeciLjekar.Email = ljekar.Email;
            postojeciLjekar.UserName = ljekar.Email;
            postojeciLjekar.Specijalizacija = ljekar.Specijalizacija;
            postojeciLjekar.DatumRodjenja = ljekar.DatumRodjenja;
            postojeciLjekar.Spol = ljekar.Spol;

            try
            {
                await _userManager.UpdateAsync(postojeciLjekar);

                await LogAdminActionAsync(
                    "Uredjen",
                    "Ljekar",
                    postojeciLjekar.Id,
                    $"{postojeciLjekar.Ime} {postojeciLjekar.Prezime}");

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
        }

        // GET: Ljekars/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var ljekar = await _context.Ljekari
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ljekar == null)
            {
                return NotFound();
            }

            return View(ljekar);
        }

        // POST: Ljekars/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var ljekar = await _context.Ljekari.FindAsync(id);

            if (ljekar != null)
            {
                try
                {
                    await ClearDoctorDependenciesAsync(id);

                    _context.Ljekari.Remove(ljekar);

                    await LogAdminActionAsync(
                        "Obrisan",
                        "Ljekar",
                        ljekar.Id,
                        $"{ljekar.Ime} {ljekar.Prezime}");

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Ljekar je obrisan iz sistema.";
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Ljekar nije obrisan jer postoje povezani podaci koje baza ne dozvoljava obrisati.");

                    return View("Delete", ljekar);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task ClearDoctorDependenciesAsync(string ljekarId)
        {
            var pacijenti = await _context.Pacijenti
                .Where(p => p.LjekarId == ljekarId)
                .ToListAsync();

            foreach (var pacijent in pacijenti)
            {
                pacijent.LjekarId = null;
            }

            var tickets = await _context.Tickets
                .Where(t => t.KorisnikId == ljekarId)
                .ToListAsync();

            _context.Tickets.RemoveRange(tickets);
        }

        private async Task LogAdminActionAsync(
            string vrstaAkcije,
            string tipRacuna,
            string racunId,
            string racunNaziv)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is not Administrator administrator)
            {
                return;
            }

            _context.AdminAkcije.Add(new AdminAkcija
            {
                AdministratorId = administrator.Id,
                AdministratorNaziv =
                    $"{administrator.Ime} {administrator.Prezime}",
                VrstaAkcije = vrstaAkcije,
                TipRacuna = tipRacuna,
                RacunId = racunId,
                RacunNaziv = racunNaziv,
                DatumAkcije = DateTime.Now
            });
        }

        private bool LjekarExists(string id)
        {
            return _context.Ljekari.Any(e => e.Id == id);
        }
    }
}
