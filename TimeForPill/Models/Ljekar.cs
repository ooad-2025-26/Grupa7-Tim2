using System.ComponentModel.DataAnnotations;

namespace TimeForPill.Models
{
    public class Ljekar : Korisnik
    {
        [Required(ErrorMessage = "Specijalizacija je obavezna.")]
        public Specijalizacija Specijalizacija { get; set; }

        public ICollection<Pacijent> Pacijenti { get; set; } = new List<Pacijent>();
    }
}
