using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeForPill.Models
{
    public class Nuspojava
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Pacijent")]
        [ForeignKey(nameof(Pacijent))]
        public string PacijentId { get; set; } = string.Empty;

        public Pacijent? Pacijent { get; set; }

        [Display(Name = "Terapija")]
        [ForeignKey(nameof(Terapija))]
        public int? TerapijaId { get; set; }

        public Terapija? Terapija { get; set; }

        [Display(Name = "Lijek")]
        [ForeignKey(nameof(Lijek))]
        public int? LijekId { get; set; }

        public Lijek? Lijek { get; set; }

        [Required]
        [StringLength(100)]
        public string NazivLijeka { get; set; } = string.Empty;

        [StringLength(80)]
        public string Kategorija { get; set; } = string.Empty;

        [StringLength(260)]
        public string? Slika { get; set; }

        [StringLength(2000, ErrorMessage = "Opis nuspojave moze imati najvise 2000 karaktera.")]
        public string? Opis { get; set; }

        public bool BezNuspojava { get; set; }

        public DateTime DatumPrijave { get; set; } = DateTime.Now;
    }
}
