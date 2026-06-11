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
    public class AccountController : Controller
    {
        private const string LoginError =
            "Netacna lozinka ili email, pokusajte ponovo";
        private const string AccountPendingMessage =
            "Nalog ceka potvrdu administratora.";
        private const string EmailConfirmationMessage =
            "Potvrdite email adresu putem linka koji smo poslali na vas email.";

        private readonly ApplicationDbContext _context;
        private readonly EmailSettings _emailSettings;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            ApplicationDbContext context,
            IOptions<EmailSettings> emailSettings,
            IEmailService emailService,
            ILogger<AccountController> logger,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _emailSettings = emailSettings.Value;
            _emailService = emailService;
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    return RedirectToRoleHome(user);
                }
            }

            ViewData["AuthPage"] = true;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewData["AuthPage"] = true;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !MatchesSelectedRole(user, model.Uloga))
            {
                ModelState.AddModelError(string.Empty, LoginError);
                return View(model);
            }

            var passwordOk = await _userManager.CheckPasswordAsync(
                user,
                model.Password);

            if (!passwordOk)
            {
                ModelState.AddModelError(string.Empty, LoginError);
                return View(model);
            }

            if (!user.EmailConfirmed && user is not Administrator)
            {
                ModelState.AddModelError(
                    string.Empty,
                    user is Pacijent ? EmailConfirmationMessage : AccountPendingMessage);
                return View(model);
            }

            await _signInManager.SignInAsync(user, model.RememberMe);

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) &&
                Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToRoleHome(user);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            ViewData["AuthPage"] = true;
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewData["AuthPage"] = true;

            if (model.Uloga == KorisnickaUloga.Ljekar &&
                model.Specijalizacija == null)
            {
                ModelState.AddModelError(
                    nameof(RegisterViewModel.Specijalizacija),
                    "Specijalizacija je obavezna za ljekara.");
            }

            if (model.Uloga == KorisnickaUloga.Administrator)
            {
                ModelState.AddModelError(
                    nameof(RegisterViewModel.Uloga),
                    "Administrator se ne moze registrovati kroz javnu registraciju.");
            }

            if (model.Uloga == KorisnickaUloga.Pacijent)
            {
                ValidatePatientContact(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await BuildUserAsync(model);
            user.UserName = model.Email;
            user.Email = model.Email;
            user.EmailConfirmed = model.Uloga == KorisnickaUloga.Administrator;

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            if (user.EmailConfirmed)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToRoleHome(user);
            }

            if (user is Pacijent)
            {
                if (await SendPatientConfirmationEmailAsync(user))
                {
                    TempData["Success"] =
                        "Registracija je sacuvana. Provjerite email i potvrdite nalog prije prijave.";
                }
            }
            else if (user is Ljekar ljekar)
            {
                await NotifyAdminsAboutDoctorRequestAsync(ljekar);
                TempData["Success"] =
                    "Zahtjev za registraciju ljekara je poslan administratoru.";
            }

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(
            string? userId,
            string? token)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "Link za potvrdu emaila nije ispravan.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "Korisnik nije pronadjen.";
                return RedirectToAction(nameof(Login));
            }

            if (user is not Pacijent)
            {
                TempData["Error"] =
                    "Ovaj link je namijenjen samo potvrdi emaila pacijenta.";
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Email je potvrdjen. Sada se mozete prijaviti."
                : "Email nije potvrdjen. Link je istekao ili nije ispravan.";

            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        private async Task<ApplicationUser> BuildUserAsync(
            RegisterViewModel model)
        {
            return model.Uloga switch
            {
                KorisnickaUloga.Ljekar => new Ljekar
                {
                    Ime = model.Ime,
                    Prezime = model.Prezime,
                    DatumRodjenja = model.DatumRodjenja,
                    Spol = model.Spol,
                    Specijalizacija =
                        model.Specijalizacija ?? Specijalizacija.Kardiolog
                },

                KorisnickaUloga.Administrator => new Administrator
                {
                    Ime = model.Ime,
                    Prezime = model.Prezime,
                    DatumRodjenja = model.DatumRodjenja,
                    Spol = model.Spol,
                    datumImenovanja = DateTime.Today
                },

                _ => new Pacijent
                {
                    Ime = model.Ime,
                    Prezime = model.Prezime,
                    DatumRodjenja = model.DatumRodjenja,
                    Spol = model.Spol,
                    LjekarId = await FindDoctorForNextPatientAsync(),
                    DatumDodjeleLjekara = DateTime.Now,
                    KontaktOsoba = new KontaktOsoba
                    {
                        Ime = model.KontaktIme,
                        Prezime = model.KontaktPrezime,
                        Email = model.KontaktEmail,
                        BrojTelefona = model.KontaktTelefon
                    }
                }
            };
        }

        private async Task<string?> FindDoctorForNextPatientAsync()
        {
            var ljekari = await _context.Ljekari
                .AsNoTracking()
                .OrderBy(l => l.Prezime)
                .ThenBy(l => l.Ime)
                .Select(l => new
                {
                    l.Id,
                    LastAssignedAt = _context.Pacijenti
                        .Where(p => p.LjekarId == l.Id)
                        .Max(p => p.DatumDodjeleLjekara)
                })
                .ToListAsync();

            return ljekari
                .OrderBy(l => l.LastAssignedAt ?? DateTime.MinValue)
                .ThenBy(l => l.Id)
                .Select(l => l.Id)
                .FirstOrDefault();
        }

        private async Task<bool> SendPatientConfirmationEmailAsync(
            ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmUrl = BuildAppUrl(
                Url.Action(
                    nameof(ConfirmEmail),
                    "Account",
                    new { userId = user.Id, token }) ?? string.Empty);

            var body =
                $"""
                <p>Postovani/a {user.Ime} {user.Prezime},</p>
                <p>Potvrdite email adresu kako biste mogli koristiti TimeForPill aplikaciju.</p>
                <p><a href="{confirmUrl}">Potvrdite email adresu</a></p>
                """;

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email ?? string.Empty,
                    "TimeForPill potvrda email adrese",
                    body,
                    isBodyHtml: true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Email potvrda nije poslana korisniku {UserId}.",
                    user.Id);

                TempData["Error"] =
                    "Nalog je kreiran, ali email za potvrdu nije poslan. Provjerite SMTP postavke.";

                return false;
            }
        }

        private async Task NotifyAdminsAboutDoctorRequestAsync(Ljekar ljekar)
        {
            var adminEmails = await _context.Administratori
                .AsNoTracking()
                .Where(a => a.EmailConfirmed && a.Email != null)
                .Select(a => a.Email!)
                .ToListAsync();

            if (!adminEmails.Any())
            {
                return;
            }

            var loginUrl = BuildAppUrl("/Account/Login");
            var requestsUrl = BuildAppUrl("/Admin/ZahtjeviNaloga");
            var body =
                $"""
                <p>Novi ljekar trazi potvrdu naloga.</p>
                <p><strong>{ljekar.Ime} {ljekar.Prezime}</strong></p>
                <p>Email: {ljekar.Email}</p>
                <p>Specijalizacija: {ljekar.Specijalizacija}</p>
                <p><a href="{loginUrl}">Otvorite TimeForPill login</a></p>
                <p><a href="{requestsUrl}">Otvorite zahtjeve za potvrdu naloga</a></p>
                """;

            foreach (var email in adminEmails.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        email,
                        "TimeForPill zahtjev za potvrdu naloga ljekara",
                        body,
                        isBodyHtml: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Email administratoru nije poslan za zahtjev ljekara {LjekarId}.",
                        ljekar.Id);
                }
            }
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

        private void ValidatePatientContact(RegisterViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.KontaktIme))
            {
                ModelState.AddModelError(
                    nameof(RegisterViewModel.KontaktIme),
                    "Ime kontakt osobe je obavezno.");
            }

            if (string.IsNullOrWhiteSpace(model.KontaktPrezime))
            {
                ModelState.AddModelError(
                    nameof(RegisterViewModel.KontaktPrezime),
                    "Prezime kontakt osobe je obavezno.");
            }

            if (string.IsNullOrWhiteSpace(model.KontaktEmail))
            {
                ModelState.AddModelError(
                    nameof(RegisterViewModel.KontaktEmail),
                    "Email kontakt osobe je obavezan.");
            }
            else if (IsSameEmail(model.Email, model.KontaktEmail))
            {
                ModelState.AddModelError(
                    nameof(RegisterViewModel.KontaktEmail),
                    "Email kontakt osobe ne moze biti isti kao email pacijenta.");
            }

            if (string.IsNullOrWhiteSpace(model.KontaktTelefon))
            {
                ModelState.AddModelError(
                    nameof(RegisterViewModel.KontaktTelefon),
                    "Telefon kontakt osobe je obavezan.");
            }
        }

        private static bool IsSameEmail(string? email, string? contactEmail)
        {
            return string.Equals(
                email?.Trim(),
                contactEmail?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesSelectedRole(
            ApplicationUser user,
            KorisnickaUloga uloga)
        {
            return uloga switch
            {
                KorisnickaUloga.Pacijent => user is Pacijent,
                KorisnickaUloga.Ljekar => user is Ljekar,
                KorisnickaUloga.Administrator => user is Administrator,
                _ => false
            };
        }

        private RedirectToActionResult RedirectToRoleHome(
            ApplicationUser user)
        {
            return user switch
            {
                Pacijent => RedirectToAction("Home", "Pacijent"),
                Ljekar => RedirectToAction("Home", "Ljekar"),
                Administrator => RedirectToAction("Home", "Admin"),
                _ => RedirectToAction(nameof(Login))
            };
        }
    }
}
