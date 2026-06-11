using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Models;

namespace TimeForPill.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Pacijent> Pacijenti { get; set; }
        public DbSet<Ljekar> Ljekari { get; set; }
        public DbSet<Administrator> Administratori { get; set; }

        public DbSet<KontaktOsoba> KontaktOsobe { get; set; }
        public DbSet<Lijek> Lijekovi { get; set; }
        public DbSet<Terapija> Terapije { get; set; }
        public DbSet<Zahtjev> Zahtjevi { get; set; }
        public DbSet<Notifikacija> Notifikacije { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<AdminAkcija> AdminAkcije { get; set; }
        public DbSet<TerapijskaDoza> TerapijskeDoze { get; set; }
        public DbSet<PacijentDnevnaStatistika> PacijentDnevneStatistike { get; set; }
        public DbSet<Nuspojava> Nuspojave { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TPH inheritance
            modelBuilder.Entity<ApplicationUser>().ToTable("Korisnici");

            modelBuilder.Entity<Pacijent>();
            modelBuilder.Entity<Ljekar>();
            modelBuilder.Entity<Administrator>();

            modelBuilder.Entity<KontaktOsoba>().ToTable("KontaktOsobe");
            modelBuilder.Entity<Lijek>().ToTable("Lijekovi");
            modelBuilder.Entity<Terapija>().ToTable("Terapije");
            modelBuilder.Entity<Zahtjev>().ToTable("Zahtjevi");
            modelBuilder.Entity<Notifikacija>().ToTable("Notifikacije");
            modelBuilder.Entity<Ticket>().ToTable("Tickets");
            modelBuilder.Entity<AdminAkcija>().ToTable("AdminAkcije");
            modelBuilder.Entity<TerapijskaDoza>().ToTable("TerapijskeDoze");
            modelBuilder.Entity<PacijentDnevnaStatistika>().ToTable("PacijentDnevneStatistike");
            modelBuilder.Entity<Nuspojava>().ToTable("Nuspojave");

            modelBuilder.Entity<Pacijent>()
                .HasOne(p => p.KontaktOsoba)
                .WithMany()
                .HasForeignKey(p => p.KontaktOsobaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Pacijent>()
                .HasOne(p => p.Ljekar)
                .WithMany()
                .HasForeignKey(p => p.LjekarId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Terapija>()
                .HasOne(t => t.Lijek)
                .WithMany()
                .HasForeignKey(t => t.LijekId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Terapija>()
                .HasOne(t => t.Pacijent)
                .WithMany()
                .HasForeignKey(t => t.PacijentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notifikacija>()
                .HasOne(n => n.Terapija)
                .WithMany()
                .HasForeignKey(n => n.TerapijaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Zahtjev>()
                .HasOne(z => z.Terapija)
                .WithMany()
                .HasForeignKey(z => z.TerapijaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TerapijskaDoza>()
                .HasOne(d => d.Terapija)
                .WithMany()
                .HasForeignKey(d => d.TerapijaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PacijentDnevnaStatistika>()
                .HasOne(s => s.Pacijent)
                .WithMany()
                .HasForeignKey(s => s.PacijentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PacijentDnevnaStatistika>()
                .HasIndex(s => new { s.PacijentId, s.Datum })
                .IsUnique();

            modelBuilder.Entity<Nuspojava>()
                .HasOne(n => n.Pacijent)
                .WithMany()
                .HasForeignKey(n => n.PacijentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Nuspojava>()
                .HasOne(n => n.Terapija)
                .WithMany()
                .HasForeignKey(n => n.TerapijaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Nuspojava>()
                .HasOne(n => n.Lijek)
                .WithMany()
                .HasForeignKey(n => n.LijekId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Korisnik)
                .WithMany()
                .HasForeignKey(t => t.KorisnikId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
