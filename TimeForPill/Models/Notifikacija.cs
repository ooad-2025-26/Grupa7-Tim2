using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeForPill.Models
{
    public class Notifikacija
    {
        [Key]
        public int Id { get; set; }

        public string Naziv { get; set; }
        public string Poruka { get; set; }

        [ForeignKey("Terapija")]
        public int TerapijaId { get; set; }
        public Terapija Terapija { get; set; }
    }
}