using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using TimeForPill.Data;
using TimeForPill.Models;
using TimeForPill.Services;
using TimeForPill.ViewModels;

namespace TimeForPill.Controllers
{
    [Authorize]
    public class PacijentController : Controller
    {
        private static readonly HashSet<string> DozvoljeneEkstenzije =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".gif",
                ".webp"
            };

        private static readonly string[] DaniUSedmici =
        {
            "Ponedjeljak",
            "Utorak",
            "Srijeda",
            "Cetvrtak",
            "Petak",
            "Subota",
            "Nedjelja"
        };

        private const int MaxDozaPoTerminu = 2;
        private const int VremenskiSlotMinuta = 30;
        private const int PrviDnevniSat = 6;
        private const int ZadnjiNedozvoljeniSat = 23;
        private const int MinutaPrijeDozeZaUzimanje = 15;
        private const int MaxBrojOdgodaPoDozi = 2;

        private readonly ApplicationDbContext _context;
        private readonly IDoseWorkflowService _doseWorkflowService;
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<PacijentController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public PacijentController(
            ApplicationDbContext context,
            IDoseWorkflowService doseWorkflowService,
            IEmailService emailService,
            IOptions<EmailSettings> emailSettings,
            IWebHostEnvironment env,
            ILogger<PacijentController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _doseWorkflowService = doseWorkflowService;
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
            _env = env;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Home()
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            await _doseWorkflowService.RefreshMissedDosesAsync();
            await _doseWorkflowService.SendDueReminderEmailsAsync();

            var now = DateTime.Now;
            var today = now.Date;
            var doses = await GetPacijentDozeAsync(pacijent.Id);
            var todayDoses = doses
                .Where(d => GetDoseBusinessDate(d) == today)
                .ToList();
            var activeTherapyIds = doses
                .Where(d => d.Status == StatusDoze.Cekanje)
                .Select(d => d.TerapijaId)
                .Distinct()
                .ToHashSet();
            var activeTodayDoses = todayDoses
                .Where(d => activeTherapyIds.Contains(d.TerapijaId))
                .ToList();
            var dailyStatistics = await GetDailyStatisticsAsync(
                pacijent.Id,
                today);
            var brojUzetihDanas = Math.Max(
                dailyStatistics?.BrojUzetih ?? 0,
                todayDoses.Count(d => d.Status == StatusDoze.Uzeto));
            var brojPropustenihDanas = Math.Max(
                dailyStatistics?.BrojPropustenih ?? 0,
                todayDoses.Count(d => d.Status == StatusDoze.Propusteno));

            var currentDose = doses
                .Where(d =>
                    d.Status == StatusDoze.Cekanje &&
                    d.VrijemePodsjetnika <= now &&
                    d.VrijemeUzimanja <= now.AddMinutes(5) &&
                    (d.OriginalnoVrijemeUzimanja ?? d.VrijemeUzimanja) > now.AddHours(-1))
                .OrderBy(d => d.VrijemeUzimanja)
                .FirstOrDefault();

            var nextDose = doses
                .Where(d =>
                    d.Status == StatusDoze.Cekanje &&
                    d.VrijemeUzimanja >= now)
                .OrderBy(d => d.VrijemeUzimanja)
                .FirstOrDefault();

            var groupedToday = activeTodayDoses
                .Where(d => d.Terapija != null)
                .GroupBy(d => d.Terapija!)
                .Select(g => ToMedicineListItem(g.Key, g.ToList()))
                .OrderBy(i => i.SljedecaDoza)
                .ToList();

            var viewModel = new PatientDashboardViewModel
            {
                Ime = pacijent.Ime,
                BrojLijekovaDanas = activeTodayDoses
                    .Select(d => d.TerapijaId)
                    .Distinct()
                    .Count(),
                BrojUzetihDanas = brojUzetihDanas,
                BrojPropustenihDanas = brojPropustenihDanas,
                ProgresDoSljedecegLijeka =
                    CalculateTakenProgress(
                        brojUzetihDanas,
                        brojPropustenihDanas,
                        activeTodayDoses.Count(d => d.Status == StatusDoze.Cekanje)),
                SljedeciLijekNaziv =
                    GetDoseMedicineName(nextDose),
                SljedeciLijekVrijeme =
                    nextDose?.VrijemeUzimanja.ToString("HH:mm") ?? "-",
                SljedeciLijekVrijemeIso =
                    nextDose?.VrijemeUzimanja.ToString("O"),
                PreostaloDoSljedeceg =
                    nextDose == null ? "-" : FormatRemaining(now, nextDose.VrijemeUzimanja),
                SljedeciLijekSlika =
                    nextDose?.Terapija?.Lijek?.Slika,
                SljedecaDozaId =
                    nextDose?.Id,
                TrenutnaDoza = currentDose == null
                    ? null
                    : new DosePopupViewModel
                    {
                        DozaId = currentDose.Id,
                        NazivLijeka = GetDoseMedicineName(currentDose),
                        VrijemeUzimanja = currentDose.VrijemeUzimanja.ToString("HH:mm"),
                        VrijemeUzimanjaIso = currentDose.VrijemeUzimanja.ToString("O"),
                        Slika = currentDose.Terapija?.Lijek?.Slika
                    },
                DanasnjeTerapije = groupedToday
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> HomeStatus()
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return Unauthorized();
            }

            await _doseWorkflowService.RefreshMissedDosesAsync();
            await _doseWorkflowService.SendDueReminderEmailsAsync();

            var now = DateTime.Now;
            var today = now.Date;
            var doses = await GetPacijentDozeAsync(pacijent.Id);
            var todayDoses = doses
                .Where(d => GetDoseBusinessDate(d) == today)
                .ToList();
            var activeTherapyIds = doses
                .Where(d => d.Status == StatusDoze.Cekanje)
                .Select(d => d.TerapijaId)
                .Distinct()
                .ToHashSet();
            var activeTodayDoses = todayDoses
                .Where(d => activeTherapyIds.Contains(d.TerapijaId))
                .ToList();
            var dailyStatistics = await GetDailyStatisticsAsync(
                pacijent.Id,
                today);
            var brojUzetihDanas = Math.Max(
                dailyStatistics?.BrojUzetih ?? 0,
                todayDoses.Count(d => d.Status == StatusDoze.Uzeto));
            var brojPropustenihDanas = Math.Max(
                dailyStatistics?.BrojPropustenih ?? 0,
                todayDoses.Count(d => d.Status == StatusDoze.Propusteno));

            var nextDose = doses
                .Where(d =>
                    d.Status == StatusDoze.Cekanje &&
                    d.VrijemeUzimanja >= now)
                .OrderBy(d => d.VrijemeUzimanja)
                .FirstOrDefault();

            return Json(new
            {
                brojLijekovaDanas = activeTodayDoses
                    .Select(d => d.TerapijaId)
                    .Distinct()
                    .Count(),
                brojUzetihDanas = brojUzetihDanas,
                brojPropustenihDanas = brojPropustenihDanas,
                progresDoSljedecegLijeka =
                    CalculateTakenProgress(
                        brojUzetihDanas,
                        brojPropustenihDanas,
                        activeTodayDoses.Count(d => d.Status == StatusDoze.Cekanje)),
                sljedeciLijekNaziv = GetDoseMedicineName(nextDose),
                sljedeciLijekVrijeme =
                    nextDose?.VrijemeUzimanja.ToString("HH:mm") ?? "-",
                sljedeciLijekVrijemeIso =
                    nextDose?.VrijemeUzimanja.ToString("O"),
                preostaloDoSljedeceg =
                    nextDose == null
                        ? "-"
                        : FormatRemaining(now, nextDose.VrijemeUzimanja),
                sljedeciLijekSlika = nextDose?.Terapija?.Lijek?.Slika,
                sljedecaDozaId = nextDose?.Id
            });
        }

