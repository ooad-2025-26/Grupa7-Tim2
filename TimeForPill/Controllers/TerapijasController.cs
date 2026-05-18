using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill.Controllers
{
    public class TerapijasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TerapijasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Terapijas
        public async Task<IActionResult> Index()
        {
            var terapije = await _context.Terapije
                .Include(t => t.Lijek)
                .Include(t => t.Pacijent)
                .AsNoTracking()
                .OrderByDescending(t => t.Pocetak)
                .ToListAsync();

            return View(terapije);
        }

        // GET: Terapijas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var terapija = await _context.Terapije
                .Include(t => t.Lijek)
                .Include(t => t.Pacijent)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (terapija == null)
            {
                return NotFound();
            }

            return View(terapija);
        }

        // GET: Terapijas/Create
        public async Task<IActionResult> Create()
        {
            await PopulateListsAsync();

            return View(new Terapija
            {
                Pocetak = DateTime.Today,
                Kraj = DateTime.Today.AddDays(7),
                DnevnaDoza = 1,
                Status = StatusTerapije.Cekanje
            });
        }

        // POST: Terapijas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Status,Naziv,Pocetak,Kraj,DnevnaDoza,LijekId,PacijentId,NotifikacijaID")]
        Terapija terapija)
        {
            ValidateTerapijaDates(terapija);
            ValidateTerapijaReferences(terapija);

            if (!ModelState.IsValid)
            {
                await PopulateListsAsync(
                    terapija.LijekId,
                    terapija.PacijentId);

                return View(terapija);
            }

            try
            {
                _context.Add(terapija);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Terapija nije sacuvana. Provjerite odabrani lijek, pacijenta i vezu sa bazom.");

                await PopulateListsAsync(
                    terapija.LijekId,
                    terapija.PacijentId);

                return View(terapija);
            }
        }

        // GET: Terapijas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var terapija = await _context.Terapije.FindAsync(id);

            if (terapija == null)
            {
                return NotFound();
            }

            await PopulateListsAsync(
                terapija.LijekId,
                terapija.PacijentId);

            return View(terapija);
        }

        // POST: Terapijas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,

            [Bind("Id,Status,Naziv,Pocetak,Kraj,DnevnaDoza,LijekId,PacijentId,NotifikacijaID")]
        Terapija terapija)
        {
            if (id != terapija.Id)
            {
                return NotFound();
            }

            ValidateTerapijaDates(terapija);
            ValidateTerapijaReferences(terapija);

            if (!ModelState.IsValid)
            {
                await PopulateListsAsync(
                    terapija.LijekId,
                    terapija.PacijentId);

                return View(terapija);
            }

            try
            {
                _context.Update(terapija);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TerapijaExists(terapija.Id))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Izmjene nisu sacuvane. Provjerite odabrani lijek, pacijenta i vezu sa bazom.");

                await PopulateListsAsync(
                    terapija.LijekId,
                    terapija.PacijentId);

                return View(terapija);
            }
        }

        // GET: Terapijas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var terapija = await _context.Terapije
                .Include(t => t.Lijek)
                .Include(t => t.Pacijent)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (terapija == null)
            {
                return NotFound();
            }

            return View(terapija);
        }

        // POST: Terapijas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var terapija = await _context.Terapije.FindAsync(id);

            if (terapija != null)
            {
                _context.Terapije.Remove(terapija);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateListsAsync(
            int? selectedLijekId = null,
            string? selectedPacijentId = null)
        {
            var lijekovi = await _context.Lijekovi
                .AsNoTracking()
                .OrderBy(l => l.Naziv)
                .Select(l => new
                {
                    l.Id,
                    l.Naziv
                })
                .ToListAsync();

            var pacijenti = await _context.Pacijenti
                .AsNoTracking()
                .OrderBy(p => p.Prezime)
                .ThenBy(p => p.Ime)
                .Select(p => new
                {
                    p.Id,
                    Naziv = p.Ime + " " + p.Prezime
                })
                .ToListAsync();

            ViewData["LijekId"] =
                new SelectList(
                    lijekovi,
                    "Id",
                    "Naziv",
                    selectedLijekId);

            ViewData["PacijentId"] =
                new SelectList(
                    pacijenti,
                    "Id",
                    "Naziv",
                    selectedPacijentId);
        }

        private void ValidateTerapijaDates(
            Terapija terapija)
        {
            if (terapija.Kraj < terapija.Pocetak)
            {
                ModelState.AddModelError(
                    nameof(Terapija.Kraj),
                    "Datum kraja ne moze biti prije datuma pocetka.");
            }
        }

        private void ValidateTerapijaReferences(
            Terapija terapija)
        {
            if (!terapija.LijekId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(Terapija.LijekId),
                    "Odaberite lijek.");
            }

            if (string.IsNullOrEmpty(terapija.PacijentId))
            {
                ModelState.AddModelError(
                    nameof(Terapija.PacijentId),
                    "Odaberite pacijenta.");
            }
        }

        private bool TerapijaExists(int id)
        {
            return _context.Terapije.Any(e => e.Id == id);
        }
    }
}