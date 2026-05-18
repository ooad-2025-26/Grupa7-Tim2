using System.ComponentModel.DataAnnotations;

namespace TimeForPill.Models
{
    public class Pacijent : Korisnik
    {
        [Display(Name = "Kontakt osoba")]
        public int? KontaktOsobaId { get; set; }

        [Display(Name = "Kontakt osoba")]
        public KontaktOsoba KontaktOsoba { get; set; } = new KontaktOsoba();

        [Display(Name = "Ljekar")]
        public int? LjekarId { get; set; }

        public Ljekar? Ljekar { get; set; }

        [Display(Name = "Legacy terapija")]
        public int? TerapijaId { get; set; }

        public ICollection<Terapija> Terapije { get; set; } = new List<Terapija>();
    }
}
