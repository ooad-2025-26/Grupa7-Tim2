using System.ComponentModel.DataAnnotations;

namespace TimeForPill.Models
{
    public class Ljekar : ApplicationUser
    {
        [Required(ErrorMessage = "Specijalizacija je obavezna.")]
        public Specijalizacija Specijalizacija { get; set; }
    }
}
