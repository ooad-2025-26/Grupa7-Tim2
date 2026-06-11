using System.ComponentModel.DataAnnotations;

namespace TimeForPill.Models
{
    public class AdminAkcija
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string AdministratorId { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string AdministratorNaziv { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string VrstaAkcije { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string TipRacuna { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string RacunId { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string RacunNaziv { get; set; } = string.Empty;

        [StringLength(256)]
        public string RacunEmail { get; set; } = string.Empty;

        public DateTime DatumAkcije { get; set; } = DateTime.Now;
    }
}
