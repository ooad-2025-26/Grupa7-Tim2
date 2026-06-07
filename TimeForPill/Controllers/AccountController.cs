using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;
using TimeForPill.ViewModels;

namespace TimeForPill.Controllers
{
    public class AccountController : Controller
    {
        private const string LoginError =
            "Netacna lozinka ili email, pokusajte ponovo";

        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            ApplicationDbContext context,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
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
            user.EmailConfirmed = true;

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToRoleHome(user);
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
