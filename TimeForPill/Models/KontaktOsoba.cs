using System.ComponentModel.DataAnnotations;

namespace TimeForPill.Models
{
    public class KontaktOsoba
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ime kontakt osobe je obavezno.")]
        [StringLength(50, MinimumLength = 2)]
        public string Ime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Prezime kontakt osobe je obavezno.")]
        [StringLength(50, MinimumLength = 2)]
        public string Prezime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email kontakt osobe je obavezan.")]
        [EmailAddress(ErrorMessage = "Unesite ispravnu email adresu.")]
        [StringLength(120)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Broj telefona je obavezan.")]
        [Phone(ErrorMessage = "Unesite ispravan broj telefona.")]
        [StringLength(30, MinimumLength = 6)]
        [Display(Name = "Broj telefona")]
        public string BrojTelefona { get; set; } = string.Empty;
    }
}
