namespace TimeForPill.Models
{
    public class Lijek
    {

        public int Id { get; set; }
        public string Naziv { get; set; } = string.Empty;
        public string Kategorija { get; set; } = string.Empty;
        public string Slika { get; set; } = string.Empty;

        public Lijek(string naziv, string kategorija, string slika)
        {
            Naziv = naziv;
            Kategorija = kategorija;
            Slika = slika;
        }

        public int DajId() => Id;
    }
}