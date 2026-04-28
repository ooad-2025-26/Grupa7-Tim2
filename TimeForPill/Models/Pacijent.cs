using TimeForPill.Enums;

namespace TimeForPill.Models
{
    public class Pacijent : Korisnik
    {
        public int KontaktOsobaId { get; set; }
        public int LjekarId { get; set; }

        public bool Registracija(Korisnik korisnik)
        {
            return true;
        }

        public void PogledajTerapije() { }

        public bool PrijaviNuspojavu(string poruka)
        {
            return true;
        }

        public bool PotvrdiUzimanjeLijeka(int terapijaId)
        {
            
            return true;
        }

        public bool PosaljiZahtjev(int zahtjevId)
        {
            return true;
        }

        public int DajBrojTerapija() { return 0; }

        public int DajIducuTerapiju()
        {
            return 0;
        }

        public int PregledHistorijeTerapije() { return 0; }
    }
}