        public async Task<IActionResult> MojiLijekovi()
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            await _doseWorkflowService.RefreshMissedDosesAsync();

            var terapije = await GetPacijentTerapijeAsync(pacijent.Id);
            var doze = await GetPacijentDozeAsync(pacijent.Id);
            var viewModel = terapije
                .Where(t => doze.Any(d =>
                    d.TerapijaId == t.Id &&
                    d.Status == StatusDoze.Cekanje))
                .OrderBy(t => t.Kraj)
                .Select(t => ToMedicineListItem(
                    t,
                    doze.Where(d => d.TerapijaId == t.Id).ToList()))
                .ToList();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ObrisiLijek(int id)
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var terapija = await _context.Terapije
                .Include(t => t.Lijek)
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.PacijentId == pacijent.Id);

            if (terapija == null)
            {
                return NotFound();
            }

            var doze = await _context.TerapijskeDoze
                .Where(d => d.TerapijaId == terapija.Id)
                .ToListAsync();
            var notifikacije = await _context.Notifikacije
                .Where(n => n.TerapijaId == terapija.Id)
                .ToListAsync();
            var zahtjevi = await _context.Zahtjevi
                .Where(z => z.TerapijaId == terapija.Id)
                .ToListAsync();
            _context.TerapijskeDoze.RemoveRange(doze);
            _context.Notifikacije.RemoveRange(notifikacije);
            _context.Zahtjevi.RemoveRange(zahtjevi);
            _context.Terapije.Remove(terapija);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Terapija je obrisana iz tvoje liste.";

            return RedirectToAction(nameof(MojiLijekovi));
        }

        [HttpGet]
        public async Task<IActionResult> LijekForma(int? id)
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                var firstDose = GetFirstDoseCandidate(DateTime.Now);
                var newModel = new MedicineFormViewModel
                {
                    AutomatskiPocetak = true,
                    Pocetak = firstDose,
                    Kraj = firstDose
                };
                await PopulateMedicineCatalogAsync(newModel);
                return View(newModel);
            }

            var terapija = await _context.Terapije
                .Include(t => t.Lijek)
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.Id == id.Value &&
                    t.PacijentId == pacijent.Id);

            if (terapija == null)
            {
                return NotFound();
            }

            var firstDoseTime = await _context.TerapijskeDoze
                .AsNoTracking()
                .Where(d => d.TerapijaId == terapija.Id)
                .OrderBy(d => d.VrijemeUzimanja)
                .Select(d => (DateTime?)d.VrijemeUzimanja)
                .FirstOrDefaultAsync();

            var model = new MedicineFormViewModel
            {
                TerapijaId = terapija.Id,
                LijekId = terapija.LijekId,
                Naziv = terapija.Lijek?.Naziv ?? terapija.Naziv,
                Kategorija = terapija.Lijek?.Kategorija ?? "Terapija",
                UkupanBrojDoza = terapija.UkupanBrojDoza,
                IntervalSati = terapija.IntervalSati,
                AutomatskiPocetak = false,
                Pocetak = firstDoseTime ?? terapija.Pocetak,
                Kraj = terapija.Kraj,
                PostojecaSlika = terapija.Lijek?.Slika
            };

            await PopulateMedicineCatalogAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LijekForma(MedicineFormViewModel model)
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var selectedLijek = await GetSelectedCatalogMedicineAsync(model);
            if (model.AutomatskiPocetak)
            {
                model.Pocetak = GetFirstDoseCandidate(DateTime.Now);
            }

            if (selectedLijek == null)
            {
                ModelState.AddModelError(
                    nameof(MedicineFormViewModel.LijekId),
                    "Odaberite lijek iz kataloga.");
            }
            else
            {
                model.Naziv = selectedLijek.Naziv;
                model.Kategorija = selectedLijek.Kategorija;
                model.PostojecaSlika = selectedLijek.Slika;

                var activeTherapyExists = await IsMedicineActiveForPatientAsync(
                    pacijent.Id,
                    selectedLijek.Id,
                    model.TerapijaId);
                if (activeTherapyExists)
                {
                    ModelState.AddModelError(
                        nameof(MedicineFormViewModel.LijekId),
                        $"Vec imate aktivnu terapiju za lijek {selectedLijek.Naziv}. Postojecu terapiju mozete samo azurirati iz ekrana Moji lijekovi.");
                }
            }

            if (!model.AutomatskiPocetak &&
                model.Pocetak < DateTime.Now.AddMinutes(-1))
            {
                ModelState.AddModelError(
                    nameof(MedicineFormViewModel.Pocetak),
                    "Vrijeme prve doze ne moze biti u proslosti.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateMedicineCatalogAsync(model);
                return View(model);
            }

            try
            {
                if (model.TerapijaId.HasValue)
                {
                    await UpdateExistingTherapyAsync(
                        model,
                        pacijent.Id,
                        selectedLijek!);
                }
                else
                {
                    await CreateNewTherapyAsync(
                        model,
                        pacijent.Id,
                        selectedLijek!);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Terapija i pojedinacne doze su sacuvane.";
                return RedirectToAction(nameof(MojiLijekovi));
            }
            catch (DoseScheduleException ex)
            {
                ModelState.AddModelError(
                    nameof(MedicineFormViewModel.Pocetak),
                    ex.Message);

                await PopulateMedicineCatalogAsync(model);
                return View(model);
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Terapija nije sacuvana. Provjerite vezu sa bazom i pokusajte ponovo.");

                await PopulateMedicineCatalogAsync(model);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObnovaTerapije()
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await BuildRenewTherapyViewModelAsync(pacijent.Id);
            model.TerapijaId = model.Terapije.FirstOrDefault()?.TerapijaId;
            model.PreostaloDoza =
                model.Terapije.FirstOrDefault()?.PreostaloDoza ?? 0;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ObnovaTerapije(
            RenewTherapyViewModel model)
        {
            var pacijent = await _context.Pacijenti
                .Include(p => p.Ljekar)
                .FirstOrDefaultAsync(p => p.Id == GetCurrentUserId());

            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            model.Terapije = (await BuildRenewTherapyViewModelAsync(pacijent.Id))
                .Terapije;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var terapija = await _context.Terapije
                .Include(t => t.Lijek)
                .FirstOrDefaultAsync(t =>
                    t.Id == model.TerapijaId &&
                    t.PacijentId == pacijent.Id);

            if (terapija == null)
            {
                ModelState.AddModelError(
                    nameof(RenewTherapyViewModel.TerapijaId),
                    "Odabrana terapija nije pronadjena.");

                return View(model);
            }

            if (model.PreostaloDoza == 0)
            {
                model.PreostaloDoza = await _context.TerapijskeDoze
                    .CountAsync(d =>
                        d.TerapijaId == terapija.Id &&
                        d.Status == StatusDoze.Cekanje);
            }

            var nazivLijeka = terapija.Lijek?.Naziv ?? terapija.Naziv;
            var sadrzaj =
                $"Pacijent: {pacijent.Ime} {pacijent.Prezime}\n" +
                $"Lijek: {nazivLijeka}\n" +
                $"Preostalo doza: {model.PreostaloDoza}\n" +
                $"Napomena: {model.Napomena ?? "Nema dodatne napomene."}";

            var zahtjev = new Zahtjev
            {
                Naziv = $"Obnova terapije - {nazivLijeka}",
                Sadrzaj = sadrzaj,
                TerapijaId = terapija.Id,
                Status = StatusZahtjeva.Neobraden,
                DatumKreiranja = DateTime.Now
            };

            _context.Zahtjevi.Add(zahtjev);
            await _context.SaveChangesAsync();

            try
            {
                var doktorEmail = pacijent.Ljekar?.Email;
                if (!string.IsNullOrWhiteSpace(doktorEmail))
                {
                    await _emailService.SendEmailAsync(
                        doktorEmail,
                        "Pacijent zeli obnovu lijeka",
                        BuildDoctorRenewalEmailBody(
                            zahtjev.Id,
                            pacijent,
                            nazivLijeka,
                            model.PreostaloDoza,
                            model.Napomena),
                        isBodyHtml: true);
                }

                TempData["Success"] =
                    "Zahtjev za obnovu lijeka poslan.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Zahtjev je sacuvan, ali email nije poslan.");

                TempData["Success"] =
                    "Zahtjev za obnovu lijeka poslan.";
            }

            return RedirectToAction(nameof(ObnovaTerapije));
        }

        [HttpGet]
        public async Task<IActionResult> Profil()
        {
            var pacijent = await _context.Pacijenti
                .Include(p => p.KontaktOsoba)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == GetCurrentUserId());

            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(new ProfileViewModel
            {
                KorisnikId = pacijent.Id,
                Uloga = KorisnickaUloga.Pacijent,
                Ime = pacijent.Ime,
                Prezime = pacijent.Prezime,
                DatumRodjenja = pacijent.DatumRodjenja,
                Email = pacijent.Email ?? string.Empty,
                PrikaziKontaktOsobu = true,
                KontaktIme = pacijent.KontaktOsoba?.Ime ?? "Kontakt",
                KontaktPrezime = pacijent.KontaktOsoba?.Prezime ?? "Osoba",
                KontaktEmail =
                    pacijent.KontaktOsoba?.Email ?? string.Empty,
                KontaktTelefon =
                    pacijent.KontaktOsoba?.BrojTelefona ?? "061000000"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profil(ProfileViewModel model)
        {
            model.PrikaziKontaktOsobu = true;
            model.Uloga = KorisnickaUloga.Pacijent;

            if (IsSameEmail(model.Email, model.KontaktEmail))
            {
                ModelState.AddModelError(
                    nameof(ProfileViewModel.KontaktEmail),
                    "Email kontakt osobe ne moze biti isti kao email pacijenta.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var pacijent = await _context.Pacijenti
                .Include(p => p.KontaktOsoba)
                .FirstOrDefaultAsync(p => p.Id == GetCurrentUserId());

            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            pacijent.Ime = model.Ime;
            pacijent.Prezime = model.Prezime;
            pacijent.DatumRodjenja = model.DatumRodjenja;
            pacijent.Email = model.Email;
            pacijent.UserName = model.Email;

            pacijent.KontaktOsoba ??= new KontaktOsoba();
            pacijent.KontaktOsoba.Ime = model.KontaktIme;
            pacijent.KontaktOsoba.Prezime = model.KontaktPrezime;
            pacijent.KontaktOsoba.Email = model.KontaktEmail;
            pacijent.KontaktOsoba.BrojTelefona = model.KontaktTelefon;

            await _userManager.UpdateAsync(pacijent);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profil je azuriran.";
            return RedirectToAction(nameof(Profil));
        }

        public async Task<IActionResult> Raspored()
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            await _doseWorkflowService.RefreshMissedDosesAsync();

            var startSedmice = GetStartOfWeek(DateTime.Today);
            var krajSedmice = startSedmice.AddDays(7);
            var aktivneTerapijeIds = await _context.TerapijskeDoze
                .AsNoTracking()
                .Where(d =>
                    d.Terapija != null &&
                    d.Terapija.PacijentId == pacijent.Id &&
                    d.Status == StatusDoze.Cekanje)
                .Select(d => d.TerapijaId)
                .Distinct()
                .ToListAsync();

            var doze = await _context.TerapijskeDoze
                .Include(d => d.Terapija)
                    .ThenInclude(t => t!.Lijek)
                .AsNoTracking()
                .Where(d =>
                    d.Terapija != null &&
                    d.Terapija.PacijentId == pacijent.Id &&
                    aktivneTerapijeIds.Contains(d.TerapijaId) &&
                    d.VrijemeUzimanja >= startSedmice &&
                    d.VrijemeUzimanja < krajSedmice)
                .OrderBy(d => d.VrijemeUzimanja)
                .ToListAsync();

            var sedmica = Enumerable.Range(0, 7)
                .Select(index =>
                {
                    var datum = startSedmice.AddDays(index);
                    return new ScheduleDayViewModel
                    {
                        NazivDana = DaniUSedmici[index],
                        Datum = datum,
                        Terapije = doze
                            .Where(d => d.VrijemeUzimanja.Date == datum)
                            .Select(d => new ScheduleItemViewModel
                            {
                                Vrijeme = d.VrijemeUzimanja.ToString("HH:mm"),
                                NazivLijeka = GetDoseMedicineName(d),
                                Status = d.Status,
                                Slika = d.Terapija?.Lijek?.Slika
                            })
                            .ToList()
                    };
                })
                .ToList();

            return View(sedmica);
        }

        public async Task<IActionResult> Historija(string period = "dnevna")
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            await _doseWorkflowService.RefreshMissedDosesAsync();

            var today = DateTime.Today;
            var start = period switch
            {
                "godisnja" => new DateTime(today.Year, 1, 1),
                "mjesecna" => new DateTime(today.Year, today.Month, 1),
                _ => today
            };

            var statistics = await _context.PacijentDnevneStatistike
                .AsNoTracking()
                .Where(s =>
                    s.PacijentId == pacijent.Id &&
                    s.Datum >= start &&
                    s.Datum <= today)
                .ToListAsync();

            var model = new HistoryViewModel
            {
                Period = period,
                Uzeto = statistics.Sum(s => s.BrojUzetih),
                NijeUzeto = statistics.Sum(s => s.BrojPropustenih)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Uzmi(int id)
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            await _doseWorkflowService.RefreshMissedDosesAsync();

            var doza = await _context.TerapijskeDoze
                .Include(d => d.Terapija)
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.Terapija != null &&
                    d.Terapija.PacijentId == pacijent.Id);

            if (doza == null)
            {
                return NotFound();
            }

            if (doza.Status != StatusDoze.Cekanje)
            {
                TempData["Error"] =
                    "Ova doza vise nije u statusu cekanja i ne moze se oznaciti kao uzeta.";
                return RedirectToAction(nameof(Home));
            }

            var now = DateTime.Now;
            var earliestAllowed = doza.VrijemeUzimanja
                .AddMinutes(-MinutaPrijeDozeZaUzimanje);
            if (now < earliestAllowed)
            {
                TempData["Error"] =
                    "Doza se moze oznaciti kao uzeta tek 15 minuta prije zakazanog termina.";
                return RedirectToAction(nameof(Home));
            }

            doza.Status = StatusDoze.Uzeto;
            doza.VrijemeEvidentiranja = now;
            await IncrementDailyStatisticAsync(doza, StatusDoze.Uzeto);
            await _context.SaveChangesAsync();
            await UpdateTerapijaStatusAsync(doza.TerapijaId);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Jedna doza je oznacena kao uzeta.";
            if (await ShouldAskForSideEffectsAsync(doza.TerapijaId, pacijent.Id))
            {
                return RedirectToAction(
                    nameof(PrijaviNuspojavu),
                    new { id = doza.TerapijaId, zavrsena = true });
            }

            return RedirectToAction(nameof(Home));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Odgodi(int id)
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            await _doseWorkflowService.RefreshMissedDosesAsync();

            var doza = await _context.TerapijskeDoze
                .Include(d => d.Terapija)
                    .ThenInclude(t => t!.Lijek)
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.Terapija != null &&
                    d.Terapija.PacijentId == pacijent.Id);

            if (doza == null)
            {
                return NotFound();
            }

            if (doza.Status != StatusDoze.Cekanje)
            {
                TempData["Error"] =
                    "Ova doza vise nije u statusu cekanja i ne moze se odgoditi.";
                return RedirectToAction(nameof(Home));
            }

            doza.BrojOdgoda++;
            var naziv = GetDoseMedicineName(doza);
            if (doza.BrojOdgoda >= MaxBrojOdgodaPoDozi)
            {
                doza.Status = StatusDoze.Propusteno;
                doza.VrijemeEvidentiranja = DateTime.Now;
                await IncrementDailyStatisticAsync(doza, StatusDoze.Propusteno);
                await _context.SaveChangesAsync();
                await UpdateTerapijaStatusAsync(doza.TerapijaId);

                _context.Notifikacije.Add(new Notifikacija
                {
                    Naziv = $"Propustena doza - {naziv}",
                    Poruka =
                        $"Doza lijeka {naziv} oznacena je kao propustena jer je odgodjena dva puta.",
                    TerapijaId = doza.TerapijaId
                });

                await _context.SaveChangesAsync();
                await _doseWorkflowService.RefreshMissedDosesAsync();
                TempData["Success"] =
                    "Doza je oznacena kao propustena jer je odgodjena dva puta.";
                return RedirectToAction(nameof(Home));
            }

            doza.VrijemeUzimanja = doza.VrijemeUzimanja.AddMinutes(30);
            doza.VrijemePodsjetnika = doza.VrijemePodsjetnika.AddMinutes(30);
            doza.EmailPodsjetnikPoslan = false;

            if (doza.Terapija != null)
            {
                var therapyDoseTimes = await _context.TerapijskeDoze
                    .Where(d => d.TerapijaId == doza.TerapijaId)
                    .Select(d => new
                    {
                        d.Id,
                        d.VrijemeUzimanja
                    })
                    .ToListAsync();

                doza.Terapija.Kraj = therapyDoseTimes
                    .Select(d => d.Id == doza.Id
                        ? doza.VrijemeUzimanja
                        : d.VrijemeUzimanja)
                    .Max();
            }

            _context.Notifikacije.Add(new Notifikacija
            {
                Naziv = $"Odgoda - {naziv}",
                Poruka =
                    $"Samo trenutna doza lijeka {naziv} pomjerena je za 30 minuta.",
                TerapijaId = doza.TerapijaId
            });

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Samo ova doza je odgodjena za 30 minuta.";
            return RedirectToAction(nameof(Home));
        }

        public async Task<IActionResult> Nuspojave()
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await _context.Nuspojave
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

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> PrijaviNuspojavu(
            int id,
            bool zavrsena = false)
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await BuildSideEffectFormAsync(
                id,
                pacijent.Id,
                zavrsena);
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrijaviNuspojavu(
            SideEffectFormViewModel model)
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!model.TerapijaId.HasValue)
            {
                return NotFound();
            }

            var terapija = await _context.Terapije
                .Include(t => t.Lijek)
                .FirstOrDefaultAsync(t =>
                    t.Id == model.TerapijaId.Value &&
                    t.PacijentId == pacijent.Id);
            if (terapija == null)
            {
                return NotFound();
            }

            PopulateSideEffectMedicineData(model, terapija);

            if (model.ZavrsenaTerapija && model.ImaNuspojave == null)
            {
                ModelState.AddModelError(
                    nameof(SideEffectFormViewModel.ImaNuspojave),
                    "Odaberite da li ste imali nuspojave.");
            }

            var shouldSaveSideEffect =
                !model.ZavrsenaTerapija || model.ImaNuspojave == true;
            if (shouldSaveSideEffect &&
                string.IsNullOrWhiteSpace(model.Opis))
            {
                ModelState.AddModelError(
                    nameof(SideEffectFormViewModel.Opis),
                    "Upisite opis nuspojave.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.ZavrsenaTerapija &&
                model.ImaNuspojave == false)
            {
                await SaveNoSideEffectsAnswerAsync(pacijent.Id, terapija);
                TempData["Success"] =
                    "Zabiljezeno je da niste imali nuspojave za ovu terapiju.";
                return RedirectToAction(nameof(MojiLijekovi));
            }

            _context.Nuspojave.Add(new Nuspojava
            {
                PacijentId = pacijent.Id,
                TerapijaId = terapija.Id,
                LijekId = terapija.LijekId,
                NazivLijeka = model.NazivLijeka,
                Kategorija = model.Kategorija,
                Slika = model.Slika,
                Opis = model.Opis?.Trim(),
                BezNuspojava = false,
                DatumPrijave = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Nuspojava je sacuvana.";

            return RedirectToAction(nameof(Nuspojave));
        }

        public async Task<IActionResult> NuspojavaDetalji(int id)
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await _context.Nuspojave
                .AsNoTracking()
                .Where(n =>
                    n.Id == id &&
                    n.PacijentId == pacijent.Id &&
                    !n.BezNuspojava)
                .Select(n => new SideEffectListItemViewModel
                {
                    Id = n.Id,
                    NazivLijeka = n.NazivLijeka,
                    Kategorija = n.Kategorija,
                    Slika = n.Slika,
                    Opis = n.Opis ?? string.Empty,
                    DatumPrijave = n.DatumPrijave
                })
                .FirstOrDefaultAsync();

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ObrisiNuspojavu(int id)
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var nuspojava = await _context.Nuspojave
                .FirstOrDefaultAsync(n =>
                    n.Id == id &&
                    n.PacijentId == pacijent.Id);
            if (nuspojava == null)
            {
                return NotFound();
            }

            _context.Nuspojave.Remove(nuspojava);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Nuspojava je obrisana.";
            return RedirectToAction(nameof(Nuspojave));
        }

        private async Task<Pacijent?> GetCurrentPacijentAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user as Pacijent;
        }

        private string? GetCurrentUserId()
        {
            return _userManager.GetUserId(User);
        }

        private async Task<List<Terapija>> GetPacijentTerapijeAsync(
            string pacijentId)
        {
            return await _context.Terapije
                .Include(t => t.Lijek)
                .AsNoTracking()
                .Where(t => t.PacijentId == pacijentId)
                .OrderBy(t => t.Kraj)
                .ToListAsync();
        }

        private async Task<List<TerapijskaDoza>> GetPacijentDozeAsync(
            string pacijentId)
        {
            return await _context.TerapijskeDoze
                .Include(d => d.Terapija)
                    .ThenInclude(t => t!.Lijek)
                .AsNoTracking()
                .Where(d =>
                    d.Terapija != null &&
                    d.Terapija.PacijentId == pacijentId)
                .OrderBy(d => d.VrijemeUzimanja)
                .ToListAsync();
        }

        private async Task CreateNewTherapyAsync(
            MedicineFormViewModel model,
            string pacijentId,
            Lijek lijek)
        {
            var occupiedDoseTimes = await GetOccupiedDoseTimesAsync(
                pacijentId,
                excludedTerapijaId: null);
            var schedule = GenerateDoseSchedule(
                GetFirstDoseCandidateFromModel(model),
                model.UkupanBrojDoza,
                model.IntervalSati,
                occupiedDoseTimes,
                model.AutomatskiPocetak);
            var start = schedule.First();
            var end = schedule.Last();

            var terapija = new Terapija
            {
                Naziv = lijek.Naziv,
                Pocetak = start,
                Kraj = end,
                DnevnaDoza = CalculateLegacyDailyDose(model.IntervalSati),
                UkupanBrojDoza = model.UkupanBrojDoza,
                BrojDozaPoObnovi = model.UkupanBrojDoza,
                IntervalSati = model.IntervalSati,
                Status = StatusTerapije.Cekanje,
                PacijentId = pacijentId,
                LijekId = lijek.Id
            };

            _context.Terapije.Add(terapija);
            AddGeneratedDosesToContext(
                terapija,
                schedule);
        }

        private async Task UpdateExistingTherapyAsync(
            MedicineFormViewModel model,
            string pacijentId,
            Lijek lijek)
        {
            var terapija = await _context.Terapije
                .Include(t => t.Lijek)
                .FirstOrDefaultAsync(t =>
                    t.Id == model.TerapijaId &&
                    t.PacijentId == pacijentId);

            if (terapija == null)
            {
                throw new DbUpdateException(
                    "Terapija nije pronadjena za trenutnog pacijenta.");
            }

            var existingDoses = await _context.TerapijskeDoze
                .Where(d => d.TerapijaId == terapija.Id)
                .ToListAsync();

            _context.TerapijskeDoze.RemoveRange(existingDoses);

            var occupiedDoseTimes = await GetOccupiedDoseTimesAsync(
                pacijentId,
                terapija.Id);
            var schedule = GenerateDoseSchedule(
                GetFirstDoseCandidateFromModel(model),
                model.UkupanBrojDoza,
                model.IntervalSati,
                occupiedDoseTimes,
                model.AutomatskiPocetak);
            var start = schedule.First();
            var end = schedule.Last();

            terapija.Naziv = lijek.Naziv;
            terapija.Pocetak = start;
            terapija.Kraj = end;
            terapija.DnevnaDoza = CalculateLegacyDailyDose(model.IntervalSati);
            terapija.UkupanBrojDoza = model.UkupanBrojDoza;
            terapija.BrojDozaPoObnovi = model.UkupanBrojDoza;
            terapija.IntervalSati = model.IntervalSati;
            terapija.Status = StatusTerapije.Cekanje;
            terapija.LijekId = lijek.Id;

            AddGeneratedDosesToContext(
                terapija,
                schedule);
        }

        private void AddGeneratedDosesToContext(
            Terapija terapija,
            IReadOnlyList<DateTime> schedule)
        {
            for (var index = 0; index < schedule.Count; index++)
            {
                var scheduledAt = schedule[index];
                _context.TerapijskeDoze.Add(new TerapijskaDoza
                {
                    Terapija = terapija,
                    RedniBroj = index + 1,
                    VrijemeUzimanja = scheduledAt,
                    OriginalnoVrijemeUzimanja = scheduledAt,
                    VrijemePodsjetnika = scheduledAt.AddMinutes(-5),
                    Status = StatusDoze.Cekanje
                });
            }
        }

        private async Task<List<DateTime>> GetOccupiedDoseTimesAsync(
            string pacijentId,
            int? excludedTerapijaId)
        {
            return await _context.TerapijskeDoze
                .AsNoTracking()
                .Where(d =>
                    d.Terapija != null &&
                    d.Terapija.PacijentId == pacijentId &&
                    d.Status == StatusDoze.Cekanje &&
                    (!excludedTerapijaId.HasValue ||
                        d.TerapijaId != excludedTerapijaId.Value))
                .Select(d => d.VrijemeUzimanja)
                .ToListAsync();
        }

        private static IReadOnlyList<DateTime> GenerateDoseSchedule(
            DateTime firstCandidate,
            int totalDoses,
            int intervalHours,
            IEnumerable<DateTime> occupiedDoseTimes,
            bool allowAutoReschedule)
        {
            if (!allowAutoReschedule)
            {
                return GenerateExactDoseSchedule(
                    firstCandidate,
                    totalDoses,
                    intervalHours,
                    occupiedDoseTimes);
            }

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

        private static IReadOnlyList<DateTime> GenerateExactDoseSchedule(
            DateTime firstCandidate,
            int totalDoses,
            int intervalHours,
            IEnumerable<DateTime> occupiedDoseTimes)
        {
            var schedule = new List<DateTime>();
            var occupancy = occupiedDoseTimes
                .Select(NormalizeDoseTime)
                .GroupBy(time => time)
                .ToDictionary(group => group.Key, group => group.Count());
            var current = NormalizeDoseTime(firstCandidate);

            for (var index = 0; index < totalDoses; index++)
            {
                if (index > 0)
                {
                    current = GetNextExactAllowedDoseTime(
                        current,
                        intervalHours);
                }

                if (!IsAllowedExactDoseTime(current))
                {
                    throw new DoseScheduleException(
                        $"Odabrano vrijeme {current:dd.MM.yyyy HH:mm} je u periodu kada se terapije ne zakazuju. Odaberite termin izmedju 06:00 i 22:59.");
                }

                var currentOccupancy = GetOccupancyCount(occupancy, current);
                if (currentOccupancy >= MaxDozaPoTerminu)
                {
                    throw new DoseScheduleException(
                        $"Termin {current:dd.MM.yyyy HH:mm} vec ima dvije zakazane doze. Odaberite drugo vrijeme pocetka terapije.");
                }

                schedule.Add(current);
                occupancy[current] = currentOccupancy + 1;
            }

            return schedule;
        }

        private static DateTime GetNextExactAllowedDoseTime(
            DateTime previous,
            int intervalHours)
        {
            var current = previous.AddHours(intervalHours);
            for (var attempt = 0; attempt < 24 * 366; attempt++)
            {
                if (IsAllowedExactDoseTime(current))
                {
                    return current;
                }

                current = current.AddHours(intervalHours);
            }

            throw new DoseScheduleException(
                "Nije pronadjen dozvoljen termin za terapiju. Promijenite pocetak terapije ili interval uzimanja.");
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
                "Nije pronadjen slobodan termin za terapiju.");
        }

        private static DateTime GetFirstDoseCandidate(DateTime now)
        {
            var nextSlot = ToDoseSlot(now)
                .AddMinutes(VremenskiSlotMinuta);

            return MoveToAllowedDoseTime(nextSlot);
        }

        private static DateTime GetFirstDoseCandidateFromModel(
            MedicineFormViewModel model)
        {
            if (model.AutomatskiPocetak)
            {
                return GetFirstDoseCandidate(DateTime.Now);
            }

            return model.Pocetak;
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

        private static bool IsAllowedExactDoseTime(DateTime candidate)
        {
            return candidate.Hour >= PrviDnevniSat &&
                candidate.Hour < ZadnjiNedozvoljeniSat;
        }

        private static DateTime NormalizeDoseTime(DateTime value)
        {
            return new DateTime(
                value.Year,
                value.Month,
                value.Day,
                value.Hour,
                value.Minute,
                0);
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

        private async Task<RenewTherapyViewModel> BuildRenewTherapyViewModelAsync(
            string pacijentId)
        {
            var terapije = await GetPacijentTerapijeAsync(pacijentId);
            var pendingCounts = await _context.TerapijskeDoze
                .Where(d =>
                    d.Terapija != null &&
                    d.Terapija.PacijentId == pacijentId &&
                    d.Status == StatusDoze.Cekanje)
                .GroupBy(d => d.TerapijaId)
                .Select(g => new
                {
                    TerapijaId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(g => g.TerapijaId, g => g.Count);

            return new RenewTherapyViewModel
            {
                Terapije = terapije
                    .Where(t => pendingCounts.GetValueOrDefault(t.Id) > 0)
                    .Select(t => new TherapyOptionViewModel
                    {
                        TerapijaId = t.Id,
                        Naziv = t.Lijek?.Naziv ?? t.Naziv,
                        PreostaloDoza = pendingCounts.GetValueOrDefault(t.Id)
                    })
                    .ToList()
            };
        }

        private async Task PopulateMedicineCatalogAsync(
            MedicineFormViewModel model)
        {
            var lijekovi = await _context.Lijekovi
                .AsNoTracking()
                .OrderBy(l => l.Naziv)
                .Select(l => new MedicineCatalogOptionViewModel
                {
                    Id = l.Id,
                    Naziv = l.Naziv,
                    Kategorija = l.Kategorija,
                    Slika = l.Slika
                })
                .ToListAsync();

            model.DostupniLijekovi = lijekovi;

            var selected = lijekovi
                .FirstOrDefault(l => l.Id == model.LijekId);

            if (selected != null)
            {
                model.Naziv = selected.Naziv;
                model.Kategorija = selected.Kategorija;
                model.PostojecaSlika = selected.Slika;
            }
        }

        private async Task<Lijek?> GetSelectedCatalogMedicineAsync(
            MedicineFormViewModel model)
        {
            if (!model.LijekId.HasValue)
            {
                return null;
            }

            return await _context.Lijekovi
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == model.LijekId.Value);
        }

        private async Task<bool> IsMedicineActiveForPatientAsync(
            string pacijentId,
            int lijekId,
            int? excludedTerapijaId)
        {
            return await _context.TerapijskeDoze
                .AsNoTracking()
                .AnyAsync(d =>
                    d.Terapija != null &&
                    d.Terapija.PacijentId == pacijentId &&
                    d.Terapija.LijekId == lijekId &&
                    d.Status == StatusDoze.Cekanje &&
                    (!excludedTerapijaId.HasValue ||
                        d.TerapijaId != excludedTerapijaId.Value));
        }

        private async Task<bool> ShouldAskForSideEffectsAsync(
            int terapijaId,
            string pacijentId)
        {
            var allDosesTaken = await _context.TerapijskeDoze
                .AsNoTracking()
                .Where(d => d.TerapijaId == terapijaId)
                .AllAsync(d => d.Status == StatusDoze.Uzeto);
            if (!allDosesTaken)
            {
                return false;
            }

            return !await _context.Nuspojave
                .AsNoTracking()
                .AnyAsync(n =>
                    n.PacijentId == pacijentId &&
                    n.TerapijaId == terapijaId);
        }

        private async Task<SideEffectFormViewModel?> BuildSideEffectFormAsync(
            int terapijaId,
            string pacijentId,
            bool zavrsenaTerapija)
        {
            var terapija = await _context.Terapije
                .Include(t => t.Lijek)
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.Id == terapijaId &&
                    t.PacijentId == pacijentId);
            if (terapija == null)
            {
                return null;
            }

            var model = new SideEffectFormViewModel
            {
                TerapijaId = terapija.Id,
                ZavrsenaTerapija = zavrsenaTerapija
            };

            PopulateSideEffectMedicineData(model, terapija);
            return model;
        }

        private static void PopulateSideEffectMedicineData(
            SideEffectFormViewModel model,
            Terapija terapija)
        {
            model.TerapijaId = terapija.Id;
            model.LijekId = terapija.LijekId;
            model.NazivLijeka = terapija.Lijek?.Naziv ?? terapija.Naziv;
            model.Kategorija = terapija.Lijek?.Kategorija ?? "Terapija";
            model.Slika = terapija.Lijek?.Slika;
        }

        private async Task SaveNoSideEffectsAnswerAsync(
            string pacijentId,
            Terapija terapija)
        {
            var existingAnswer = await _context.Nuspojave
                .FirstOrDefaultAsync(n =>
                    n.PacijentId == pacijentId &&
                    n.TerapijaId == terapija.Id &&
                    n.BezNuspojava);
            if (existingAnswer != null)
            {
                return;
            }

            _context.Nuspojave.Add(new Nuspojava
            {
                PacijentId = pacijentId,
                TerapijaId = terapija.Id,
                LijekId = terapija.LijekId,
                NazivLijeka = terapija.Lijek?.Naziv ?? terapija.Naziv,
                Kategorija = terapija.Lijek?.Kategorija ?? "Terapija",
                Slika = terapija.Lijek?.Slika,
                BezNuspojava = true,
                DatumPrijave = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        private string BuildDoctorRenewalEmailBody(
            int zahtjevId,
            Pacijent pacijent,
            string nazivLijeka,
            int preostaloDoza,
            string? napomena)
        {
            var requestUrl =
                $"{EmailSettings.PublicAppUrl}/Ljekar/ZahtjevDetalji/{zahtjevId}";
            var encodedUrl = WebUtility.HtmlEncode(requestUrl);
            var encodedPatient = WebUtility.HtmlEncode(
                $"{pacijent.Ime} {pacijent.Prezime}".Trim());
            var encodedMedicine = WebUtility.HtmlEncode(nazivLijeka);
            var encodedNote = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(napomena)
                    ? "Nema dodatne napomene."
                    : napomena);

            return
                $"<p>Pacijent <strong>{encodedPatient}</strong> zeli obnovu lijeka <strong>{encodedMedicine}</strong>.</p>" +
                $"<p>Preostalo doza: <strong>{preostaloDoza}</strong></p>" +
                $"<p>Napomena: {encodedNote}</p>" +
                $"<p><a href=\"{encodedUrl}\" target=\"_blank\" rel=\"noopener\">Otvori zahtjev u TimeForPill aplikaciji</a></p>";
        }

        private async Task<string?> TrySaveImageAsync(IFormFile? slikaFile)
        {
            if (slikaFile == null || slikaFile.Length == 0)
            {
                return null;
            }

            var extension = Path.GetExtension(slikaFile.FileName);
            if (!DozvoljeneEkstenzije.Contains(extension))
            {
                ModelState.AddModelError(
                    nameof(MedicineFormViewModel.SlikaFile),
                    "Dozvoljene su samo JPG, PNG, GIF i WEBP slike.");
                return null;
            }

            if (slikaFile.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    nameof(MedicineFormViewModel.SlikaFile),
                    "Slika moze biti velika najvise 2 MB.");
                return null;
            }

            var uploads = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(uploads);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploads, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await slikaFile.CopyToAsync(stream);

            return $"/images/{fileName}";
        }

        private static MedicineListItemViewModel ToMedicineListItem(
            Terapija terapija,
            IReadOnlyList<TerapijskaDoza> doze)
        {
            var next = doze
                .Where(d => d.Status == StatusDoze.Cekanje)
                .OrderBy(d => d.VrijemeUzimanja)
                .FirstOrDefault();
            var status = doze.Any(d => d.Status == StatusDoze.Propusteno)
                ? StatusDoze.Propusteno
                : next?.Status ?? StatusDoze.Uzeto;

            return new MedicineListItemViewModel
            {
                TerapijaId = terapija.Id,
                LijekId = terapija.LijekId,
                Naziv = terapija.Lijek?.Naziv ?? terapija.Naziv,
                Kategorija = terapija.Lijek?.Kategorija ?? "Terapija",
                DnevnaDoza = terapija.DnevnaDoza,
                UkupanBrojDoza = terapija.UkupanBrojDoza,
                IntervalSati = terapija.IntervalSati,
                UzeteDoze = doze.Count(d => d.Status == StatusDoze.Uzeto),
                PropusteneDoze = doze.Count(d => d.Status == StatusDoze.Propusteno),
                CekajuceDoze = doze.Count(d => d.Status == StatusDoze.Cekanje),
                Pocetak = terapija.Pocetak,
                Kraj = terapija.Kraj,
                Status = status,
                Slika = terapija.Lijek?.Slika,
                SljedecaDoza = next?.VrijemeUzimanja.ToString("HH:mm") ?? "-",
                PreostaloDoSljedeceDoze = next == null
                    ? "-"
                    : FormatRemaining(DateTime.Now, next.VrijemeUzimanja)
            };
        }

        private async Task UpdateTerapijaStatusAsync(int terapijaId)
        {
            var terapija = await _context.Terapije
                .FirstOrDefaultAsync(t => t.Id == terapijaId);

            if (terapija == null)
            {
                return;
            }

            var statuses = await _context.TerapijskeDoze
                .Where(d => d.TerapijaId == terapijaId)
                .Select(d => d.Status)
                .ToListAsync();

            if (statuses.Count == 0)
            {
                terapija.Status = StatusTerapije.Cekanje;
                return;
            }

            if (statuses.All(s => s == StatusDoze.Uzeto))
            {
                terapija.Status = StatusTerapije.Uzeto;
            }
            else if (!statuses.Any(s => s == StatusDoze.Cekanje) &&
                statuses.Any(s => s == StatusDoze.Propusteno))
            {
                terapija.Status = StatusTerapije.Propusteno;
            }
            else
            {
                terapija.Status = StatusTerapije.Cekanje;
            }
        }

        private static string GetDoseMedicineName(TerapijskaDoza? doza)
        {
            return doza?.Terapija?.Lijek?.Naziv ??
                doza?.Terapija?.Naziv ??
                "Nema aktivne terapije";
        }

        private static DateTime GetDoseBusinessDate(TerapijskaDoza doza)
        {
            return (doza.OriginalnoVrijemeUzimanja ?? doza.VrijemeUzimanja).Date;
        }

        private static bool IsSameEmail(string? email, string? contactEmail)
        {
            return string.Equals(
                email?.Trim(),
                contactEmail?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private async Task<PacijentDnevnaStatistika?> GetDailyStatisticsAsync(
            string pacijentId,
            DateTime datum)
        {
            return await _context.PacijentDnevneStatistike
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.PacijentId == pacijentId &&
                    s.Datum == datum.Date);
        }

        private async Task IncrementDailyStatisticAsync(
            TerapijskaDoza doza,
            StatusDoze status)
        {
            var pacijentId = doza.Terapija?.PacijentId;
            if (string.IsNullOrWhiteSpace(pacijentId))
            {
                return;
            }

            var datum = GetDoseBusinessDate(doza);
            var statistic = await _context.PacijentDnevneStatistike
                .FirstOrDefaultAsync(s =>
                    s.PacijentId == pacijentId &&
                    s.Datum == datum);

            if (statistic == null)
            {
                statistic = new PacijentDnevnaStatistika
                {
                    PacijentId = pacijentId,
                    Datum = datum
                };

                _context.PacijentDnevneStatistike.Add(statistic);
            }

            if (status == StatusDoze.Uzeto)
            {
                statistic.BrojUzetih++;
            }
            else if (status == StatusDoze.Propusteno)
            {
                statistic.BrojPropustenih++;
            }
        }

        private static int CalculateTakenProgress(
            int brojUzetih,
            int brojPropustenih,
            int brojCekajucih)
        {
            var total = brojUzetih + brojPropustenih + brojCekajucih;
            if (total == 0)
            {
                return 0;
            }

            return (int)Math.Round(
                (double)brojUzetih / total * 100);
        }

        private static DateTime CalculateTherapyEnd(
            DateTime start,
            int totalDoses,
            int intervalHours)
        {
            return start.AddHours(intervalHours * Math.Max(0, totalDoses - 1));
        }

        private static int CalculateLegacyDailyDose(int intervalHours)
        {
            return Math.Clamp(
                (int)Math.Round(24d / intervalHours),
                1,
                20);
        }

        private static string FormatRemaining(DateTime now, DateTime nextDose)
        {
            var remaining = nextDose - now;
            if (remaining.TotalMinutes < 1)
            {
                return "manje od minute";
            }

            return $"{(int)remaining.TotalHours}h {remaining.Minutes}min";
        }

        private static DateTime GetStartOfWeek(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }

        private sealed class DoseScheduleException : Exception
        {
            public DoseScheduleException(string message)
                : base(message)
            {
            }
        }
    }
}
