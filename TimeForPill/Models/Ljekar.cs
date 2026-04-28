using TimeForPill.Enums;

namespace TimeForPill.Models
{
    public class Ljekar : Korisnik
    {
        public Specijalizacija Specijalizacija { get; set; }

        public void PregledZahtjeva()
        {
            
        }

        public bool OdaberiZahtjev(int zahtjevId)
        {
            
            return true;
        }

        public int DajBrojZahtjeva() { return 0; }

        public int DajBrojObradjenihZahtjeva() { return 0; }
    }
}