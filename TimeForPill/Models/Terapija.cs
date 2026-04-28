using TimeForPill.Enums;

namespace TimeForPill.Models
{
    public class Terapija
    {
        public int TerapijaId { get; set; }
        public StatusTerapije Status { get; set; }
        public string Naziv { get; set; } = string.Empty;
        public DateTime Pocetak { get; set; }
        public DateTime Kraj { get; set; }
        public int DnevnaDoza { get; set; }
        public int LijekId { get; set; }
        public int PacijentId { get; set; }

        public void GenerisiTermine()
        {
        }

        public bool Azuriraj()
        {
            return true;
        }

        public bool Deaktiviraj()
        {
            return true;
        }
    }
}   