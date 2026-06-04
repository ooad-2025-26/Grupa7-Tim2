using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeForPill.Models
{
    public class TerapijskaDoza
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Terapija")]
        [ForeignKey(nameof(Terapija))]
        public int TerapijaId { get; set; }

        public Terapija? Terapija { get; set; }

        [Range(1, 10000)]
        [Display(Name = "Redni broj doze")]
        public int RedniBroj { get; set; }

        [Required]
        [Display(Name = "Vrijeme uzimanja")]
        public DateTime VrijemeUzimanja { get; set; }

        [Display(Name = "Originalno vrijeme uzimanja")]
        public DateTime? OriginalnoVrijemeUzimanja { get; set; }

        [Required]
        [Display(Name = "Vrijeme podsjetnika")]
        public DateTime VrijemePodsjetnika { get; set; }

        [Required]
        public StatusDoze Status { get; set; } = StatusDoze.Cekanje;

        [Display(Name = "Vrijeme evidentiranja")]
        public DateTime? VrijemeEvidentiranja { get; set; }

        public bool EmailPodsjetnikPoslan { get; set; }

        public bool KontaktObavijestPoslana { get; set; }

        [Range(0, 2)]
        public int BrojOdgoda { get; set; }
    }
}
