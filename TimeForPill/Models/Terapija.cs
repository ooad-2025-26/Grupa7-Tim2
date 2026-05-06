using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeForPill.Models
{
    public class Terapija
    {
        [Key]
        public int Id { get; set; }

        public StatusTerapije Status { get; set; }

        public string Naziv { get; set; }

        public DateTime Pocetak { get; set; }

        public DateTime Kraj { get; set; }

        public int DnevnaDoza { get; set; }

        [ForeignKey("Lijek")]
        public int LijekId { get; set; }

        [ForeignKey("Pacijent")]
        public int PacijentId { get; set; }

        [ForeignKey("Notifikacija")]
        public int NotifikacijaID { get; set; }
    }
}