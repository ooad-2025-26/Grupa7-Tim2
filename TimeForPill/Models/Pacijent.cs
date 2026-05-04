using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeForPill.Models
{
    public class Pacijent : Korisnik
    {
        [ForeignKey("KontaktOsoba")]
        public int KontaktOsobaId { get; set; }

        public KontaktOsoba KontaktOsoba { get; set; }

        [ForeignKey("Ljekar")]
        public int LjekarId { get; set; }

        public Ljekar Ljekar { get; set; }

        public List<Terapija> Terapije { get; set; }
    }
}