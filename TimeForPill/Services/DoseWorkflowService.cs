using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill.Services
{
    public class DoseWorkflowService : IDoseWorkflowService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<DoseWorkflowService> _logger;

        public DoseWorkflowService(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<DoseWorkflowService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task RefreshMissedDosesAsync()
        {
            var now = DateTime.Now;
            var missedLimit = now.AddHours(-1);

            var newlyMissed = await _context.TerapijskeDoze
                .Include(d => d.Terapija)
                    .ThenInclude(t => t!.Pacijent)
                        .ThenInclude(p => p!.KontaktOsoba)
                .Include(d => d.Terapija)
                    .ThenInclude(t => t!.Lijek)
                .Where(d =>
                    d.Status == StatusDoze.Cekanje &&
                    (d.OriginalnoVrijemeUzimanja ?? d.VrijemeUzimanja) <= missedLimit)
                .ToListAsync();

            foreach (var doza in newlyMissed)
            {
                doza.Status = StatusDoze.Propusteno;
            }

            var affectedTherapyIds = newlyMissed
                .Select(d => d.TerapijaId)
                .Distinct()
                .ToList();

            if (newlyMissed.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            foreach (var terapijaId in affectedTherapyIds)
            {
                await UpdateTherapyStatusAsync(terapijaId);
            }

            if (affectedTherapyIds.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            await NotifyContactsForMissedDosesAsync();
        }

        public async Task SendDueReminderEmailsAsync()
        {
            var now = DateTime.Now;
            var reminders = await _context.TerapijskeDoze
                .Include(d => d.Terapija)
                    .ThenInclude(t => t!.Pacijent)
                .Include(d => d.Terapija)
                    .ThenInclude(t => t!.Lijek)
                .Where(d =>
                    d.Status == StatusDoze.Cekanje &&
                    !d.EmailPodsjetnikPoslan &&
                    d.VrijemePodsjetnika <= now &&
                    (d.OriginalnoVrijemeUzimanja ?? d.VrijemeUzimanja) > now.AddHours(-1))
                .OrderBy(d => d.VrijemeUzimanja)
                .Take(25)
                .ToListAsync();

            foreach (var doza in reminders)
            {
                var pacijent = doza.Terapija?.Pacijent;
                var email = pacijent?.Email;
                if (string.IsNullOrWhiteSpace(email))
                {
                    doza.EmailPodsjetnikPoslan = true;
                    continue;
                }

                var naziv = doza.Terapija?.Lijek?.Naziv ??
                    doza.Terapija?.Naziv ??
                    "lijek";

                try
                {
                    await _emailService.SendEmailAsync(
                        email,
                        $"Podsjetnik za lijek {naziv}",
                        $"Vrijeme je za dozu lijeka {naziv}. Planirano vrijeme uzimanja: {doza.VrijemeUzimanja:dd.MM.yyyy HH:mm}.\n\n" +
                        "Prijava u aplikaciju: https://timeforpill.runasp.net/Account/Login");

                    doza.EmailPodsjetnikPoslan = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Email podsjetnik nije poslan za dozu {DozaId}.",
                        doza.Id);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task NotifyContactsForMissedDosesAsync()
        {
            var completedDoses = await _context.TerapijskeDoze
                .Include(d => d.Terapija)
                    .ThenInclude(t => t!.Pacijent)
                        .ThenInclude(p => p!.KontaktOsoba)
                .Where(d =>
                    d.Status != StatusDoze.Cekanje &&
                    d.Terapija != null &&
                    d.Terapija.PacijentId != null)
                .ToListAsync();

            foreach (var patientDoses in completedDoses
                .GroupBy(d => d.Terapija!.PacijentId!))
            {
                var orderedDoses = patientDoses
                    .OrderBy(GetOriginalDoseTime)
                    .ThenBy(d => d.Id)
                    .ToList();
                var missedStreak = new List<TerapijskaDoza>();

                foreach (var doza in orderedDoses)
                {
                    if (doza.Status == StatusDoze.Propusteno)
                    {
                        missedStreak.Add(doza);
                        continue;
                    }

                    await NotifyContactForMissedStreakAsync(missedStreak);
                    missedStreak.Clear();
                }

                await NotifyContactForMissedStreakAsync(missedStreak);
            }

            await _context.SaveChangesAsync();
        }

        private async Task NotifyContactForMissedStreakAsync(
            IReadOnlyList<TerapijskaDoza> missedStreak)
        {
            if (missedStreak.Count < 3 ||
                missedStreak.Take(3).All(d => d.KontaktObavijestPoslana))
            {
                return;
            }

            var sample = missedStreak[0];
            var pacijent = sample.Terapija?.Pacijent;
            var kontaktEmail = pacijent?.KontaktOsoba?.Email;
            if (string.IsNullOrWhiteSpace(kontaktEmail))
            {
                foreach (var doza in missedStreak)
                {
                    doza.KontaktObavijestPoslana = true;
                }

                return;
            }

            var imePrezime = $"{pacijent?.Ime} {pacijent?.Prezime}".Trim();
            if (string.IsNullOrWhiteSpace(imePrezime))
            {
                imePrezime = "Pacijent";
            }

            try
            {
                await _emailService.SendEmailAsync(
                    kontaktEmail,
                    "TimeForPill obavijest o propustenoj terapiji",
                    $"{imePrezime} je propustilo terapiju prevelik broj puta, obratiti paznju.");

                foreach (var doza in missedStreak)
                {
                    doza.KontaktObavijestPoslana = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Kontakt osoba nije obavijestena za pacijenta {PacijentId} nakon tri uzastopne propustene doze.",
                    sample.Terapija?.PacijentId);
            }
        }

        private async Task UpdateTherapyStatusAsync(int terapijaId)
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
            }
            else if (statuses.All(s => s == StatusDoze.Uzeto))
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

        private static DateTime GetOriginalDoseTime(TerapijskaDoza doza)
        {
            return doza.OriginalnoVrijemeUzimanja ?? doza.VrijemeUzimanja;
        }
    }
}
