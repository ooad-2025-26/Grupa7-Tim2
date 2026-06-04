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
        public string? SljedeciLijekVrijemeIso { get; set; }
        public string PreostaloDoSljedeceg { get; set; } = "-";
        public string? SljedeciLijekSlika { get; set; }
        public int? SljedecaDozaId { get; set; }
        public DosePopupViewModel? TrenutnaDoza { get; set; }
        public IReadOnlyList<MedicineListItemViewModel> DanasnjeTerapije { get; set; } =
            Array.Empty<MedicineListItemViewModel>();
    }

    public class DosePopupViewModel
    {
        public int DozaId { get; set; }
        public string NazivLijeka { get; set; } = string.Empty;
        public string VrijemeUzimanja { get; set; } = string.Empty;
        public string? VrijemeUzimanjaIso { get; set; }
        public string? Slika { get; set; }
    }

    public class MedicineListItemViewModel
    {
        public int TerapijaId { get; set; }
        public int? LijekId { get; set; }
        public string Naziv { get; set; } = string.Empty;
        public string Kategorija { get; set; } = string.Empty;
        public int DnevnaDoza { get; set; }
        public int UkupanBrojDoza { get; set; }
        public int IntervalSati { get; set; }
        public int UzeteDoze { get; set; }
        public int PropusteneDoze { get; set; }
        public int CekajuceDoze { get; set; }
        public DateTime Pocetak { get; set; }
        public DateTime Kraj { get; set; }
        public StatusDoze Status { get; set; }
        public string? Slika { get; set; }
        public string SljedecaDoza { get; set; } = "-";
    }

    public class MedicineCatalogOptionViewModel
    {
        public int Id { get; set; }
        public string Naziv { get; set; } = string.Empty;
        public string Kategorija { get; set; } = string.Empty;
        public string? Slika { get; set; }
    }

    public class MedicineFormViewModel
    {
        public int? TerapijaId { get; set; }

        [Required(ErrorMessage = "Odaberite lijek iz kataloga.")]
        [Display(Name = "Lijek")]
        public int? LijekId { get; set; }

        [Display(Name = "Naziv lijeka")]
        public string Naziv { get; set; } = string.Empty;

        public string Kategorija { get; set; } = "Terapija";

        [Range(1, 10000, ErrorMessage = "Ukupan broj doza mora biti izmedju 1 i 10000.")]
        [Display(Name = "Ukupan broj doza")]
        public int UkupanBrojDoza { get; set; } = 21;

        [Range(1, 168, ErrorMessage = "Interval mora biti izmedju 1 i 168 sati.")]
        [Display(Name = "Uzimati svakih sati")]
        public int IntervalSati { get; set; } = 8;

        public DateTime Pocetak { get; set; } = DateTime.Now;

        public DateTime Kraj { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "Fotografija lijeka")]
        public IFormFile? SlikaFile { get; set; }

        public string? PostojecaSlika { get; set; }

        public IReadOnlyList<MedicineCatalogOptionViewModel> DostupniLijekovi { get; set; } =
            Array.Empty<MedicineCatalogOptionViewModel>();
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
        public StatusDoze Status { get; set; }
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

    public class DoctorPatientListItemViewModel
    {
        public string PacijentId { get; set; } = string.Empty;
        public string ImePrezime { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int BrojAktivnihLijekova { get; set; }
        public int BrojCekajucihDoza { get; set; }
    }

    public class DoctorPatientMedicationViewModel
    {
        public string Pacijent { get; set; } = string.Empty;
        public IReadOnlyList<DoctorMedicationItemViewModel> Lijekovi { get; set; } =
            Array.Empty<DoctorMedicationItemViewModel>();
    }

    public class DoctorMedicationItemViewModel
    {
        public string Naziv { get; set; } = string.Empty;
        public string Kategorija { get; set; } = string.Empty;
        public int UkupanBrojDoza { get; set; }
        public int IntervalSati { get; set; }
        public int UzeteDoze { get; set; }
        public int PropusteneDoze { get; set; }
        public int CekajuceDoze { get; set; }
        public string SljedecaDoza { get; set; } = "-";
        public string Period { get; set; } = string.Empty;
    }
}
