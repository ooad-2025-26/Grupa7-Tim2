using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeForPill.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Naslov je obavezan.")]
        [StringLength(100)]
        public string Naslov { get; set; }

        [Required(ErrorMessage = "Opis je obavezan.")]
        [StringLength(1000)]
        public string Opis { get; set; }

        public DateTime DatumKreiranja { get; set; }
            = DateTime.Now;

        public StatusTicketa Status { get; set; }
            = StatusTicketa.Otvoren;

        // PRIORITET
        public PrioritetTicketa Prioritet { get; set; }
            = PrioritetTicketa.Srednji;

        // FK PREMA KORISNIKU
        public string? KorisnikId { get; set; }

        [ForeignKey(nameof(KorisnikId))]
        public ApplicationUser? Korisnik { get; set; }
    }
}