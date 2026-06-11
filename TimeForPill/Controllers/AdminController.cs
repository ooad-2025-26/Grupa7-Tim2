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
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailSettings _emailSettings;
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            IOptions<EmailSettings> emailSettings,
            IEmailService emailService,
            ILogger<AdminController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _emailSettings = emailSettings.Value;
            _emailService = emailService;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Home()
        {
            var administrator = await GetCurrentAdminAsync();
            if (administrator == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var danas = DateTime.Today;
            var pocetakGodine = new DateTime(danas.Year, 1, 1);
            var krajGodine = pocetakGodine.AddYears(1);

            var zadnjeAkcije = await _context.AdminAkcije
                .AsNoTracking()
                .Where(a => a.AdministratorId == administrator.Id)
                .Where(a => a.DatumAkcije >= pocetakGodine &&
                    a.DatumAkcije < krajGodine)
                .OrderByDescending(a => a.DatumAkcije)
                .ThenByDescending(a => a.Id)
                .Take(10)
                .ToListAsync();

            var zahtjeviZaPotvrdu = await GetPendingAccountRequestsAsync();

            var model = new AdminDashboardViewModel
            {
                Ime = administrator.Ime,
                BrojPacijenata = await _context.Pacijenti.CountAsync(),
                BrojLjekara = await _context.Ljekari.CountAsync(),
                BrojIzvrsenihAkcija = await _context.AdminAkcije
                    .CountAsync(a =>
                        a.AdministratorId == administrator.Id &&
                        a.DatumAkcije >= pocetakGodine &&
                        a.DatumAkcije < krajGodine),
                BrojZahtjevaZaPotvrdu = zahtjeviZaPotvrdu.Count,
                ZadnjiZahtjevZaPotvrdu = zahtjeviZaPotvrdu.FirstOrDefault(),
                ZadnjeAkcije = zadnjeAkcije
                    .Select(a =>
                        $"{a.DatumAkcije:dd.MM.yyyy HH:mm} - {a.VrstaAkcije} {a.TipRacuna}: {a.RacunNaziv}")
                    .ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> ZahtjeviNaloga()
        {
            if (!await IsCurrentAdminAsync())
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await GetAccountRequestsForCurrentYearAsync();
            return View(model);
        }

        public async Task<IActionResult> ZahtjevNalogaDetalji(string id)
        {
            if (!await IsCurrentAdminAsync())
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user is not Ljekar ljekar || ljekar.EmailConfirmed)
            {
                return NotFound();
            }

            return View(BuildAccountRequestViewModel(ljekar));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PotvrdiNalog(string id)
        {
            var administrator = await GetCurrentAdminAsync();
            if (administrator == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user is not Ljekar ljekar)
            {
                return NotFound();
            }

            ljekar.EmailConfirmed = true;
            var result = await _userManager.UpdateAsync(ljekar);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Nalog nije potvrdjen. Pokusajte ponovo.";
                return RedirectToAction(nameof(ZahtjeviNaloga));
            }

            AddAdminAction(administrator, "Potvrdjen", ljekar);
            await _context.SaveChangesAsync();

            try
            {
                await SendDoctorApprovedEmailAsync(ljekar);
                TempData["Success"] = "Nalog je potvrdjen i ljekar je obavijesten emailom.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Email ljekaru nije poslan nakon potvrde naloga {LjekarId}.",
                    ljekar.Id);

                TempData["Success"] = "Nalog je potvrdjen.";
                TempData["Error"] = "Email ljekaru nije poslan.";
            }

            return RedirectToAction(nameof(ZahtjeviNaloga));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OdbijNalog(string id)
        {
            var administrator = await GetCurrentAdminAsync();
            if (administrator == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user is not Ljekar)
            {
                return NotFound();
            }

            AddAdminAction(administrator, "Odbijen", user);

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Nalog nije odbijen. Pokusajte ponovo.";
                return RedirectToAction(nameof(ZahtjeviNaloga));
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Zahtjev za nalog je odbijen.";
            return RedirectToAction(nameof(ZahtjeviNaloga));
        }

        public async Task<IActionResult> PregledPacijenata()
        {
            if (!await IsCurrentAdminAsync())
            {
                return RedirectToAction("Login", "Account");
            }

            var pacijenti = await _context.Pacijenti
                .Include(p => p.Ljekar)
                .Include(p => p.KontaktOsoba)
                .AsNoTracking()
                .OrderBy(p => p.Prezime)
                .ThenBy(p => p.Ime)
                .ToListAsync();

            return View(pacijenti);
        }

        public async Task<IActionResult> PregledLjekara()
        {
            if (!await IsCurrentAdminAsync())
            {
                return RedirectToAction("Login", "Account");
            }

            var ljekari = await _context.Ljekari
                .AsNoTracking()
                .OrderBy(l => l.Prezime)
                .ThenBy(l => l.Ime)
                .ToListAsync();

            return View(ljekari);
        }

        [HttpGet]
        public async Task<IActionResult> Profil()
        {
            var administrator = await GetCurrentAdminAsync();
            if (administrator == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(new ProfileViewModel
            {
                KorisnikId = administrator.Id,
                Uloga = KorisnickaUloga.Administrator,
                Ime = administrator.Ime,
                Prezime = administrator.Prezime,
                DatumRodjenja = administrator.DatumRodjenja,
                Spol = administrator.Spol,
                Email = administrator.Email ?? string.Empty,
                PrikaziKontaktOsobu = false
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profil(ProfileViewModel model)
        {
            model.Uloga = KorisnickaUloga.Administrator;
            model.PrikaziKontaktOsobu = false;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var administrator = await GetCurrentAdminAsync();
            if (administrator == null)
            {
                return RedirectToAction("Login", "Account");
            }

            administrator.Ime = model.Ime;
            administrator.Prezime = model.Prezime;
            administrator.DatumRodjenja = model.DatumRodjenja;
            administrator.Spol = model.Spol;
            administrator.Email = model.Email;
            administrator.UserName = model.Email;

            var result = await _userManager.UpdateAsync(administrator);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            TempData["Success"] = "Profil je azuriran.";
            return RedirectToAction(nameof(Profil));
        }

        private async Task<bool> IsCurrentAdminAsync()
        {
            return await GetCurrentAdminAsync() != null;
        }

        private async Task<Administrator?> GetCurrentAdminAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user as Administrator;
        }

        private async Task<List<AccountApprovalRequestViewModel>> GetPendingAccountRequestsAsync()
        {
            var users = await _userManager.Users
                .Where(u => !u.EmailConfirmed)
                .OrderBy(u => u.Prezime)
                .ThenBy(u => u.Ime)
                .ToListAsync();

            return users
                .Where(u => u is Ljekar)
                .Select(BuildAccountRequestViewModel)
                .ToList();
        }

        private async Task<List<AccountApprovalRequestViewModel>>
            GetAccountRequestsForCurrentYearAsync()
        {
            var danas = DateTime.Today;
            var pocetakGodine = new DateTime(danas.Year, 1, 1);
            var krajGodine = pocetakGodine.AddYears(1);

            var pending = await GetPendingAccountRequestsAsync();
            var processed = await _context.AdminAkcije
                .AsNoTracking()
                .Where(a =>
                    (a.VrstaAkcije == "Potvrdjen" ||
                        a.VrstaAkcije == "Odbijen") &&
                    a.DatumAkcije >= pocetakGodine &&
                    a.DatumAkcije < krajGodine)
                .OrderByDescending(a => a.DatumAkcije)
                .ThenByDescending(a => a.Id)
                .Select(a => new AccountApprovalRequestViewModel
                {
                    Id = a.RacunId,
                    ImePrezime = a.RacunNaziv,
                    Email = a.RacunEmail,
                    Uloga = a.TipRacuna,
                    Specijalizacija = "-",
                    Status = a.VrstaAkcije == "Potvrdjen"
                        ? "Odobren"
                        : "Odbijen",
                    Datum = a.DatumAkcije,
                    MozePregled = false
                })
                .ToListAsync();

            return pending
                .Concat(processed)
                .OrderBy(r => r.Status == "Neobraden" ? 0 : 1)
                .ThenByDescending(r => r.Datum ?? DateTime.MaxValue)
                .ThenBy(r => r.ImePrezime)
                .ToList();
        }

        private static AccountApprovalRequestViewModel BuildAccountRequestViewModel(
            ApplicationUser user)
        {
            return new AccountApprovalRequestViewModel
            {
                Id = user.Id,
                ImePrezime = $"{user.Ime} {user.Prezime}".Trim(),
                Email = user.Email ?? string.Empty,
                Uloga = GetAccountType(user),
                Specijalizacija = user is Ljekar ljekar
                    ? ljekar.Specijalizacija.ToString()
                    : "-",
                Status = "Neobraden",
                Datum = null,
                MozePregled = true
            };
        }

        private void AddAdminAction(
            Administrator administrator,
            string vrstaAkcije,
            ApplicationUser user)
        {
            _context.AdminAkcije.Add(new AdminAkcija
            {
                AdministratorId = administrator.Id,
                AdministratorNaziv =
                    $"{administrator.Ime} {administrator.Prezime}",
                VrstaAkcije = vrstaAkcije,
                TipRacuna = GetAccountType(user),
                RacunId = user.Id,
                RacunNaziv = $"{user.Ime} {user.Prezime}",
                RacunEmail = user.Email ?? string.Empty,
                DatumAkcije = DateTime.Now
            });
        }

        private static string GetAccountType(ApplicationUser user)
        {
            return user switch
            {
                Ljekar => "Ljekar",
                Pacijent => "Pacijent",
                Administrator => "Administrator",
                _ => "Korisnik"
            };
        }

        private async Task SendDoctorApprovedEmailAsync(Ljekar ljekar)
        {
            var loginUrl = BuildAppUrl("/Account/Login");
            var body =
                $"""
                <p>Postovani/a {ljekar.Ime} {ljekar.Prezime},</p>
                <p>Administrator je odobrio vas TimeForPill nalog.</p>
                <p>Sada se mozete prijaviti u aplikaciju.</p>
                <p><a href="{loginUrl}">Otvorite TimeForPill login</a></p>
                """;

            await _emailService.SendEmailAsync(
                ljekar.Email ?? string.Empty,
                "TimeForPill nalog je odobren",
                body,
                isBodyHtml: true);
        }

        private string BuildAppUrl(string relativeUrl)
        {
            if (relativeUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                relativeUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return relativeUrl;
            }

            return $"{_emailSettings.EffectiveAppUrl}/{relativeUrl.TrimStart('/')}";
        }
    }
}
