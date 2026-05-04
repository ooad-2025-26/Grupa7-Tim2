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
        public Lijek Lijek { get; set; }

        [ForeignKey("Pacijent")]
        public int PacijentId { get; set; }
        public Pacijent Pacijent { get; set; }

        public List<Notifikacija> Notifikacije { get; set; }
    }
}