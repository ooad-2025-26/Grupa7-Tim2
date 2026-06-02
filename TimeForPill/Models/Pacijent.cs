using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeForPill.Models
{
    public class Pacijent : ApplicationUser
    {
        [Display(Name = "Kontakt osoba")]
        [ForeignKey(nameof(KontaktOsoba))]
        public int? KontaktOsobaId { get; set; }

        [Display(Name = "Kontakt osoba")]
        public KontaktOsoba KontaktOsoba { get; set; } = new KontaktOsoba();

        [Display(Name = "Ljekar")]
        [ForeignKey(nameof(Ljekar))]
        public string? LjekarId { get; set; }

        public Ljekar? Ljekar { get; set; }

        [Display(Name = "Legacy terapija")]
        public int? TerapijaId { get; set; }
    }
}
