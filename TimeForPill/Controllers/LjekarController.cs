using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;
using TimeForPill.ViewModels;

namespace TimeForPill.Controllers
{
    [Authorize]
    public class LjekarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LjekarController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Home()
        {
            var ljekar = await GetCurrentLjekarAsync();
            if (ljekar == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var zahtjevi = await GetLjekarZahtjevi(ljekar.Id)
                .AsNoTracking()
                .ToListAsync();

            var model = new DoctorDashboardViewModel
            {
                BrojZahtjeva = zahtjevi.Count,
                BrojNeobradjenihZahtjeva =
                    zahtjevi.Count(z => z.Status == StatusZahtjeva.Neobraden),
                BrojObradjenihZahtjeva =
                    zahtjevi.Count(z => z.Status != StatusZahtjeva.Neobraden),
                ZadnjaCetiriPotvrdjena = zahtjevi
                    .Where(z => z.Status == StatusZahtjeva.Obraden)
                    .OrderByDescending(z => z.Id)
                    .Take(4)
                    .Select(ToRequestListItem)
                    .ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> Zahtjevi()
        {
            var ljekar = await GetCurrentLjekarAsync();
            if (ljekar == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await GetLjekarZahtjevi(ljekar.Id)
                .AsNoTracking()
                .OrderBy(z => z.Status)
                .ThenByDescending(z => z.Id)
                .Select(z => ToRequestListItem(z))
                .ToListAsync();

            return View(model);
        }

        public async Task<IActionResult> MojiPacijenti()
        {
            var ljekar = await GetCurrentLjekarAsync();
            if (ljekar == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var pacijenti = await _context.Pacijenti
                .AsNoTracking()
                .Where(p => p.LjekarId == ljekar.Id)
                .OrderBy(p => p.Prezime)
                .ThenBy(p => p.Ime)
                .ToListAsync();

            var pacijentIds = pacijenti
                .Select(p => p.Id)
                .ToList();

            var doze = await _context.TerapijskeDoze
                .Include(d => d.Terapija)
                .AsNoTracking()
                .Where(d =>
                    d.Terapija != null &&
                    d.Terapija.PacijentId != null &&
                    pacijentIds.Contains(d.Terapija.PacijentId) &&
                    d.Status == StatusDoze.Cekanje)
                .ToListAsync();

            var model = pacijenti
                .Select(p =>
                {
                    var pacijentDoze = doze
                        .Where(d => d.Terapija?.PacijentId == p.Id)
                        .ToList();

                    return new DoctorPatientListItemViewModel
                    {
                        PacijentId = p.Id,
                        ImePrezime = $"{p.Ime} {p.Prezime}",
                        Email = p.Email ?? string.Empty,
                        BrojAktivnihLijekova = pacijentDoze
                            .Select(d => d.TerapijaId)
                            .Distinct()
                            .Count(),
                        BrojCekajucihDoza = pacijentDoze.Count
                    };
                })
                .ToList();

            return View(model);
        }

        public async Task<IActionResult> PacijentLijekovi(string id)
        {
            var ljekar = await GetCurrentLjekarAsync();
            if (ljekar == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var pacijent = await _context.Pacijenti
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.LjekarId == ljekar.Id);

            if (pacijent == null)
            {
                return NotFound();
            }

            var terapije = await _context.Terapije
                .Include(t => t.Lijek)
                .AsNoTracking()
                .Where(t => t.PacijentId == pacijent.Id)
                .OrderBy(t => t.Kraj)
                .ToListAsync();

            var terapijaIds = terapije.Select(t => t.Id).ToList();
            var doze = await _context.TerapijskeDoze
                .AsNoTracking()
                .Where(d => terapijaIds.Contains(d.TerapijaId))
                .OrderBy(d => d.VrijemeUzimanja)
                .ToListAsync();

            var model = new DoctorPatientMedicationViewModel
            {
                Pacijent = $"{pacijent.Ime} {pacijent.Prezime}",
                Lijekovi = terapije
                    .Select(t =>
                    {
                        var terapijaDoze = doze
                            .Where(d => d.TerapijaId == t.Id)
                            .ToList();
                        var next = terapijaDoze
                            .Where(d => d.Status == StatusDoze.Cekanje)
                            .OrderBy(d => d.VrijemeUzimanja)
                            .FirstOrDefault();

                        return new DoctorMedicationItemViewModel
                        {
                            Naziv = t.Lijek?.Naziv ?? t.Naziv,
                            Kategorija = t.Lijek?.Kategorija ?? "Terapija",
                            UkupanBrojDoza = t.UkupanBrojDoza,
                            IntervalSati = t.IntervalSati,
                            UzeteDoze = terapijaDoze.Count(d => d.Status == StatusDoze.Uzeto),
                            PropusteneDoze = terapijaDoze.Count(d => d.Status == StatusDoze.Propusteno),
                            CekajuceDoze = terapijaDoze.Count(d => d.Status == StatusDoze.Cekanje),
                            SljedecaDoza = next?.VrijemeUzimanja.ToString("dd.MM.yyyy HH:mm") ?? "-",
                            Period = $"{t.Pocetak:dd.MM.yyyy HH:mm} - {t.Kraj:dd.MM.yyyy HH:mm}"
                        };
                    })
                    .Where(l => l.CekajuceDoze > 0)
                    .ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> ZahtjevDetalji(int id)
        {
            var ljekar = await GetCurrentLjekarAsync();
            if (ljekar == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var zahtjev = await GetLjekarZahtjevi(ljekar.Id)
                .AsNoTracking()
                .FirstOrDefaultAsync(z => z.Id == id);

            if (zahtjev == null)
            {
                return NotFound();
            }

            var model = new RequestDetailsViewModel
            {
                Id = zahtjev.Id,
                Naziv = zahtjev.Naziv,
                Sadrzaj = zahtjev.Sadrzaj,
                Status = zahtjev.Status,
                Pacijent = GetPatientName(zahtjev),
                Lijek = GetMedicineName(zahtjev)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Potvrdi(int id)
        {
            return await UpdateZahtjevStatusAsync(
                id,
                StatusZahtjeva.Obraden,
                "Zahtjev je potvrdjen.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Odbij(int id)
        {
            return await UpdateZahtjevStatusAsync(
                id,
                StatusZahtjeva.Odbijen,
                "Zahtjev je odbijen.");
        }

        [HttpGet]
        public async Task<IActionResult> Profil()
        {
            var ljekar = await GetCurrentLjekarAsync();
            if (ljekar == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(new ProfileViewModel
            {
                KorisnikId = ljekar.Id,
                Uloga = KorisnickaUloga.Ljekar,
                Ime = ljekar.Ime,
                Prezime = ljekar.Prezime,
                DatumRodjenja = ljekar.DatumRodjenja,
                Email = ljekar.Email ?? string.Empty,
                Specijalizacija = ljekar.Specijalizacija,
                PrikaziKontaktOsobu = false
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profil(ProfileViewModel model)
        {
            model.Uloga = KorisnickaUloga.Ljekar;
            model.PrikaziKontaktOsobu = false;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ljekar = await _context.Ljekari
                .FirstOrDefaultAsync(l => l.Id == GetCurrentUserId());

            if (ljekar == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ljekar.Ime = model.Ime;
            ljekar.Prezime = model.Prezime;
            ljekar.DatumRodjenja = model.DatumRodjenja;
            ljekar.Email = model.Email;
            ljekar.UserName = model.Email;
            ljekar.Specijalizacija =
                model.Specijalizacija ?? ljekar.Specijalizacija;

            await _userManager.UpdateAsync(ljekar);
            TempData["Success"] = "Profil je azuriran.";

            return RedirectToAction(nameof(Profil));
        }

        private async Task<IActionResult> UpdateZahtjevStatusAsync(
            int id,
            StatusZahtjeva status,
            string message)
        {
            var ljekar = await GetCurrentLjekarAsync();
            if (ljekar == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var zahtjev = await GetLjekarZahtjevi(ljekar.Id)
                .FirstOrDefaultAsync(z => z.Id == id);

            if (zahtjev == null)
            {
                return NotFound();
            }

            if (status == StatusZahtjeva.Obraden &&
                zahtjev.Status == StatusZahtjeva.Neobraden &&
                zahtjev.Terapija != null)
            {
                await ExtendTherapyAtEndAsync(zahtjev.Terapija);
            }

            zahtjev.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = message;
            return RedirectToAction(nameof(ZahtjevDetalji), new { id });
        }

        private async Task ExtendTherapyAtEndAsync(Terapija terapija)
        {
            var intervalHours = terapija.IntervalSati <= 0
                ? 24
                : terapija.IntervalSati;
            var renewalDoseCount = terapija.UkupanBrojDoza <= 0
                ? 1
                : terapija.UkupanBrojDoza;

            var existingDoses = await _context.TerapijskeDoze
                .Where(d => d.TerapijaId == terapija.Id)
                .OrderBy(d => d.VrijemeUzimanja)
                .ToListAsync();

            var lastDoseTime = existingDoses.Count == 0
                ? terapija.Kraj
                : existingDoses.Max(d => d.VrijemeUzimanja);
            var nextRedniBroj = existingDoses.Count == 0
                ? 1
                : existingDoses.Max(d => d.RedniBroj) + 1;

            for (var index = 0; index < renewalDoseCount; index++)
            {
                var scheduledAt = lastDoseTime.AddHours(
                    intervalHours * (index + 1));

                _context.TerapijskeDoze.Add(new TerapijskaDoza
                {
                    TerapijaId = terapija.Id,
                    RedniBroj = nextRedniBroj + index,
                    VrijemeUzimanja = scheduledAt,
                    OriginalnoVrijemeUzimanja = scheduledAt,
                    VrijemePodsjetnika = scheduledAt.AddMinutes(-5),
                    Status = StatusDoze.Cekanje
                });

                terapija.Kraj = scheduledAt;
            }

            terapija.Status = StatusTerapije.Cekanje;
        }

        private async Task<Ljekar?> GetCurrentLjekarAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user as Ljekar;
        }

        private string? GetCurrentUserId()
        {
            return _userManager.GetUserId(User);
        }

        private IQueryable<Zahtjev> GetLjekarZahtjevi(string ljekarId)
        {
            return _context.Zahtjevi
                .Include(z => z.Terapija)
                    .ThenInclude(t => t!.Pacijent)
                .Include(z => z.Terapija)
                    .ThenInclude(t => t!.Lijek)
                .Where(z =>
                    z.Terapija != null &&
                    z.Terapija.Pacijent != null &&
                    z.Terapija.Pacijent.LjekarId == ljekarId);
        }

        private static RequestListItemViewModel ToRequestListItem(
            Zahtjev zahtjev)
        {
            return new RequestListItemViewModel
            {
                Id = zahtjev.Id,
                Naziv = zahtjev.Naziv,
                Status = zahtjev.Status,
                Pacijent = GetPatientName(zahtjev),
                Lijek = GetMedicineName(zahtjev)
            };
        }

        private static string GetPatientName(Zahtjev zahtjev)
        {
            var pacijent = zahtjev.Terapija?.Pacijent;
            return pacijent == null
                ? "Nepoznat pacijent"
                : $"{pacijent.Ime} {pacijent.Prezime}";
        }

        private static string GetMedicineName(Zahtjev zahtjev)
        {
            return zahtjev.Terapija?.Lijek?.Naziv ??
                zahtjev.Terapija?.Naziv ??
                "Nepoznat lijek";
        }
    }
}
