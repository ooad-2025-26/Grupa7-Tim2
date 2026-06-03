**Patterni ponasanja:**





**Observer Pattern (za modul Notifikacija)**

Problem: Kada se promijeni stanje Terapije, moramo ručno pozivati servis za obavještavanje( Email). To stvara čvrstu zavisnost gdje Terapija mora "znati" za sve servise.

Rješenje: Terapija emituje događaj, a servisi (npr. notifikacija) se sami pretplate na promjene.

Prednost: Slaba veza. Terapija ne mora poznavati implementaciju notifikacijskih servisa, što nam omogućava da lagano dodajemo nove kanale komunikacije.



**State Pattern (za modul Zahtjev)**

Problem: Zahtjev ima kompleksan životni ciklus (cekanje, odobren, odbijen). Upravljanje ovim statusima kroz if-else naredbe dovodi do "špageti koda" koji je podložan greškama.

Rješenje: Svako stanje zahtjeva postaje zasebna klasa (npr. StanjeOdobren) koja definiše pravila ponašanja za taj status.

Prednost: Eliminacija složenih uslovnih naredbi, svako stanje posjeduje izolovanu logiku, što drastično olakšava testiranje i dodavanje novih statusa.





**Strategy Pattern (za modul Validacija zahtjeva)**

Problem: Proces validacije zahtjeva postaje sve složeniji kako dodajemo nova pravila. Hardkodiranje provjera u jednoj metodi čini sistem krutim i nefleksibilnim.

Rješenje: Svako pravilo validacije izdvojeno je u zasebnu klasu (strategiju) koja implementira zajednički interfejs. Validaciju vršimo kroz listu ovih strategija.

Prednost: Dodavanje novog pravila validacije zahtijeva samo kreiranje nove klase, čime se poštuje princip da je sistem otvoren za proširenje, a zatvoren za modifikaciju.



**Chain of Responsibility Pattern (za modul Obrada Zahtjeva)**

Problem: Proces obrade zahtjeva zahtijeva niz koraka (validacija, provjera dostupnosti terapije, logovanje). Korištenje dugih if-else blokova za ove provjere čini kod nepreglednim i teško održivim.

Kako radi: Kreiramo niz objekata. Svaki objekat odlučuje da li će obraditi zahtjev ili ga proslijediti sljedećem u lancu.

Prednost: Omogućava dinamičko dodavanje ili promjenu redoslijeda koraka obrade bez uticaja na kod.



**Command Pattern (za modul Operacije/Akcije)**

Problem: Operacije poput "Potvrdi uzimanje lijeka" ili "Poništi terapiju" moraju podržavati logovanje i operaciju "undo" (poništi), što je teško postići direktnim pozivanjem metoda.

Kako radi: Pretvaramo svaku operaciju u samostalni objekt (npr. PotvrdiTerapijuCommand). Taj objekt sadrži sve potrebne parametre i metodu Execute().

Prednost: Lako stavljanje akcija u red čekanja (queue), podrška za undo/redo operacije i čista separacija između onoga ko poziva akciju i onoga ko je izvršava.



**Iterator Pattern (za modul Historija Terapija)**

Problem: Pacijent ima listu svih dosadašnjih terapija. Pristupanje ovoj kolekciji uz direktno izlaganje strukture liste može dovesti do narušavanja enkapsulacije.

Kako radi: Uvodimo iterator koji omogućava sekvencijalni pristup elementima kolekcije bez da korisnik zna da li je u pozadini niz, stek ili neka druga kolekcija.

Prednost: Obezbjeđuje jedinstven način prolaska kroz kolekcije, čineći kod otpornijim na promjene u strukturi podataka.



**Mediator Pattern (za modul Komunikacija sistema)**

Problem: Ljekar, Pacijent i Administrator često trebaju komunicirati, a ako bi svako znao za svakoga, dobili bismo haotičnu "špageti" mrežnu komunikaciju.

Kako radi: Uvodimo klasu KomunikacijskiMediator. Objekti ne komuniciraju direktno, već šalju poruke medijatoru koji ih prosljeđuje odgovarajućoj strani.

Prednost: Smanjuje čvrstu ovisnost između klasa, objekti komuniciraju bez poznavanja interne strukture drugih objekata.



**Visitor Pattern (za modul Statistika)**

Problem: Potrebno je generisati različite izvještaje o terapijama ili pacijentima bez da se mijenja logika samih klasa Pacijent ili Terapija svaki put kada dodamo novi tip izvještaja.

Kako radi: Operacije se definišu u "Visitor" klasama (npr. GodisnjiIzvjestajVisitor), koje posjećuju elemente strukture i izvršavaju operacije nad njima.

Prednost: Dodavanje novih operacija nad objektima bez promjene njihovih klasa, čime se poštuje OCP.



**Interpreter Pattern (za modul Definisanje Doza)**

Problem: Pacijent možda žele unositi doze terapije kroz jednostavne tekstualne instrukcije (npr. "2x1 dnevno"). Interpretacija takvog teksta kroz obične metode bi bila vrlo komplikovana.

Kako radi: Kreiramo sistem koji interpretira i izvršava gramatička pravila (instrukcije) definisane za specifičnu upotrebu.

Prednost: Omogućava korisniku fleksibilan način unosa podataka koji sistem lako pretvara u konkretne operacije.



**Memento Pattern (za modul Uređivanje Profila)**

Problem: Prilikom uređivanja profila, korisnik može napraviti grešku i htjeti se vratiti na prethodno sačuvano stanje podataka.

Kako radi: Kreiramo "memento" objekt koji čuva stanje podataka korisnika u određenom trenutku, omogućavajući povratak na to stanje ako korisnik se korisnik odluči za povratak.

Prednost: Pruža siguran mehanizam za poništavanje promjena i vraćanje na ranije stanje objekta bez direktnog izlaganja njegove unutrašnje strukture.

