using System.ComponentModel.DataAnnotations;

namespace TimeForPill.Models
{
    public abstract class Korisnik
    {
        [Key]
        public int Id { get; set; }

        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string Email { get; set; }
        public string Lozinka { get; set; }
        public DateTime DatumRodjenja { get; set; }
        public Spol Spol { get; set; }
    }
}