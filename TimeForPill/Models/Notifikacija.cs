using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeForPill.Models
{
    public class Notifikacija
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Naziv notifikacije je obavezan.")]
        [StringLength(100, MinimumLength = 2)]
        public string Naziv { get; set; } = string.Empty;

        [Required(ErrorMessage = "Poruka je obavezna.")]
        [StringLength(500, MinimumLength = 5)]
        public string Poruka { get; set; } = string.Empty;

        [Display(Name = "Terapija")]
        [ForeignKey(nameof(Terapija))]
        public int? TerapijaId { get; set; }

        public Terapija? Terapija { get; set; }
    }
}
