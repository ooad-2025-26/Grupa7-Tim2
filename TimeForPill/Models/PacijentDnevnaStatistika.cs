using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeForPill.Models
{
    public class PacijentDnevnaStatistika
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Pacijent")]
        [ForeignKey(nameof(Pacijent))]
        public string PacijentId { get; set; } = string.Empty;

        public Pacijent? Pacijent { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Datum { get; set; }

        [Range(0, 10000)]
        public int BrojUzetih { get; set; }

        [Range(0, 10000)]
        public int BrojPropustenih { get; set; }
    }
}
