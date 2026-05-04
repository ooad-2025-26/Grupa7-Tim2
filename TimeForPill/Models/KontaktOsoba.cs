using System.ComponentModel.DataAnnotations;

namespace TimeForPill.Models
{
    public class KontaktOsoba
    {
        [Key]
        public int Id { get; set; }

        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string Email { get; set; }
        public string BrojTelefona { get; set; }

        public List<Pacijent> Pacijenti { get; set; }
    }
}