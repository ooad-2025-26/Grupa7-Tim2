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

            modelBuilder.Entity<Lijek>().ToTable("Lijek");
            modelBuilder.Entity<Terapija>().ToTable("Terapija");
            modelBuilder.Entity<Zahtjev>().ToTable("Zahtjev");
            modelBuilder.Entity<Notifikacija>().ToTable("Notifikacija");
        }
    }
}