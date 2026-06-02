using System.ComponentModel.DataAnnotations;

namespace TimeForPill.Models
{
    public class Lijek
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Naziv lijeka je obavezan.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Naziv mora imati izmedju 2 i 100 karaktera.")]
        public string Naziv { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kategorija je obavezna.")]
        [StringLength(80, MinimumLength = 2,
            ErrorMessage = "Kategorija mora imati izmedju 2 i 80 karaktera.")]
        public string Kategorija { get; set; } = string.Empty;

        [StringLength(260,
            ErrorMessage = "Putanja slike moze imati najvise 260 karaktera.")]
        public string? Slika { get; set; }
    }
}
