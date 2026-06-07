# TimeForPill - Informacioni sistem za pracenje terapije

## O projektu

TimeForPill je web aplikacija namijenjena pacijentima, ljekarima i administratorima radi lakseg pracenja terapija, doza lijekova i zahtjeva za obnovu terapije.

Aplikacija omogucava:

- registraciju i prijavu korisnika kroz uloge Pacijent, Ljekar i Administrator,
- kontrolu pristupa tako da neautentifikovani korisnici ne mogu pristupiti aplikaciji,
- dodavanje terapije na osnovu kataloga lijekova koji odrzava ljekar,
- generisanje pojedinacnih doza na osnovu ukupnog broja doza i intervala uzimanja,
- rucni ili automatski odabir vremena prve doze terapije,
- pravilo da u istom terminu ne mogu biti vise od dvije doze,
- izbjegavanje termina izmedju 23:00 i 06:00,
- prikaz Home ekrana pacijenta sa brojem lijekova danas, uzetih doza, propustenih doza i sljedecom terapijom,
- oznacavanje pojedinacne doze kao uzete,
- odgadjanje pojedinacne doze za 30 minuta,
- automatsko oznacavanje doze kao propustene ako nije uzeta u roku od 1 sat ili ako je odgodjena dva puta,
- slanje email podsjetnika pacijentu 5 minuta prije svake doze,
- slanje email upozorenja kontakt osobi kada pacijent propusti 3, 6, 9 ili vise uzastopnih doza,
- slanje zahtjeva ljekaru za obnovu terapije,
- automatsko produzavanje terapije nakon sto ljekar potvrdi zahtjev za obnovu,
- slanje email obavijesti pacijentu kada ljekar potvrdi ili odbije zahtjev,
- pregled sedmicnog rasporeda terapija,
- pregled historije uzimanja lijekova kroz dnevnu, mjesecnu i godisnju statistiku,
- prikaz aktivnih lijekova pacijenta ljekaru kroz sekciju Moji pacijenti,
- administraciju pacijenata i ljekara kroz administratorski panel,
- pracenje administratorskih akcija nad korisnickim nalozima.

Cilj sistema je da pacijentu pomogne da redovno uzima terapiju, da kontakt osoba bude obavijestena u kriticnim situacijama, a da ljekar ima bolji uvid u aktivne terapije i zahtjeve svojih pacijenata.

## Online pristup

Aplikacija je hostovana na sljedecem linku:

