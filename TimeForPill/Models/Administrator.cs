namespace TimeForPill.Models
{
    public class Administrator
    {
        public string Ime { get; set; } = string.Empty;
        public string Prezime { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Lozinka { get; set; } = string.Empty;

        public bool Registracija(Korisnik korisnik)
        {
            return true;
        }

        public bool Prijava(string email, string lozinka)
        {
            return Email == email && Lozinka == lozinka;
        }

        public bool PromijeniLozinku(string staraLozinka, string novaLozinka)
        {
            return true;
        }

        public bool PregledKorisnickihRacuna()
        {
            return true;
        }

        public bool BrisanjeKorisnickihRacuna(int korisnikId)
        {
            return true;
        }
    }
}