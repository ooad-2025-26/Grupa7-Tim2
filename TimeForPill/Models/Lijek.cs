using System.ComponentModel.DataAnnotations;

namespace TimeForPill.Models
{
    public class Lijek
    {
        [Key]
        public int Id { get; set; }

        public string Naziv { get; set; }
        public string Kategorija { get; set; }
        public string Slika { get; set; }

        public List<Terapija> Terapije { get; set; }
    }
}