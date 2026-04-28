using TimeForPill.Enums;

namespace TimeForPill.Models
{
    public class Zahtjev
    {

        public int ZahtjevId { get; set; }
        public string Naziv { get; set; } = string.Empty;
        public string Sadrzaj { get; set; } = string.Empty;
        public int TerapijaId { get; set; }
        public StatusZahtjeva Status { get; set; }

        public Zahtjev(string naziv, string sadrzaj, int terapijaId)
        {
            Naziv = naziv;
            Sadrzaj = sadrzaj;
            TerapijaId = terapijaId;
            Status = StatusZahtjeva.Neobraden;
        }

        public bool PromijeniStatus(StatusZahtjeva noviStatus)
        {
            Status = noviStatus;
            return true;
        }
    }
}