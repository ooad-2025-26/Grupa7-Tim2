using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TimeForPill.Models
{
    public abstract class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Ime je obavezno.")]
        [StringLength(50, MinimumLength = 2,
            ErrorMessage = "Ime mora imati izmedju 2 i 50 karaktera.")]
        public string Ime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Prezime je obavezno.")]
        [StringLength(50, MinimumLength = 2,
            ErrorMessage = "Prezime mora imati izmedju 2 i 50 karaktera.")]
        public string Prezime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Datum rodjenja je obavezan.")]
        [DataType(DataType.Date)]
        [Display(Name = "Datum rodjenja")]
        public DateTime DatumRodjenja { get; set; }

        [Required(ErrorMessage = "Spol je obavezan.")]
        public Spol Spol { get; set; }
    }
}