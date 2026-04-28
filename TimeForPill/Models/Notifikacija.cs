namespace TimeForPill.Models
{
    public class Notifikacija
    {

        public int NotifikacijaId { get; set; }
        public string Naziv { get; set; } = string.Empty;
        public string Poruka { get; set; } = string.Empty;
        public int TerapijaId { get; set; }

        public Notifikacija(string naziv, string poruka, int lijekId)
        {
            Naziv = naziv;
            Poruka = poruka;
            TerapijaId = lijekId;
        }

        public void PosaljiNotifikaciju()
        {
            
        }
    }
}