using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeForPill.Models
{
    public class Terapija
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Status terapije je obavezan.")]
        public StatusTerapije Status { get; set; }

        [Required(ErrorMessage = "Naziv terapije je obavezan.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Naziv terapije mora imati izmedju 2 i 100 karaktera.")]
        public string Naziv { get; set; } = string.Empty;

        [Required(ErrorMessage = "Datum pocetka je obavezan.")]
        [DataType(DataType.Date)]
        public DateTime Pocetak { get; set; }

        [Required(ErrorMessage = "Datum kraja je obavezan.")]
        [DataType(DataType.Date)]
        public DateTime Kraj { get; set; }

        [Display(Name = "Dnevna doza")]
        [Range(1, 20, ErrorMessage = "Dnevna doza mora biti izmedju 1 i 20.")]
        public int DnevnaDoza { get; set; }

        [Display(Name = "Lijek")]
        [ForeignKey(nameof(Lijek))]
        public int? LijekId { get; set; }

        public Lijek? Lijek { get; set; }

        [Display(Name = "Pacijent")]
        [ForeignKey(nameof(Pacijent))]
        public string? PacijentId { get; set; }

        public Pacijent? Pacijent { get; set; }

        [Display(Name = "Notifikacija")]
        public int? NotifikacijaID { get; set; }
    }
}
