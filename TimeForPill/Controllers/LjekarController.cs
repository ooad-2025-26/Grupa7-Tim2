using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;
using TimeForPill.Services;
using TimeForPill.ViewModels;

namespace TimeForPill.Controllers
{
    [Authorize]
    public class LjekarController : Controller
    {
        private const int MaxDozaPoTerminu = 2;
        private const int VremenskiSlotMinuta = 30;
        private const int PrviDnevniSat = 6;
        private const int ZadnjiNedozvoljeniSat = 23;

        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<LjekarController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public LjekarController(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<LjekarController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
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
                .Where(z => z.DatumKreiranja >= GetStartOfCurrentYear() &&
                    z.DatumKreiranja < GetStartOfNextYear())
                .ToListAsync();

            var model = new DoctorDashboardViewModel
            {
                Ime = ljekar.Ime,
                BrojZahtjeva = zahtjevi.Count,
                BrojNeobradjenihZahtjeva =
                    zahtjevi.Count(z => z.Status == StatusZahtjeva.Neobraden),
                BrojObradjenihZahtjeva =
                    zahtjevi.Count(z => z.Status != StatusZahtjeva.Neobraden),
                ZadnjaCetiriPotvrdjena = zahtjevi
                    .Where(z => z.Status == StatusZahtjeva.Obraden)
                    .OrderByDescending(z => z.DatumKreiranja)
                    .ThenByDescending(z => z.Id)
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
                .Where(z => z.DatumKreiranja >= GetStartOfCurrentYear() &&
                    z.DatumKreiranja < GetStartOfNextYear())
                .OrderByDescending(z => z.DatumKreiranja)
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

        public async Task<IActionResult> PacijentNuspojave(string id)
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

            var nuspojave = await _context.Nuspojave
                .AsNoTracking()
                .Where(n =>
                    n.PacijentId == pacijent.Id &&
                    !n.BezNuspojava)
                .OrderByDescending(n => n.DatumPrijave)
                .ThenByDescending(n => n.Id)
                .Select(n => new SideEffectListItemViewModel
                {
                    Id = n.Id,
                    NazivLijeka = n.NazivLijeka,
                    Kategorija = n.Kategorija,
                    Slika = n.Slika,
                    Opis = n.Opis ?? string.Empty,
                    DatumPrijave = n.DatumPrijave
                })
                .ToListAsync();

            var model = new DoctorPatientSideEffectsViewModel
            {
                PacijentId = pacijent.Id,
                Pacijent = $"{pacijent.Ime} {pacijent.Prezime}",
                Nuspojave = nuspojave
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
                Sadrzaj = RemoveRenewalDoseLine(zahtjev.Sadrzaj),
                DatumKreiranja = zahtjev.DatumKreiranja,
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

            var previousStatus = zahtjev.Status;
            if (status == StatusZahtjeva.Obraden &&
                previousStatus == StatusZahtjeva.Neobraden &&
                zahtjev.Terapija != null)
            {
                await ExtendTherapyAtEndAsync(zahtjev);
            }

            zahtjev.Status = status;
            await _context.SaveChangesAsync();
            if (previousStatus != status)
            {
                await NotifyPatientAboutRequestDecisionAsync(zahtjev, status);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(ZahtjevDetalji), new { id });
        }

        private async Task NotifyPatientAboutRequestDecisionAsync(
            Zahtjev zahtjev,
            StatusZahtjeva status)
        {
            var patientEmail = zahtjev.Terapija?.Pacijent?.Email;
            if (string.IsNullOrWhiteSpace(patientEmail))
            {
                return;
            }

            var medicineName = GetMedicineName(zahtjev);
            var decisionText = status == StatusZahtjeva.Obraden
                ? "potvrdio"
                : "odbio";
            var subject = "Obavijest o zahtjevu za obnovu terapije";
            var body =
                $"Doktor je {decisionText} zahtjev za obnovu lijeka {medicineName}.";

            try
            {
                await _emailService.SendEmailAsync(
                    patientEmail,
                    subject,
                    body);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Email pacijentu nije poslan nakon obrade zahtjeva {ZahtjevId}.",
                    zahtjev.Id);
            }
        }

        private async Task ExtendTherapyAtEndAsync(Zahtjev zahtjev)
        {
            var terapija = zahtjev.Terapija;
            if (terapija == null)
            {
                return;
            }

            var intervalHours = terapija.IntervalSati <= 0
                ? 24
                : terapija.IntervalSati;

            var existingDoses = await _context.TerapijskeDoze
                .Where(d => d.TerapijaId == terapija.Id)
                .OrderBy(d => d.VrijemeUzimanja)
                .ToListAsync();
            var currentPendingDoseCount =
                existingDoses.Count(d => d.Status == StatusDoze.Cekanje);
            var renewalDoseCount = GetRenewalDoseCount(terapija);

            var lastDoseTime = existingDoses.Count == 0
                ? terapija.Kraj
                : existingDoses.Max(d => d.VrijemeUzimanja);
            var nextRedniBroj = existingDoses.Count == 0
                ? 1
                : existingDoses.Max(d => d.RedniBroj) + 1;

            var occupiedDoseTimes = await GetPatientOccupiedDoseTimesAsync(
                terapija.PacijentId);
            var schedule = GenerateRenewalDoseSchedule(
                lastDoseTime.AddHours(intervalHours),
                renewalDoseCount,
                intervalHours,
                occupiedDoseTimes);

            for (var index = 0; index < schedule.Count; index++)
            {
                var scheduledAt = schedule[index];

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

            if (terapija.BrojDozaPoObnovi <= 0)
            {
                terapija.BrojDozaPoObnovi = renewalDoseCount;
            }

            terapija.UkupanBrojDoza =
                currentPendingDoseCount + renewalDoseCount;
            terapija.Status = StatusTerapije.Cekanje;
        }

        private static int GetRenewalDoseCount(Terapija terapija)
        {
            return terapija.BrojDozaPoObnovi > 0
                ? terapija.BrojDozaPoObnovi
                : terapija.UkupanBrojDoza <= 0
                ? 1
                : terapija.UkupanBrojDoza;
        }

        private async Task<List<DateTime>> GetPatientOccupiedDoseTimesAsync(
            string? pacijentId)
        {
            if (string.IsNullOrWhiteSpace(pacijentId))
            {
                return new List<DateTime>();
            }

            return await _context.TerapijskeDoze
                .AsNoTracking()
                .Where(d =>
                    d.Terapija != null &&
                    d.Terapija.PacijentId == pacijentId &&
                    d.Status == StatusDoze.Cekanje)
                .Select(d => d.VrijemeUzimanja)
                .ToListAsync();
        }

        private static IReadOnlyList<DateTime> GenerateRenewalDoseSchedule(
            DateTime firstCandidate,
            int totalDoses,
            int intervalHours,
            IEnumerable<DateTime> occupiedDoseTimes)
        {
            var schedule = new List<DateTime>();
            var occupancy = occupiedDoseTimes
                .Select(ToDoseSlot)
                .GroupBy(slot => slot)
                .ToDictionary(group => group.Key, group => group.Count());
            var candidate = MoveToAllowedDoseTime(firstCandidate);

            for (var index = 0; index < totalDoses; index++)
            {
                var idealTime = index == 0
                    ? candidate
                    : schedule[index - 1].AddHours(intervalHours);
                var scheduledAt = FindNextAvailableDoseSlot(
                    idealTime,
                    occupancy);
                var slot = ToDoseSlot(scheduledAt);

                schedule.Add(scheduledAt);
                occupancy[slot] = GetOccupancyCount(occupancy, slot) + 1;
            }

            return schedule;
        }

        private static DateTime FindNextAvailableDoseSlot(
            DateTime candidate,
            IDictionary<DateTime, int> occupancy)
        {
            var current = MoveToAllowedDoseTime(candidate);

            for (var attempt = 0; attempt < 2 * 24 * 366; attempt++)
            {
                var slot = ToDoseSlot(current);
                if (IsAllowedDoseTime(slot) &&
                    GetOccupancyCount(occupancy, slot) < MaxDozaPoTerminu)
                {
                    return slot;
                }

                current = MoveToAllowedDoseTime(
                    slot.AddMinutes(VremenskiSlotMinuta));
            }

            throw new InvalidOperationException(
                "Nije pronadjen slobodan termin za obnovu terapije.");
        }

        private static int GetOccupancyCount(
            IDictionary<DateTime, int> occupancy,
            DateTime slot)
        {
            return occupancy.TryGetValue(slot, out var count)
                ? count
                : 0;
        }

        private static DateTime MoveToAllowedDoseTime(DateTime candidate)
        {
            var slot = ToDoseSlot(candidate);
            if (slot.Hour < PrviDnevniSat)
            {
                return slot.Date.AddHours(PrviDnevniSat);
            }

            if (slot.Hour >= ZadnjiNedozvoljeniSat)
            {
                return slot.Date.AddDays(1).AddHours(PrviDnevniSat);
            }

            return slot;
        }

        private static bool IsAllowedDoseTime(DateTime candidate)
        {
            return candidate.Hour >= PrviDnevniSat &&
                candidate.Hour < ZadnjiNedozvoljeniSat;
        }

        private static DateTime ToDoseSlot(DateTime value)
        {
            var minute = value.Minute < VremenskiSlotMinuta
                ? 0
                : VremenskiSlotMinuta;

            return new DateTime(
                value.Year,
                value.Month,
                value.Day,
                value.Hour,
                minute,
                0);
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
                DatumKreiranja = zahtjev.DatumKreiranja,
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

        private static string RemoveRenewalDoseLine(string sadrzaj)
        {
            var lines = sadrzaj.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None);

            return string.Join(
                Environment.NewLine,
                lines.Where(line => !line
                    .TrimStart()
                    .StartsWith(
                        "Doza koje se dodaju obnovom:",
                        StringComparison.OrdinalIgnoreCase)));
        }

        private static DateTime GetStartOfCurrentYear()
        {
            var today = DateTime.Today;
            return new DateTime(today.Year, 1, 1);
        }

        private static DateTime GetStartOfNextYear()
        {
            return GetStartOfCurrentYear().AddYears(1);
        }
    }
}
