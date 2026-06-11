using System.ComponentModel.DataAnnotations;
using TimeForPill.Models;

namespace TimeForPill.ViewModels
{
    public enum KorisnickaUloga
    {
        Pacijent,
        Ljekar,
        Administrator
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Odaberite tip korisnika.")]
        [Display(Name = "Prijavljujem se kao")]
        public KorisnickaUloga Uloga { get; set; } = KorisnickaUloga.Pacijent;

        [Required(ErrorMessage = "Email je obavezan.")]
        [EmailAddress(ErrorMessage = "Unesite ispravnu email adresu.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lozinka je obavezna.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Odaberite tip korisnika.")]
        [Display(Name = "Registrujem se kao")]
        public KorisnickaUloga Uloga { get; set; } = KorisnickaUloga.Pacijent;

        [Required(ErrorMessage = "Ime je obavezno.")]
        [RegularExpression(@"^[A-Za-zČĆŽŠĐčćžšđ]+$",
            ErrorMessage = "Ime smije sadrzavati samo slova")]
        [StringLength(50, MinimumLength = 2)]
        public string Ime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Prezime je obavezno.")]
        [RegularExpression(@"^[A-Za-zČĆŽŠĐčćžšđ]+$",
            ErrorMessage = "Prezime smije sadrzavati samo slova")]
        [StringLength(50, MinimumLength = 2)]
        public string Prezime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Datum rodjenja je obavezan.")]
        [DataType(DataType.Date)]
        [Display(Name = "Datum rodjenja")]
        public DateTime DatumRodjenja { get; set; } = DateTime.Today.AddYears(-18);

        [Required(ErrorMessage = "Spol je obavezan.")]
        public Spol Spol { get; set; }

        [Required(ErrorMessage = "Email je obavezan.")]
        [EmailAddress(ErrorMessage = "Unesite ispravnu email adresu.")]
        public string Email { get; set; } = "pacijent@gmail.com";

        [Required(ErrorMessage = "Lozinka je obavezna.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Lozinka mora imati najmanje 6 karaktera.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Potvrdite lozinku.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Lozinke se ne poklapaju.")]
        [Display(Name = "Potvrda lozinke")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Specijalizacija")]
        public Specijalizacija? Specijalizacija { get; set; }

        [Display(Name = "Ime kontakt osobe")]
        [RegularExpression(@"^[A-Za-zČĆŽŠĐčćžšđ]+$",
            ErrorMessage = "Ime smije sadrzavati samo slova")]
        public string KontaktIme { get; set; } = "Kontakt";

        [Display(Name = "Prezime kontakt osobe")]
        [RegularExpression(@"^[A-Za-zČĆŽŠĐčćžšđ]+$",
            ErrorMessage = "Prezime smije sadrzavati samo slova")]
        public string KontaktPrezime { get; set; } = "Osoba";

        [EmailAddress(ErrorMessage = "Unesite ispravnu email adresu kontakt osobe.")]
        [Display(Name = "Email kontakt osobe")]
        public string KontaktEmail { get; set; } = "ebosnjakov2@etf.unsa.ba";

        [Phone(ErrorMessage = "Unesite ispravan broj telefona.")]
        [Display(Name = "Telefon kontakt osobe")]
        public string KontaktTelefon { get; set; } = "061000000";
    }
}
