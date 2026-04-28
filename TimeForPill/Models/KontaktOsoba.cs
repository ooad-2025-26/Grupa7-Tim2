namespace TimeForPill.Models
{
    public class KontaktOsoba
    {
        public string Ime { get; set; } = string.Empty;
        public string Prezime { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string BrojTelefona { get; set; } = string.Empty;

        public KontaktOsoba(string ime, string prezime, string email, string brojTelefona)
        {
            Ime = ime;
            Prezime = prezime;
            Email = email;
            BrojTelefona = brojTelefona;
        }
    }
}