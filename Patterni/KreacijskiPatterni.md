**Kreacijski Patterni**







**Factory Method:**



Problem: Dupliranje logike: Kreiranje korisnika (Pacijent, Ljekar) na više mjesta u kodu stvara if-else strukture koje je teško pratiti.



Umjesto da ručno instanciramo objekte (new Pacijent(), new Ljekar()), uveli smo klasu KorisnikFactory.



Kako radi: Klijentski kod sada samo pošalje zahtjev: "Kreiraj mi korisnika tipa Ljekar". Klasa KorisnikFactory unutar sebe rješava instanciranje i nakon toga vraća kreirani objekat.

Prednost: Sva logika kreiranja je na jednom mjestu. Ako u buducnosti dodamo novu ulogu, izmjenu radimo samo u klasi KorisnikFactory, dok ostatak aplikacije ostaje netaknut.





**Singleton:**



Problem: Neefikasnost resursa: Svako otvaranje nove konekcije prema bazi podataka bespotrebno troši sistemske resurse.



Kreiramo klasu BazaSingleton koja kreira jedinstvenu konekciju na bazu, i tu konekciju prosljedujemo ostalim klasama kada im zatreba.



Kako radi: Ova klasa ima mehanizam koji osigurava da tokom rada aplikacije postoji samo jedna aktivna konekcija. Ako drugi dio sistema zatraži konekciju, dobija referencu na već postojeću, umjesto da otvara novu.

Prednost: Ovim smo drastično smanjili opterećenje baze i optimizovali korištenje memorije.





**Prototype Pattern:**



Problem: Klasa Terapija je vrlo kompleksna. Kada pacijent želi da kreira novu terapiju koja je skoro identična postojećoj, ručno ponavljanje svih parametara je podložno greškama i neefikasno.



Kako radi: Implementiramo metodu Clone() unutar klase Terapija. Pacijent uzima postojeću terapiju, klonira je, i samo mijenja neophodne parametre poput datuma početka ili doze lijeka.

Prednost: Drastično ubrzava proces kreiranja sličnih objekata i izbjegava ponovno učitavanje podataka iz baze ako je objekat već u memoriji.



**Abstract Factory Pattern:**



Problem: Sistem ima različite entitete koji su međusobno povezani: Terapija, Lijek i Notifikacija. Ako bi se sistem proširio na više različitih vrsta ustanova, pravila kreiranja ovih objekata bi se razlikovala zavisno od tipa ustanove.



Kako radi: Umjesto obične fabrike, pravimo ITerapijskiSistemFactory. Ova fabrika ima metode za kreiranje čitave familije objekata: KreirajTerapiju(), KreirajLijek(), KreirajNotifikaciju().

Prednost: Obezbjeđuje da objekti koji se koriste zajedno budu kompatibilni unutar svoje grupe, bez da klijentski kod zna detalje o konkretnim klasama koje se kreiraju.



**Builder Pattern:**



Problem:Klase poput Pacijent ili Terapija imaju mnogo atributa. Ponekad za kreiranje objekata ovih klasa trebaju samo osnovne informacije, a nekad i dodatne informacije. Klasični konstruktori postaju predugački i nepregledni.



Kako radi: Uvodimo PacijentBuilder. Klijent koristi metodu koja omogućava kreiranje objekta: new PacijentBuilder().SetIme("Hamo").SetKontaktOsoba(osoba).Build().

Prednost: Kod postaje čitljiv i pregledan. Objekat se gradi korak po korak i osigurava se da se objekat ne može instancirati ako nisu zadovoljeni minimalni uslovi.

