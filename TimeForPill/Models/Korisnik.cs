using System.ComponentModel.DataAnnotations;

namespace TimeForPill.Models
{
    public abstract class Korisnik
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ime je obavezno.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Ime mora imati izmedju 2 i 50 karaktera.")]
        public string Ime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Prezime je obavezno.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Prezime mora imati izmedju 2 i 50 karaktera.")]
        public string Prezime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email je obavezan.")]
        [EmailAddress(ErrorMessage = "Unesite ispravnu email adresu.")]
        [StringLength(120, ErrorMessage = "Email moze imati najvise 120 karaktera.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lozinka je obavezna.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Lozinka mora imati najmanje 6 karaktera.")]
        [DataType(DataType.Password)]
        public string Lozinka { get; set; } = string.Empty;

        [Required(ErrorMessage = "Datum rodjenja je obavezan.")]
        [DataType(DataType.Date)]
        [Display(Name = "Datum rodjenja")]
        public DateTime DatumRodjenja { get; set; }

        [Required(ErrorMessage = "Spol je obavezan.")]
        public Spol Spol { get; set; }
    }
}
