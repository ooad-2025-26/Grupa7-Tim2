**Kreacijski Patterni**



**Trenutni problemi:**



Dupliranje logike: Kreiranje korisnika (Pacijent, Ljekar) na više mjesta u kodu stvara if-else strukture koje je teško pratiti.



Neefikasnost resursa: Svako otvaranje nove konekcije prema bazi podataka bespotrebno troši sistemske resurse.



Teško proširenje: Ako poželimo dodati novu ulogu u sistem (npr. farmaceut), moramo mijenjati kod na više lokacija, što povećava rizik od grešaka.



Kako bismo ovo riješili i kod učinili profesionalnijim, odlučili smo uvesti dva kreacijska paterna: Factory Method i Singleton.





**Factory Method:**

Umjesto da ručno instanciramo objekte (new Pacijent(), new Ljekar()), uveli smo klasu KorisnikFactory.



Kako radi: Klijentski kod sada samo pošalje zahtjev: "Kreiraj mi korisnika tipa Ljekar". Klasa KorisnikFactory unutar sebe rješava instanciranje i nakon toga vraća kreirani objekat.



Prednost: Sva logika kreiranja je na jednom mjestu. Ako u buducnosti dodamo novu ulogu, izmjenu radimo samo u klasi KorisnikFactory, dok ostatak aplikacije ostaje netaknut.





**Singleton:**

Kreiramo klasu BazaSingleton koja kreira jedinstvenu konekciju na bazu, i tu konekciju prosljedujemo ostalim klasama kada im zatreba.



Kako radi: Ova klasa ima mehanizam koji osigurava da tokom rada aplikacije postoji samo jedna aktivna konekcija. Ako drugi dio sistema zatraži konekciju, dobija referencu na već postojeću, umjesto da otvara novu.



Prednost: Ovim smo drastično smanjili opterećenje baze i optimizovali korištenje memorije.

