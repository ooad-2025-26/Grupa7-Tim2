**Patterni ponasanja:**





**1**

Problem: Kada se promijeni stanje Terapije, moramo ručno pozivati servis za obavještavanje( Email). To stvara čvrstu zavisnost gdje Terapija mora "znati" za sve servise.



Koristimo  Observer Pattern (za modul Notifikacija)



Rješenje: Terapija emituje događaj, a servisi (npr. notifikacija) se sami pretplate na promjene.



Prednost: Slaba veza. Terapija ne mora poznavati implementaciju notifikacijskih servisa, što nam omogućava da lagano dodajemo nove kanale komunikacije.



**2**

Problem: Zahtjev ima kompleksan životni ciklus (cekanje, odobren, odbijen). Upravljanje ovim statusima kroz if-else naredbe dovodi do "špageti koda" koji je podložan greškama.



Koristimo State Pattern (za modul Zahtjev)



Rješenje: Svako stanje zahtjeva postaje zasebna klasa (npr. StanjeOdobren) koja definiše pravila ponašanja za taj status.



Prednost: Eliminacija složenih uslovnih naredbi, svako stanje posjeduje izolovanu logiku, što drastično olakšava testiranje i dodavanje novih statusa.



**3**

Problem: Proces validacije zahtjeva postaje sve složeniji kako dodajemo nova pravila. Hardkodiranje provjera u jednoj metodi čini sistem krutim i nefleksibilnim.



Koristimo Strategy Pattern (za modul Validacija zahtjeva)



Rješenje: Svako pravilo validacije izdvojeno je u zasebnu klasu (strategiju) koja implementira zajednički interfejs. Validaciju vršimo kroz listu ovih strategija.



Prednost: Dodavanje novog pravila validacije zahtijeva samo kreiranje nove klase, čime se poštuje princip da je sistem otvoren za proširenje, a zatvoren za modifikaciju.





