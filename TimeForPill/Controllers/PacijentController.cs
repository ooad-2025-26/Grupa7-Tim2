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

        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<PacijentController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public PacijentController(
            ApplicationDbContext context,
            IEmailService emailService,
            IWebHostEnvironment env,
            IOptions<EmailSettings> emailSettings,
            ILogger<PacijentController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
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

            var now = DateTime.Now;
            var danas = now.Date;
            var terapije = await GetPacijentTerapijeAsync(pacijent.Id);
            var danasnje = terapije
                .Where(t => t.Pocetak.Date <= danas && t.Kraj.Date >= danas)
                .ToList();

            var next = terapije
                .Select(t => new
                {
                    Terapija = t,
                    Vrijeme = GetNextDoseDateTime(t, now)
                })
                .Where(x => x.Vrijeme.HasValue)
                .OrderBy(x => x.Vrijeme)
                .FirstOrDefault();

            var viewModel = new PatientDashboardViewModel
            {
                Ime = pacijent.Ime,
                BrojLijekovaDanas = danasnje.Count,
                BrojUzetihDanas =
                    danasnje.Count(t => t.Status == StatusTerapije.Uzeto),
                BrojPropustenihDanas =
                    danasnje.Count(t => t.Status == StatusTerapije.Propusteno),
                ProgresDoSljedecegLijeka =
                    next?.Vrijeme == null ? 0 : CalculateProgress(now, next.Vrijeme.Value),
                SljedeciLijekNaziv =
                    next?.Terapija.Lijek?.Naziv ?? next?.Terapija.Naziv ?? "Nema aktivne terapije",
                SljedeciLijekVrijeme =
                    next?.Vrijeme?.ToString("HH:mm") ?? "-",
                PreostaloDoSljedeceg =
                    next?.Vrijeme == null ? "-" : FormatRemaining(now, next.Vrijeme.Value),
                SljedeciLijekSlika =
                    next?.Terapija.Lijek?.Slika,
                SljedecaTerapijaId =
                    next?.Terapija.Id,
                DanasnjeTerapije =
                    danasnje.Select(t => ToMedicineListItem(t, now)).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> MojiLijekovi()
        {
            var pacijent = await GetCurrentPacijentAsync();
            if (pacijent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var terapije = await GetPacijentTerapijeAsync(pacijent.Id);
            var viewModel = terapije
                .OrderBy(t => t.Kraj)
                .Select(t => ToMedicineListItem(t, DateTime.Now))
                .ToList();

            return View(viewModel);
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
                return View(new MedicineFormViewModel());
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

            return View(new MedicineFormViewModel
            {
                TerapijaId = terapija.Id,
                LijekId = terapija.LijekId,
                Naziv = terapija.Lijek?.Naziv ?? terapija.Naziv,
                Kategorija = terapija.Lijek?.Kategorija ?? "Terapija",
                DnevnaDoza = terapija.DnevnaDoza,
                Pocetak = terapija.Pocetak,
                Kraj = terapija.Kraj,
                PostojecaSlika = terapija.Lijek?.Slika
            });
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

            if (model.Kraj.Date < model.Pocetak.Date)
            {
                ModelState.AddModelError(
                    nameof(MedicineFormViewModel.Kraj),
                    "Datum kraja ne moze biti prije datuma pocetka.");
            }

            var uploadedImage = await TrySaveImageAsync(model.SlikaFile);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (model.TerapijaId.HasValue)
                {
                    await UpdateExistingTherapyAsync(
                        model,
                        pacijent.Id,
                        uploadedImage);
                }
                else
                {
                    await CreateNewTherapyAsync(
                        model,
                        pacijent.Id,
                        uploadedImage);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Lijek je sacuvan u bazu.";
                return RedirectToAction(nameof(MojiLijekovi));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Lijek nije sacuvan. Provjerite vezu sa bazom i pokusajte ponovo.");

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
                model.PreostaloDoza = CalculateRemainingDoses(
                    terapija,
                    DateTime.Now);
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

            var terapije = await GetPacijentTerapijeAsync(pacijent.Id);
            var startSedmice = GetStartOfWeek(DateTime.Today);
            var sedmica = Enumerable.Range(0, 7)
                .Select(index =>
                {
                    var datum = startSedmice.AddDays(index);
                    var stavke = terapije
                        .Where(t =>
                            t.Pocetak.Date <= datum &&
                            t.Kraj.Date >= datum)
                        .SelectMany(t => GetDoseTimes(t.DnevnaDoza)
                            .Select(time => new ScheduleItemViewModel
                            {
                                Vrijeme = time.ToString(@"hh\:mm"),
                                NazivLijeka = t.Lijek?.Naziv ?? t.Naziv,
                                Status = ResolveScheduleStatus(t, datum, time),
                                Slika = t.Lijek?.Slika
                            }))
                        .OrderBy(i => i.Vrijeme)
                        .ToList();

                    return new ScheduleDayViewModel
                    {
                        NazivDana = DaniUSedmici[index],
                        Datum = datum,
                        Terapije = stavke
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

            var today = DateTime.Today;
            var start = period switch
            {
                "godisnja" => new DateTime(today.Year, 1, 1),
                "mjesecna" => new DateTime(today.Year, today.Month, 1),
                _ => today
            };

            var terapije = await _context.Terapije
                .AsNoTracking()
                .Where(t =>
                    t.PacijentId == pacijent.Id &&
                    t.Pocetak.Date <= today &&
                    t.Kraj.Date >= start)
                .ToListAsync();

            var model = new HistoryViewModel
            {
                Period = period,
                Uzeto = terapije.Count(t => t.Status == StatusTerapije.Uzeto),
                NijeUzeto = terapije.Count(t => t.Status != StatusTerapije.Uzeto)
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

            var terapija = await _context.Terapije
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.PacijentId == pacijent.Id);

            if (terapija == null)
            {
                return NotFound();
            }

            terapija.Status = StatusTerapije.Uzeto;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Doza je oznacena kao uzeta.";
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

            var terapija = await _context.Terapije
                .Include(t => t.Lijek)
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.PacijentId == pacijent.Id);

            if (terapija == null)
            {
                return NotFound();
            }

            var naziv = terapija.Lijek?.Naziv ?? terapija.Naziv;
            _context.Notifikacije.Add(new Notifikacija
            {
                Naziv = $"Odgoda - {naziv}",
                Poruka =
                    $"Podsjetnik za {naziv} je odgodjen do {DateTime.Now.AddMinutes(30):HH:mm}.",
                TerapijaId = terapija.Id
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Podsjetnik je odgodjen za 30 minuta.";
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

        private async Task CreateNewTherapyAsync(
            MedicineFormViewModel model,
            string pacijentId,
            string? uploadedImage)
        {
            var lijek = new Lijek
            {
                Naziv = model.Naziv,
                Kategorija = model.Kategorija,
                Slika = uploadedImage
            };

            _context.Lijekovi.Add(lijek);

            await _context.Terapije.AddAsync(new Terapija
            {
                Naziv = model.Naziv,
                Pocetak = model.Pocetak,
                Kraj = model.Kraj,
                DnevnaDoza = model.DnevnaDoza,
                Status = StatusTerapije.Cekanje,
                PacijentId = pacijentId,
                Lijek = lijek
            });
        }

        private async Task UpdateExistingTherapyAsync(
            MedicineFormViewModel model,
            string pacijentId,
            string? uploadedImage)
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

            terapija.Naziv = model.Naziv;
            terapija.Pocetak = model.Pocetak;
            terapija.Kraj = model.Kraj;
            terapija.DnevnaDoza = model.DnevnaDoza;

            if (terapija.Lijek == null)
            {
                terapija.Lijek = new Lijek();
            }

            terapija.Lijek.Naziv = model.Naziv;
            terapija.Lijek.Kategorija = model.Kategorija;
            if (!string.IsNullOrWhiteSpace(uploadedImage))
            {
                terapija.Lijek.Slika = uploadedImage;
            }
        }

        private async Task<RenewTherapyViewModel> BuildRenewTherapyViewModelAsync(
            string pacijentId)
        {
            var terapije = await GetPacijentTerapijeAsync(pacijentId);
            return new RenewTherapyViewModel
            {
                Terapije = terapije
                    .Select(t => new TherapyOptionViewModel
                    {
                        TerapijaId = t.Id,
                        Naziv = t.Lijek?.Naziv ?? t.Naziv,
                        PreostaloDoza = CalculateRemainingDoses(
                            t,
                            DateTime.Now)
                    })
                    .ToList()
            };
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
            DateTime now)
        {
            var next = GetNextDoseDateTime(terapija, now);

            return new MedicineListItemViewModel
            {
                TerapijaId = terapija.Id,
                LijekId = terapija.LijekId,
                Naziv = terapija.Lijek?.Naziv ?? terapija.Naziv,
                Kategorija = terapija.Lijek?.Kategorija ?? "Terapija",
                DnevnaDoza = terapija.DnevnaDoza,
                Pocetak = terapija.Pocetak,
                Kraj = terapija.Kraj,
                Status = terapija.Status,
                Slika = terapija.Lijek?.Slika,
                SljedecaDoza = next?.ToString("HH:mm") ?? "-"
            };
        }

        private static DateTime? GetNextDoseDateTime(
            Terapija terapija,
            DateTime now)
        {
            if (terapija.Status == StatusTerapije.Uzeto ||
                terapija.Kraj.Date < now.Date)
            {
                return null;
            }

            var date = terapija.Pocetak.Date > now.Date
                ? terapija.Pocetak.Date
                : now.Date;

            while (date <= terapija.Kraj.Date)
            {
                foreach (var time in GetDoseTimes(terapija.DnevnaDoza))
                {
                    var candidate = date.Add(time);
                    if (candidate >= now)
                    {
                        return candidate;
                    }
                }

                date = date.AddDays(1);
            }

            return null;
        }

        private static IReadOnlyList<TimeSpan> GetDoseTimes(int dailyDose)
        {
            if (dailyDose <= 1)
            {
                return new[] { new TimeSpan(9, 0, 0) };
            }

            if (dailyDose == 2)
            {
                return new[]
                {
                    new TimeSpan(8, 0, 0),
                    new TimeSpan(20, 0, 0)
                };
            }

            if (dailyDose == 3)
            {
                return new[]
                {
                    new TimeSpan(8, 0, 0),
                    new TimeSpan(14, 0, 0),
                    new TimeSpan(20, 0, 0)
                };
            }

            var startHour = 7.0;
            var endHour = 22.0;
            var interval = (endHour - startHour) / (dailyDose - 1);

            return Enumerable.Range(0, dailyDose)
                .Select(index => TimeSpan.FromHours(startHour + interval * index))
                .ToList();
        }

        private static int CalculateRemainingDoses(
            Terapija terapija,
            DateTime now)
        {
            if (terapija.Kraj.Date < now.Date)
            {
                return 0;
            }

            var start = terapija.Pocetak.Date > now.Date
                ? terapija.Pocetak.Date
                : now.Date;

            var total = 0;
            for (var date = start; date <= terapija.Kraj.Date; date = date.AddDays(1))
            {
                total += date == now.Date
                    ? GetDoseTimes(terapija.DnevnaDoza)
                        .Count(time => now.Date.Add(time) >= now)
                    : terapija.DnevnaDoza;
            }

            return total;
        }

        private static StatusTerapije ResolveScheduleStatus(
            Terapija terapija,
            DateTime datum,
            TimeSpan vrijeme)
        {
            if (terapija.Status != StatusTerapije.Cekanje)
            {
                return terapija.Status;
            }

            var scheduledAt = datum.Date.Add(vrijeme);
            return scheduledAt < DateTime.Now
                ? StatusTerapije.Propusteno
                : StatusTerapije.Cekanje;
        }

        private static int CalculateProgress(DateTime now, DateTime nextDose)
        {
            var dayStart = now.Date;
            var totalMinutes = (nextDose - dayStart).TotalMinutes;
            if (totalMinutes <= 0)
            {
                return 100;
            }

            var elapsed = (now - dayStart).TotalMinutes;
            return Math.Clamp(
                (int)Math.Round(elapsed / totalMinutes * 100),
                0,
                100);
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
