using System.ComponentModel.DataAnnotations.Schema;

namespace TimeForPill.Models
{
    public class Pacijent : Korisnik
    {
        [NotMapped]
        public KontaktOsoba KontaktOsoba { get; set; }

        public int LjekarId { get; set; }

        public int TerapijaId { get; set; }
    }
}