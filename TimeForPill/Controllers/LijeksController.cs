using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill
{
    [Authorize]
    public class LijeksController : Controller
    {
        private static readonly HashSet<string> DozvoljeneEkstenzije = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp"
        };

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public LijeksController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var access = await RequireCatalogAccessAsync();
            if (access != null)
            {
                return access;
            }

            var lijekovi = await _context.Lijekovi
                .AsNoTracking()
                .OrderBy(l => l.Naziv)
                .ToListAsync();

            return View(lijekovi);
        }

        public async Task<IActionResult> Details(int? id)
        {
            var access = await RequireCatalogAccessAsync();
            if (access != null)
            {
                return access;
            }

            if (id == null)
            {
                return NotFound();
            }

            var lijek = await _context.Lijekovi
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return lijek == null ? NotFound() : View(lijek);
        }

        public async Task<IActionResult> Create()
        {
            var access = await RequireCatalogAccessAsync();
            if (access != null)
            {
                return access;
            }

            return View(new Lijek());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Naziv,Kategorija,Slika")] Lijek lijek, IFormFile? SlikaFile)
        {
            var access = await RequireCatalogAccessAsync();
            if (access != null)
            {
                return access;
            }

            await TryAttachImageAsync(lijek, SlikaFile);

            if (!ModelState.IsValid)
            {
                return View(lijek);
            }

            try
            {
                _context.Add(lijek);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Lijek nije sacuvan. Provjerite vezu sa bazom i pokusajte ponovo.");
                return View(lijek);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            var access = await RequireCatalogAccessAsync();
            if (access != null)
            {
                return access;
            }

            if (id == null)
            {
                return NotFound();
            }

            var lijek = await _context.Lijekovi.FindAsync(id);
            return lijek == null ? NotFound() : View(lijek);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Naziv,Kategorija,Slika")] Lijek lijek, IFormFile? SlikaFile)
        {
            var access = await RequireCatalogAccessAsync();
            if (access != null)
            {
                return access;
            }

            if (id != lijek.Id)
            {
                return NotFound();
            }

            var postojeciLijek = await _context.Lijekovi.FindAsync(id);
            if (postojeciLijek == null)
            {
                return NotFound();
            }

            await TryAttachImageAsync(lijek, SlikaFile);
            if (string.IsNullOrWhiteSpace(lijek.Slika))
            {
                lijek.Slika = postojeciLijek.Slika;
            }

            if (!ModelState.IsValid)
            {
                return View(lijek);
            }

            postojeciLijek.Naziv = lijek.Naziv;
            postojeciLijek.Kategorija = lijek.Kategorija;
            postojeciLijek.Slika = lijek.Slika;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LijekExists(lijek.Id))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Izmjene nisu sacuvane. Provjerite vezu sa bazom i pokusajte ponovo.");
                return View(lijek);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            var access = await RequireCatalogAccessAsync();
            if (access != null)
            {
                return access;
            }

            if (id == null)
            {
                return NotFound();
            }

            var lijek = await _context.Lijekovi
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return lijek == null ? NotFound() : View(lijek);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var access = await RequireCatalogAccessAsync();
            if (access != null)
            {
                return access;
            }

            var lijek = await _context.Lijekovi.FindAsync(id);
            if (lijek != null)
            {
                var koristiSe = await _context.Terapije
                    .AnyAsync(t => t.LijekId == id);

                if (koristiSe)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Lijek se ne moze obrisati jer se koristi u terapijama pacijenata.");

                    return View("Delete", lijek);
                }

                _context.Lijekovi.Remove(lijek);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task TryAttachImageAsync(Lijek lijek, IFormFile? slikaFile)
        {
            if (slikaFile == null || slikaFile.Length == 0)
            {
                if (string.IsNullOrWhiteSpace(lijek.Slika))
                {
                    lijek.Slika = BuildAutomaticImageUrl(lijek.Naziv);
                }

                return;
            }

            var extension = Path.GetExtension(slikaFile.FileName);
            if (!DozvoljeneEkstenzije.Contains(extension))
            {
                ModelState.AddModelError(nameof(Lijek.Slika), "Dozvoljene su samo JPG, PNG, GIF i WEBP slike.");
                return;
            }

            if (slikaFile.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(Lijek.Slika), "Slika moze biti velika najvise 2 MB.");
                return;
            }

            var uploads = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(uploads);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploads, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await slikaFile.CopyToAsync(stream);

            lijek.Slika = $"/images/{fileName}";
        }

        private async Task<IActionResult?> RequireCatalogAccessAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is Ljekar || user is Administrator)
            {
                return null;
            }

            return User.Identity?.IsAuthenticated == true
                ? Forbid()
                : Challenge();
        }

        private static string BuildAutomaticImageUrl(string naziv)
        {
            var text = Uri.EscapeDataString(
                string.IsNullOrWhiteSpace(naziv) ? "Lijek" : naziv.Trim());

            return $"https://placehold.co/600x400/e8f4ef/0f766e/png?text={text}";
        }

        private bool LijekExists(int id)
        {
            return _context.Lijekovi.Any(e => e.Id == id);
        }
    }
}