[http://timeforpill.runasp.net/](http://timeforpill.runasp.net/)

Hosting se koristi preko MonsterASP/RunASP okruzenja.

## Testni korisnici

Login ekran podrzava izbor uloge prije prijave. Za testiranje se mogu koristiti sljedeci nalozi ako su kreirani u bazi:

| Uloga | Email | Lozinka |
| --- | --- | --- |
| Administrator | ehamidovic1@etf.unsa.ba | Administrator123 |
| Ljekar | ahakanovic1@etf.unsa.ba | Ljekar123 |
| Pacijent | ebosnjakov2@etf.unsa.ba | Pacijent123 |

Napomena: ako ovi nalozi ne postoje u bazi, potrebno ih je kreirati kroz ekran za registraciju ili administratorski panel. Lozinka mora imati najmanje 6 karaktera, veliko slovo, malo slovo i broj.

## Uloge u sistemu

### Pacijent

Pacijent moze:

- pregledati dnevni status terapije,
- dodati novu terapiju iz kataloga lijekova,
- odabrati automatski ili rucni pocetak terapije,
- pregledati svoje lijekove u grid prikazu,
- oznaciti dozu kao uzetu,
- odgoditi dozu za 30 minuta,
- poslati zahtjev ljekaru za obnovu aktivne terapije,
- pregledati sedmicni raspored terapija,
- pregledati historiju uzimanja lijekova,
- urediti profil i kontakt osobu.

### Ljekar

Ljekar moze:

- pregledati broj zahtjeva, obradjenih zahtjeva i neobradjenih zahtjeva,
- pregledati listu zahtjeva sortiranu od najnovijih ka starijim,
- otvoriti detalje zahtjeva,
- potvrditi ili odbiti zahtjev za obnovu terapije,
- automatski produziti terapiju pacijentu potvrdom zahtjeva,
- dobiti email kada pacijent zatrazi obnovu terapije,
- pregledati svoje pacijente,
- vidjeti aktivne lijekove koje pacijent trenutno koristi,
- upravljati katalogom lijekova,
- urediti svoj profil.

### Administrator

Administrator moze:

- pregledati broj pacijenata,
- pregledati broj ljekara,
- pregledati broj svojih izvrsenih akcija,
- pregledati zadnje administrativne akcije,
- pregledati pacijente,
- dodati, urediti, pregledati detalje ili obrisati pacijenta,
- pregledati ljekare,
- dodati, urediti, pregledati detalje ili obrisati ljekara.

## Konekcijski string

Aplikacija koristi SQL Server bazu na MonsterASP/DatabaseASP hostingu.

Primjer konfiguracije:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=db54470.public.databaseasp.net; Database=db54470; User Id=db54470; Password=<lozinka-baze>; Encrypt=False; MultipleActiveResultSets=True;"
  }
}
```

Stvarna lozinka baze ne bi trebala biti javno objavljena u README fajlu. Za lokalni rad moze biti u `appsettings.json`, a za hosting je bolje koristiti konfiguraciju koju nudi hosting panel.

## Email servis

Aplikacija koristi Gmail SMTP servis za slanje email obavijesti.

Konfiguracija:

```json
{
  "EmailSettings": {
    "AppUrl": "http://timeforpill.runasp.net",
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "From": "timeforpill2026@gmail.com",
    "UserName": "timeforpill2026@gmail.com",
    "SenderName": "TimeForPill",
    "Password": "<gmail-app-password>"
  }
}
```

Za hosting je preporuceno podesiti environment varijable:

```text
EmailSettings__AppUrl=http://timeforpill.runasp.net
EmailSettings__SmtpServer=smtp.gmail.com
EmailSettings__Port=587
EmailSettings__EnableSsl=true
EmailSettings__SenderName=TimeForPill
EmailSettings__SenderEmail=timeforpill2026@gmail.com
EmailSettings__Username=timeforpill2026@gmail.com
EmailSettings__Password=<gmail-app-password>
```

Lozinka za Gmail treba biti Gmail App Password, a ne obicna lozinka za prijavu na Gmail nalog.

## Automatske obavijesti

Podsjetnici za doze se obradjuju kroz `DoseReminderWorker`, koji svake minute pokrece provjeru:

- da li postoje doze za koje treba poslati email pacijentu,
- da li postoje doze koje treba oznaciti kao propustene,
- da li kontakt osoba treba dobiti upozorenje zbog uzastopno propustenih doza.

Bitna napomena za hosting: background worker radi samo dok je aplikacija aktivna. Ako hosting uspava aplikaciju zbog neaktivnosti, email podsjetnici se nece slati dok se aplikacija ponovo ne pokrene. Zato je preporuceno ukljuciti `AlwaysRunning`/`Always On` ili koristiti eksterni ping/task servis koji redovno otvara aplikaciju.

## Tehnologije

- ASP.NET Core MVC
- ASP.NET Core Identity
- Entity Framework Core
- SQL Server
- Razor Views
- Bootstrap
- Gmail SMTP
- MonsterASP/RunASP hosting

## Pokretanje lokalno

1. Otvoriti projekat `TimeForPill`.
2. Provjeriti `DefaultConnection` u `appsettings.json`.
3. Provjeriti SMTP konfiguraciju ili postaviti `EmailSettings__Password` kroz user-secrets/environment variables.
4. Pokrenuti migracije ako je potrebno.
5. Pokrenuti aplikaciju:

```bash
dotnet run --project TimeForPill/TimeForPill.csproj
```

Prilikom pokretanja aplikacija pokusava izvrsiti migracije nad bazom i provjeriti konekciju.
