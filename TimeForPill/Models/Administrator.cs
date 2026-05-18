using System.ComponentModel.DataAnnotations;

namespace TimeForPill.Models
{
    public class Administrator : Korisnik
    {
        [Required(ErrorMessage = "Datum imenovanja je obavezan.")]
        [DataType(DataType.Date)]
        [Display(Name = "Datum imenovanja")]
        public DateTime datumImenovanja { get; set; }
    }
}
