using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Models;

namespace TimeForPill.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Korisnik> Korisnici { get; set; }
        public DbSet<Pacijent> Pacijenti { get; set; }
        public DbSet<Ljekar> Ljekari { get; set; }
        public DbSet<Administrator> Administratori { get; set; }
        public DbSet<KontaktOsoba> KontaktOsobe { get; set; }
        public DbSet<Lijek> Lijekovi { get; set; }
        public DbSet<Terapija> Terapije { get; set; }
        public DbSet<Zahtjev> Zahtjevi { get; set; }
        public DbSet<Notifikacija> Notifikacije { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Korisnik>().ToTable("Korisnik");
            modelBuilder.Entity<Pacijent>().ToTable("Pacijent");
            modelBuilder.Entity<Ljekar>().ToTable("Ljekar");
            modelBuilder.Entity<Administrator>().ToTable("Administrator");

            modelBuilder.Entity<KontaktOsoba>().ToTable("KontaktOsoba");
            modelBuilder.Entity<Lijek>().ToTable("Lijek");
            modelBuilder.Entity<Terapija>().ToTable("Terapija");
            modelBuilder.Entity<Zahtjev>().ToTable("Zahtjev");
            modelBuilder.Entity<Notifikacija>().ToTable("Notifikacija");

            modelBuilder.Entity<Pacijent>()
                .HasOne(p => p.KontaktOsoba)
                .WithMany()
                .HasForeignKey(p => p.KontaktOsobaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Pacijent>()
                .HasOne(p => p.Ljekar)
                .WithMany(l => l.Pacijenti)
                .HasForeignKey(p => p.LjekarId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Terapija>()
                .HasOne(t => t.Lijek)
                .WithMany(l => l.Terapije)
                .HasForeignKey(t => t.LijekId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Terapija>()
                .HasOne(t => t.Pacijent)
                .WithMany(p => p.Terapije)
                .HasForeignKey(t => t.PacijentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notifikacija>()
                .HasOne(n => n.Terapija)
                .WithMany(t => t.Notifikacije)
                .HasForeignKey(n => n.TerapijaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Zahtjev>()
                .HasOne(z => z.Terapija)
                .WithMany(t => t.Zahtjevi)
                .HasForeignKey(z => z.TerapijaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
