using System.ComponentModel.DataAnnotations;

namespace TimeForPill.Models
{
    public class Ljekar : Korisnik
    {
        public Specijalizacija Specijalizacija { get; set; }

        public List<Pacijent> Pacijenti { get; set; }
    }
}