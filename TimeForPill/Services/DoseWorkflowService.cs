using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill.Services
{
    public class DoseWorkflowService : IDoseWorkflowService
    {
        private const int MaxConcurrentEmailSends = 5;

        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<DoseWorkflowService> _logger;

        public DoseWorkflowService(
            ApplicationDbContext context,
            IEmailService emailService,
            IOptions<EmailSettings> emailSettings,
            ILogger<DoseWorkflowService> logger)
        {
            _context = context;
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
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
                doza.VrijemeEvidentiranja ??= now;
            }

            await IncrementDailyStatisticsAsync(
                newlyMissed,
                status: StatusDoze.Propusteno);

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
                    (d.VrijemePodsjetnika <= now ||
                        d.VrijemeUzimanja <= now.AddMinutes(5)) &&
                    (d.OriginalnoVrijemeUzimanja ?? d.VrijemeUzimanja) > now.AddHours(-1))
                .OrderBy(d => d.VrijemeUzimanja)
                .Take(25)
                .ToListAsync();

            var emailJobs = new List<DoseReminderEmail>();
            foreach (var doza in reminders)
            {
                var expectedReminderTime = doza.VrijemeUzimanja.AddMinutes(-5);
                if (doza.VrijemePodsjetnika != expectedReminderTime)
                {
                    doza.VrijemePodsjetnika = expectedReminderTime;
                }

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

                emailJobs.Add(new DoseReminderEmail(
                    doza,
                    email,
                    "Vrijeme je za terapiju",
                    BuildDoseReminderBody(naziv, doza.VrijemeUzimanja),
                    IsBodyHtml: true));
            }

            await SendDoseReminderEmailsAsync(emailJobs);
            await _context.SaveChangesAsync();
        }

        private async Task NotifyContactsForMissedDosesAsync()
        {
            var completedDoses = await _context.TerapijskeDoze
                .Include(d => d.Terapija)
                    .ThenInclude(t => t!.Pacijent)
                        .ThenInclude(p => p!.KontaktOsoba)
                .Where(d =>
                    (d.Status != StatusDoze.Cekanje ||
                        d.BrojOdgoda >= 2) &&
                    d.Terapija != null &&
                    d.Terapija.PacijentId != null)
                .ToListAsync();

            var contactEmailJobs = new List<ContactEmailNotification>();
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
                    if (IsMissedForContactNotification(doza))
                    {
                        missedStreak.Add(doza);
                        QueueContactNotificationIfNeeded(
                            missedStreak,
                            contactEmailJobs);
                        continue;
                    }

                    missedStreak.Clear();
                }
            }

            await SendContactNotificationEmailsAsync(contactEmailJobs);
            await _context.SaveChangesAsync();
        }

        private void QueueContactNotificationIfNeeded(
            IReadOnlyList<TerapijskaDoza> missedStreak,
            ICollection<ContactEmailNotification> emailJobs)
        {
            var missedCount = missedStreak.Count;
            if (missedCount <= 0 ||
                missedCount % 3 != 0)
            {
                return;
            }

            var markerDose = missedStreak[^1];
            if (markerDose.KontaktObavijestPoslana)
            {
                return;
            }

            var sample = markerDose;
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

            emailJobs.Add(new ContactEmailNotification(
                missedStreak.ToList(),
                kontaktEmail,
                "Upozorenje o terapiji",
                $"Pacijent {imePrezime} je propustio {missedCount} uzastopnih doza terapije. Molimo provjerite njegovo stanje.",
                sample.Terapija?.PacijentId));
        }

        private async Task SendDoseReminderEmailsAsync(
            IReadOnlyList<DoseReminderEmail> emailJobs)
        {
            if (emailJobs.Count == 0)
            {
                return;
            }

            using var throttler = new SemaphoreSlim(MaxConcurrentEmailSends);
            var tasks = emailJobs.Select(async job =>
            {
                await throttler.WaitAsync();
                try
                {
                    await _emailService.SendEmailAsync(
                        job.To,
                        job.Subject,
                        job.Body,
                        job.IsBodyHtml);

                    return job;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Email podsjetnik nije poslan za dozu {DozaId}.",
                        job.Doza.Id);

                    return null;
                }
                finally
                {
                    throttler.Release();
                }
            });

            var sentJobs = await Task.WhenAll(tasks);
            foreach (var sentJob in sentJobs.Where(job => job != null))
            {
                sentJob!.Doza.EmailPodsjetnikPoslan = true;
            }
        }

        private async Task SendContactNotificationEmailsAsync(
            IReadOnlyList<ContactEmailNotification> emailJobs)
        {
            if (emailJobs.Count == 0)
            {
                return;
            }

            using var throttler = new SemaphoreSlim(MaxConcurrentEmailSends);
            var tasks = emailJobs.Select(async job =>
            {
                await throttler.WaitAsync();
                try
                {
                    await _emailService.SendEmailAsync(
                        job.To,
                        job.Subject,
                        job.Body);

                    return job;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Kontakt osoba nije obavijestena za pacijenta {PacijentId} nakon tri uzastopne propustene doze.",
                        job.PacijentId);

                    return null;
                }
                finally
                {
                    throttler.Release();
                }
            });

            var sentJobs = await Task.WhenAll(tasks);
            foreach (var sentJob in sentJobs.Where(job => job != null))
            {
                foreach (var doza in sentJob!.Doze)
                {
                    doza.KontaktObavijestPoslana = true;
                }
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

        private async Task IncrementDailyStatisticsAsync(
            IReadOnlyList<TerapijskaDoza> doze,
            StatusDoze status)
        {
            foreach (var group in doze
                .Where(d => !string.IsNullOrWhiteSpace(d.Terapija?.PacijentId))
                .GroupBy(d => new
                {
                    PacijentId = d.Terapija!.PacijentId!,
                    Datum = GetOriginalDoseTime(d).Date
                }))
            {
                var statistic = await _context.PacijentDnevneStatistike
                    .FirstOrDefaultAsync(s =>
                        s.PacijentId == group.Key.PacijentId &&
                        s.Datum == group.Key.Datum);

                if (statistic == null)
                {
                    statistic = new PacijentDnevnaStatistika
                    {
                        PacijentId = group.Key.PacijentId,
                        Datum = group.Key.Datum
                    };

                    _context.PacijentDnevneStatistike.Add(statistic);
                }

                if (status == StatusDoze.Uzeto)
                {
                    statistic.BrojUzetih += group.Count();
                }
                else if (status == StatusDoze.Propusteno)
                {
                    statistic.BrojPropustenih += group.Count();
                }
            }
        }

        private static DateTime GetOriginalDoseTime(TerapijskaDoza doza)
        {
            return doza.OriginalnoVrijemeUzimanja ?? doza.VrijemeUzimanja;
        }

        private static bool IsMissedForContactNotification(TerapijskaDoza doza)
        {
            return doza.Status == StatusDoze.Propusteno ||
                doza.BrojOdgoda >= 2;
        }

        private string GetLoginUrl()
        {
            return $"{EmailSettings.PublicAppUrl}/Account/Login";
        }

        private string BuildDoseReminderBody(string naziv, DateTime vrijemeUzimanja)
        {
            var encodedNaziv = WebUtility.HtmlEncode(naziv);
            var loginUrl = WebUtility.HtmlEncode(GetLoginUrl());

            return
                "<p>Za 5 minuta trebate uzeti lijek " +
                $"<strong>{encodedNaziv}</strong>.</p>" +
                "<p>Planirano vrijeme uzimanja: " +
                $"<strong>{vrijemeUzimanja:dd.MM.yyyy HH:mm}</strong>.</p>" +
                $"<p><a href=\"{loginUrl}\" target=\"_blank\" rel=\"noopener\">Kliknite ovdje za prijavu u TimeForPill</a></p>";
        }

        private sealed record DoseReminderEmail(
            TerapijskaDoza Doza,
            string To,
            string Subject,
            string Body,
            bool IsBodyHtml);

        private sealed record ContactEmailNotification(
            IReadOnlyList<TerapijskaDoza> Doze,
            string To,
            string Subject,
            string Body,
            string? PacijentId);
    }
}
