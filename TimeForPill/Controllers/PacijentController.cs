using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

        private const int MaxDozaPoSatu = 2;
        private const int PrviDnevniSat = 6;
        private const int ZadnjiDnevniSat = 22;
        private const int MinutaPrijeDozeZaUzimanje = 15;
        private const int MaxBrojOdgodaPoDozi = 2;

        private readonly ApplicationDbContext _context;
        private readonly IDoseWorkflowService _doseWorkflowService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<PacijentController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public PacijentController(
            ApplicationDbContext context,
            IDoseWorkflowService doseWorkflowService,
            IEmailService emailService,
            IWebHostEnvironment env,
            IOptions<EmailSettings> emailSettings,
            ILogger<PacijentController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _doseWorkflowService = doseWorkflowService;
            _emailService = emailService;
            _env = env;
            _emailSettings = emailSettings.Value;
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

            var groupedToday = todayDoses
                .Where(d => d.Terapija != null)
                .GroupBy(d => d.Terapija!)
                .Select(g => ToMedicineListItem(g.Key, g.ToList()))
                .OrderBy(i => i.SljedecaDoza)
                .ToList();

            var viewModel = new PatientDashboardViewModel
            {
                Ime = pacijent.Ime,
                BrojLijekovaDanas = todayDoses
                    .Select(d => d.TerapijaId)
                    .Distinct()
                    .Count(),
                BrojUzetihDanas =
                    todayDoses.Count(d => d.Status == StatusDoze.Uzeto),
                BrojPropustenihDanas =
                    todayDoses.Count(d => d.Status == StatusDoze.Propusteno),
                ProgresDoSljedecegLijeka =
                    CalculateTakenProgress(todayDoses),
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

            var nextDose = doses
                .Where(d =>
                    d.Status == StatusDoze.Cekanje &&
                    d.VrijemeUzimanja >= now)
                .OrderBy(d => d.VrijemeUzimanja)
                .FirstOrDefault();

            return Json(new
            {
                brojLijekovaDanas = todayDoses
                    .Select(d => d.TerapijaId)
                    .Distinct()
                    .Count(),
                brojUzetihDanas =
                    todayDoses.Count(d => d.Status == StatusDoze.Uzeto),
                brojPropustenihDanas =
                    todayDoses.Count(d => d.Status == StatusDoze.Propusteno),
                progresDoSljedecegLijeka =
                    CalculateTakenProgress(todayDoses),
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
                var newModel = new MedicineFormViewModel();
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

            var model = new MedicineFormViewModel
            {
                TerapijaId = terapija.Id,
                LijekId = terapija.LijekId,
                Naziv = terapija.Lijek?.Naziv ?? terapija.Naziv,
                Kategorija = terapija.Lijek?.Kategorija ?? "Terapija",
                UkupanBrojDoza = terapija.UkupanBrojDoza,
                IntervalSati = terapija.IntervalSati,
                Pocetak = terapija.Pocetak,
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
                Status = StatusZahtjeva.Neobraden
            };

            _context.Zahtjevi.Add(zahtjev);
            await _context.SaveChangesAsync();

            var doktorEmail = pacijent.Ljekar?.Email;
            if (string.IsNullOrWhiteSpace(doktorEmail))
            {
                doktorEmail = _emailSettings.DoctorEmail;
            }

            try
            {
                await _emailService.SendEmailAsync(
                    doktorEmail,
                    zahtjev.Naziv,
                    sadrzaj);

                TempData["Success"] =
                    "Zahtjev je sacuvan i poslan ljekaru.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Zahtjev je sacuvan, ali email nije poslan.");

                TempData["Success"] =
                    "Zahtjev je sacuvan. Email nije poslan jer SMTP nije dostupan.";
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
                Email = pacijent.Email ?? _emailSettings.PatientEmail,
                PrikaziKontaktOsobu = true,
                KontaktIme = pacijent.KontaktOsoba?.Ime ?? "Kontakt",
                KontaktPrezime = pacijent.KontaktOsoba?.Prezime ?? "Osoba",
                KontaktEmail =
                    pacijent.KontaktOsoba?.Email ?? _emailSettings.ContactEmail,
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
            var doze = await _context.TerapijskeDoze
                .Include(d => d.Terapija)
                    .ThenInclude(t => t!.Lijek)
                .AsNoTracking()
                .Where(d =>
                    d.Terapija != null &&
                    d.Terapija.PacijentId == pacijent.Id &&
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

            var doze = await _context.TerapijskeDoze
                .AsNoTracking()
                .Where(d =>
                    d.Terapija != null &&
                    d.Terapija.PacijentId == pacijent.Id)
                .ToListAsync();
            doze = doze
                .Where(d =>
                    GetDoseBusinessDate(d) >= start &&
                    GetDoseBusinessDate(d) <= today)
                .ToList();

            var model = new HistoryViewModel
            {
                Period = period,
                Uzeto = doze.Count(d => d.Status == StatusDoze.Uzeto),
                NijeUzeto = doze.Count(d => d.Status == StatusDoze.Propusteno)
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
            await _context.SaveChangesAsync();
            await UpdateTerapijaStatusAsync(doza.TerapijaId);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Jedna doza je oznacena kao uzeta.";
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
                GetFirstDoseCandidate(DateTime.Now),
                model.UkupanBrojDoza,
                model.IntervalSati,
                occupiedDoseTimes);
            var start = schedule.First();
            var end = schedule.Last();

            var terapija = new Terapija
            {
                Naziv = lijek.Naziv,
                Pocetak = start,
                Kraj = end,
                DnevnaDoza = CalculateLegacyDailyDose(model.IntervalSati),
                UkupanBrojDoza = model.UkupanBrojDoza,
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
                GetFirstDoseCandidate(DateTime.Now),
                model.UkupanBrojDoza,
                model.IntervalSati,
                occupiedDoseTimes);
            var start = schedule.First();
            var end = schedule.Last();

            terapija.Naziv = lijek.Naziv;
            terapija.Pocetak = start;
            terapija.Kraj = end;
            terapija.DnevnaDoza = CalculateLegacyDailyDose(model.IntervalSati);
            terapija.UkupanBrojDoza = model.UkupanBrojDoza;
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
            IEnumerable<DateTime> occupiedDoseTimes)
        {
            var schedule = new List<DateTime>();
            var occupancy = occupiedDoseTimes
                .Select(ToHourSlot)
                .GroupBy(slot => slot)
                .ToDictionary(group => group.Key, group => group.Count());

            var candidate = MoveToAllowedDoseHour(firstCandidate);

            for (var index = 0; index < totalDoses; index++)
            {
                var idealTime = index == 0
                    ? candidate
                    : schedule[index - 1].AddHours(intervalHours);
                var scheduledAt = FindNextAvailableDoseSlot(
                    idealTime,
                    occupancy);
                var slot = ToHourSlot(scheduledAt);

                schedule.Add(scheduledAt);
                occupancy[slot] = GetOccupancyCount(occupancy, slot) + 1;
            }

            return schedule;
        }

        private static DateTime FindNextAvailableDoseSlot(
            DateTime candidate,
            IDictionary<DateTime, int> occupancy)
        {
            var current = MoveToAllowedDoseHour(candidate);

            for (var attempt = 0; attempt < 24 * 366; attempt++)
            {
                var slot = ToHourSlot(current);
                if (IsAllowedDoseHour(slot) &&
                    GetOccupancyCount(occupancy, slot) < MaxDozaPoSatu)
                {
                    return slot;
                }

                current = MoveToAllowedDoseHour(slot.AddHours(1));
            }

            throw new InvalidOperationException(
                "Nije pronadjen slobodan termin za terapiju.");
        }

        private static DateTime GetFirstDoseCandidate(DateTime now)
        {
            var nextHour = new DateTime(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                0,
                0).AddHours(1);

            return MoveToAllowedDoseHour(nextHour);
        }

        private static int GetOccupancyCount(
            IDictionary<DateTime, int> occupancy,
            DateTime slot)
        {
            return occupancy.TryGetValue(slot, out var count)
                ? count
                : 0;
        }

        private static DateTime MoveToAllowedDoseHour(DateTime candidate)
        {
            var slot = ToHourSlot(candidate);
            if (slot.Hour < PrviDnevniSat)
            {
                return slot.Date.AddHours(PrviDnevniSat);
            }

            if (slot.Hour > ZadnjiDnevniSat)
            {
                return slot.Date.AddDays(1).AddHours(PrviDnevniSat);
            }

            return slot;
        }

        private static bool IsAllowedDoseHour(DateTime candidate)
        {
            return candidate.Hour >= PrviDnevniSat &&
                candidate.Hour <= ZadnjiDnevniSat;
        }

        private static DateTime ToHourSlot(DateTime value)
        {
            return new DateTime(
                value.Year,
                value.Month,
                value.Day,
                value.Hour,
                0,
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
                SljedecaDoza = next?.VrijemeUzimanja.ToString("dd.MM. HH:mm") ?? "-"
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

        private static int CalculateTakenProgress(
            IReadOnlyList<TerapijskaDoza> todayDoses)
        {
            if (todayDoses.Count == 0)
            {
                return 0;
            }

            return (int)Math.Round(
                (double)todayDoses.Count(d => d.Status == StatusDoze.Uzeto) /
                todayDoses.Count * 100);
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
    }
}
