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
            if (!await IsCurrentAdminAsync())
            {
                return RedirectToAction("Login", "Account");
            }

            var processedRequests = await _context.Zahtjevi
                .AsNoTracking()
                .Where(z => z.Status != StatusZahtjeva.Neobraden)
                .OrderByDescending(z => z.Id)
                .Take(5)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                BrojPacijenata = await _context.Pacijenti.CountAsync(),
                BrojLjekara = await _context.Ljekari.CountAsync(),
                BrojIzvrsenihAkcija = await _context.Zahtjevi
                    .CountAsync(z => z.Status != StatusZahtjeva.Neobraden),
                ZadnjeAkcije = processedRequests
                    .Select(z => $"{z.Status}: {z.Naziv}")
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
            var user = await _userManager.GetUserAsync(User);
            return user is Administrator;
        }
    }
}
