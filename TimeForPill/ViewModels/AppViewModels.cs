using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TimeForPill.Models;

namespace TimeForPill.ViewModels
{
    public class PatientDashboardViewModel
    {
        public string Ime { get; set; } = string.Empty;
        public int BrojLijekovaDanas { get; set; }
        public int BrojUzetihDanas { get; set; }
        public int BrojPropustenihDanas { get; set; }
        public int ProgresDoSljedecegLijeka { get; set; }
        public string SljedeciLijekNaziv { get; set; } = "Nema aktivne terapije";
        public string SljedeciLijekVrijeme { get; set; } = "-";
        public string PreostaloDoSljedeceg { get; set; } = "-";
        public string? SljedeciLijekSlika { get; set; }
        public int? SljedecaTerapijaId { get; set; }
        public IReadOnlyList<MedicineListItemViewModel> DanasnjeTerapije { get; set; } =
            Array.Empty<MedicineListItemViewModel>();
    }

    public class MedicineListItemViewModel
    {
        public int TerapijaId { get; set; }
        public int? LijekId { get; set; }
        public string Naziv { get; set; } = string.Empty;
        public string Kategorija { get; set; } = string.Empty;
        public int DnevnaDoza { get; set; }
        public DateTime Pocetak { get; set; }
        public DateTime Kraj { get; set; }
        public StatusTerapije Status { get; set; }
        public string? Slika { get; set; }
        public string SljedecaDoza { get; set; } = "-";
    }

    public class MedicineFormViewModel
    {
        public int? TerapijaId { get; set; }
        public int? LijekId { get; set; }

        [Required(ErrorMessage = "Naziv lijeka je obavezan.")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Naziv lijeka")]
        public string Naziv { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kategorija je obavezna.")]
        [StringLength(80, MinimumLength = 2)]
        public string Kategorija { get; set; } = "Terapija";

        [Range(1, 20, ErrorMessage = "Dnevna doza mora biti izmedju 1 i 20.")]
        [Display(Name = "Doziranje")]
        public int DnevnaDoza { get; set; } = 1;

        [Required(ErrorMessage = "Datum pocetka je obavezan.")]
        [DataType(DataType.Date)]
        [Display(Name = "Pocetak terapije")]
        public DateTime Pocetak { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Datum kraja je obavezan.")]
        [DataType(DataType.Date)]
        [Display(Name = "Kraj terapije")]
        public DateTime Kraj { get; set; } = DateTime.Today.AddDays(7);

        [Display(Name = "Fotografija lijeka")]
        public IFormFile? SlikaFile { get; set; }

        public string? PostojecaSlika { get; set; }
    }

    public class TherapyOptionViewModel
    {
        public int TerapijaId { get; set; }
        public string Naziv { get; set; } = string.Empty;
        public int PreostaloDoza { get; set; }
    }

    public class RenewTherapyViewModel
    {
        [Required(ErrorMessage = "Odaberite lijek.")]
        [Display(Name = "Lijek")]
        public int? TerapijaId { get; set; }

        [Range(0, 10000, ErrorMessage = "Broj preostalih doza mora biti pozitivan.")]
        [Display(Name = "Preostalo doza")]
        public int PreostaloDoza { get; set; }

        [StringLength(1000, ErrorMessage = "Napomena moze imati najvise 1000 karaktera.")]
        public string? Napomena { get; set; }

        public IReadOnlyList<TherapyOptionViewModel> Terapije { get; set; } =
            Array.Empty<TherapyOptionViewModel>();
    }

    public class ProfileViewModel
    {
        public string? KorisnikId { get; set; }
        public KorisnickaUloga Uloga { get; set; }

        [Required(ErrorMessage = "Ime je obavezno.")]
        [StringLength(50, MinimumLength = 2)]
        public string Ime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Prezime je obavezno.")]
        [StringLength(50, MinimumLength = 2)]
        public string Prezime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Datum rodjenja je obavezan.")]
        [DataType(DataType.Date)]
        [Display(Name = "Datum rodjenja")]
        public DateTime DatumRodjenja { get; set; }

        [Required(ErrorMessage = "Email je obavezan.")]
        [EmailAddress(ErrorMessage = "Unesite ispravnu email adresu.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Specijalizacija")]
        public Specijalizacija? Specijalizacija { get; set; }

        public bool PrikaziKontaktOsobu { get; set; }

        [Display(Name = "Ime kontakt osobe")]
        public string KontaktIme { get; set; } = string.Empty;

        [Display(Name = "Prezime kontakt osobe")]
        public string KontaktPrezime { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Unesite ispravnu email adresu kontakt osobe.")]
        [Display(Name = "Email kontakt osobe")]
        public string KontaktEmail { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Unesite ispravan broj telefona.")]
        [Display(Name = "Telefon kontakt osobe")]
        public string KontaktTelefon { get; set; } = string.Empty;
    }

    public class ScheduleDayViewModel
    {
        public string NazivDana { get; set; } = string.Empty;
        public DateTime Datum { get; set; }
        public IReadOnlyList<ScheduleItemViewModel> Terapije { get; set; } =
            Array.Empty<ScheduleItemViewModel>();
    }

    public class ScheduleItemViewModel
    {
        public string Vrijeme { get; set; } = string.Empty;
        public string NazivLijeka { get; set; } = string.Empty;
        public StatusTerapije Status { get; set; }
        public string? Slika { get; set; }
    }

    public class HistoryViewModel
    {
        public string Period { get; set; } = "dnevna";
        public int Uzeto { get; set; }
        public int NijeUzeto { get; set; }
        public int Ukupno => Uzeto + NijeUzeto;
        public int ProcenatUzeto => Ukupno == 0 ? 0 : (int)Math.Round((double)Uzeto / Ukupno * 100);
        public int ProcenatNijeUzeto => 100 - ProcenatUzeto;
    }

    public class DoctorDashboardViewModel
    {
        public int BrojZahtjeva { get; set; }
        public int BrojObradjenihZahtjeva { get; set; }
        public int BrojNeobradjenihZahtjeva { get; set; }
        public IReadOnlyList<RequestListItemViewModel> ZadnjaCetiriPotvrdjena { get; set; } =
            Array.Empty<RequestListItemViewModel>();
    }

    public class RequestListItemViewModel
    {
        public int Id { get; set; }
        public string Naziv { get; set; } = string.Empty;
        public string Pacijent { get; set; } = string.Empty;
        public string Lijek { get; set; } = string.Empty;
        public StatusZahtjeva Status { get; set; }
    }

    public class RequestDetailsViewModel : RequestListItemViewModel
    {
        public string Sadrzaj { get; set; } = string.Empty;
    }

    public class AdminDashboardViewModel
    {
        public int BrojPacijenata { get; set; }
        public int BrojLjekara { get; set; }
        public int BrojIzvrsenihAkcija { get; set; }
        public IReadOnlyList<string> ZadnjeAkcije { get; set; } = Array.Empty<string>();
    }
}
