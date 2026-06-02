using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeForPill.Models
{
    public class Zahtjev
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Naziv zahtjeva je obavezan.")]
        [StringLength(100, MinimumLength = 2)]
        public string Naziv { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sadrzaj zahtjeva je obavezan.")]
        [StringLength(1000, MinimumLength = 5)]
        [Display(Name = "Sadrzaj")]
        public string Sadrzaj { get; set; } = string.Empty;

        [Display(Name = "Terapija")]
        [ForeignKey(nameof(Terapija))]
        public int? TerapijaId { get; set; }

        public Terapija? Terapija { get; set; }

        [Required(ErrorMessage = "Status zahtjeva je obavezan.")]
        public StatusZahtjeva Status { get; set; }
    }
}
