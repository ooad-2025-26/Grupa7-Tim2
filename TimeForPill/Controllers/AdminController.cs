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
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Home()
        {
            var administrator = await GetCurrentAdminAsync();
            if (administrator == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var zadnjeAkcije = await _context.AdminAkcije
                .AsNoTracking()
                .Where(a => a.AdministratorId == administrator.Id)
                .OrderByDescending(a => a.DatumAkcije)
                .ThenByDescending(a => a.Id)
                .Take(5)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                BrojPacijenata = await _context.Pacijenti.CountAsync(),
                BrojLjekara = await _context.Ljekari.CountAsync(),
                BrojIzvrsenihAkcija = await _context.AdminAkcije
                    .CountAsync(a => a.AdministratorId == administrator.Id),
                ZadnjeAkcije = zadnjeAkcije
                    .Select(a =>
                        $"{a.DatumAkcije:dd.MM.yyyy HH:mm} - {a.VrstaAkcije} {a.TipRacuna}: {a.RacunNaziv}")
                    .ToList()
            };

            return View(model);
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

        private async Task<bool> IsCurrentAdminAsync()
        {
            return await GetCurrentAdminAsync() != null;
        }

        private async Task<Administrator?> GetCurrentAdminAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user as Administrator;
        }
    }
}
