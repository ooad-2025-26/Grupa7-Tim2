using TimeForPill.Enums;

namespace TimeForPill.Models
{
    public abstract class Korisnik
    {
        public int Id { get; set; }
        public string Ime { get; set; } = string.Empty;
        public string Prezime { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Lozinka { get; set; } = string.Empty;
        public DateTime DatumRodjenja { get; set; }
        public Spol Spol { get; set; }

        public bool Prijava(string email, string lozinka)
        {
            return Email == email && Lozinka == lozinka;
        }

        public bool PromijeniLozinku(string staraLozinka, string novaLozinka)
        {
            return true;
        }
    }
}