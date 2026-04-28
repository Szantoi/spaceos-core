# SpaceOS — Termék-manifesztum
## Miért létezik ez a rendszer, és hogyan döntünk, amikor valami kétséges

> **Verzió:** v1.0 — 2026-04-23
> **Szerző:** Gábor (Founder & Architect)
> **Státusz:** ÉLŐ DOKUMENTUM
> **Nyelv:** magyar (technikai architektúra-dokumentumok angolul maradnak)
> **Hatály:** Minden jövőbeli architektúra-döntés, ADR, product review és onboarding anyag ehhez igazodik. Ha valami ellentmond a manifesztumnak, **a manifesztum nyer**.

---

## Előhang

Ezt a dokumentumot nem marketingre írtam. Önmagamnak írtam, és azoknak, akik a rendszerhez csatlakoznak — hogy három hónap vagy három év múlva is tudjuk, miért csináljuk, amit csinálunk, és miért nem hajlunk meg, amikor a gyors pénz vagy a felszínes siker erre csábít.

A SpaceOS nem egy szoftver. A SpaceOS egy válasz egy konkrét kérdésre: *miért kell egy asztalosnak évente több ezer eurót fizetnie azért, hogy a saját munkáját digitálisan kezelhesse?* A válaszom: nem kell. És ha ma még muszáj, akkor ez az állapot nem természeti törvény, hanem iparági tehetetlenség — amit fel lehet oldani.

Ez a dokumentum az iránytű. Rövid, szándékosan. Ha 30 oldalas lenne, senki nem olvasná el — beleértve magamat, három hónap múlva.

---

## 1. Miért létezik ez a rendszer

A faipar egy évszázados szakma, amelyet az elmúlt 30 évben a szoftveripar nem szolgált ki, hanem kihasznált. A professzionális CAD-licencek, a gyártásirányító szoftverek, a CNC post-processorok, az ERP-integrációk egyenként több ezer — gyakran tíz- vagy százezer — eurós költségtétel. Ezek a rendszerek a nagyvállalatoknak épültek, a KKV-k utánuk kullognak, a hobbisták és a diákok pedig kizárva maradnak.

Eközben a digitális alapok — hálózat, szabványos fájlformátumok, nyílt protokollok, felhőalapú tárolás — mindenki számára elérhetők. A barrier nem technológiai, hanem üzleti. A szoftvergyártók **szándékosan drágán tartják** azt, ami lehetne majdnem ingyen.

A SpaceOS úgy épül, hogy ez a barrier eltűnjön. Nem azzal, hogy "olcsóbb" legyen, hanem azzal, hogy **az érték más helyen születik**: a platform maga ingyenes, mert a hálózatban zajló tranzakciók és az ökoszisztéma-partnerek termelik a fenntartás forrását, nem a hozzáférés díja.

---

## 2. Alapelvek — a 10 tézis

Ezek non-negotiable-ok. Ha egy architekturális vagy üzleti döntés ellentmond valamelyiknek, **a döntés változik meg, nem a tézis**.

### 1. Az alkotás szabad.
Aki csinálni akar valamit — egy polcot, egy konyhát, egy nappalit, egy szerelőpadot —, annak az eszközök ne az útjában álljanak. A SpaceOS alapszintű funkciói mindig ingyenesek. Pontosan ingyenesek, nem "14 napig ingyenesek", nem "3 projekt után fizess", nem "korlátozott funkcionalitással ingyenesek". Az alapeszközök ingyenesek, mindig.

### 2. A fizetős rétegnek tényleg többet kell adnia.
Aki fizet, az nem az alapfunkciókért fizet. Az alapfunkciók ingyenesek. Aki fizet, az **piaci hozzáférésért, hálózati értékért, enterprise-szolgáltatásért** fizet — és amit kap, az mérhetően többet ér, mint amit ad érte. Ha ez nincs így, a pricing hibás, nem a user.

### 3. A felhasználó adata a felhasználó tulajdona.
A SpaceOS-ba került minden adat — rajz, paraméter, BOM, árajánlat — a felhasználóé. Exportálható, bármikor, teljes tartalommal, szabványos formátumban. Nem tartunk senkit túszul az adataival.

### 4. Nem áruljuk a felhasználóinkat.
Nincs fizetett hirdetés a rendszerben. Nincs adateladás harmadik félnek. Nincs profilépítés marketing célra. Az aggregált statisztikai adatokat — ha készítünk ilyet — **nyilvánosan megosztjuk mindenkivel**, beleértve a felhasználókat. A transzparencia az alapértelmezett, nem a privacy opcionális.

### 5. Mindenki a saját eszközét hozhatja.
Nincs "SpaceOS-only" CAD-kényszer. Aki AutoCAD-et használ, az használja azt. Aki BricsCAD-ot, GstarCAD-ot, ZWCAD-et, CADian-t — az is. Aki Inventort Woodwork Design-nel vagy SolidWorks-öt SWOOD-dal, aki Fusion 360-at, aki SketchUp-ot, aki Blendert — **mindegyik** elsőrangú állampolgár a rendszerben. Az integráció a mi felelősségünk, nem a felhasználóé.

### 6. A skeleton-alapú tervezés a támogatott munkamód.
A parametrikus, master-part-alapú, top-down modellezés nem opció, hanem a támogatott elsődleges munkamód. A CAD-rendszerek natív matematikai motorjait használjuk a számításra — nem helyettesítjük, nem gyengítjük. A SpaceOS a tervezésen felül értéket ad, nem alatta.

### 7. A rendszer API-first, ember-barát és gép-barát egyszerre.
Minden funkció, amit ember használni tud a felületen, elérhető gépi interfészen is. Ez nem feature, ez szerkezeti elv. Egy automatizációs script, egy integrációs partner, egy LLM-agent ugyanolyan elsőrangú kliens, mint egy ember. Ettől a rendszer programozható, automatizálható, és természetesen beszél azokon a nyelveken, amelyeken a jövő munkafolyamatai zajlanak majd.

### 8. Az AI eszköz, nem cél.
Nem "AI-powered" rendszer vagyunk. Nem azért építünk AI-funkciót, hogy a marketing-deckben jól nézzen ki. De minden eszközt úgy tervezünk, hogy egy AI is tudja értelmesen használni — és ez a gondolkodás a kódunk **DNS-e**, nem rajtafekvő réteg. Ha egy feature-t nem tudunk úgy megfogalmazni, hogy egy LLM is végre tudja hajtani, akkor a feature nem elég tiszta.

### 9. A hálózat az érték, nem a termék.
A SpaceOS értéke abban van, hogy **mennyi résztvevő között mennyi dolog történik**. Egy tervező, egy asztalos, egy beszállító, egy megrendelő, egy gépgyártó között. A legfontosabb mérőszám nem az MRR, nem az ARR, nem a user-szám — hanem a **network density**: egy átlagos projektben hány résztvevő van jelen a rendszerben.

### 10. A verseny szakmai tisztelettel kezelendő.
A MaxCut, a Microvellum, a CabinetVision, az Imos, a TopSolid Wood — jó termékek, okos emberek építették. Nem ellenségek, nem ellenfelek. Ha a modellünk működik és ezzel ők is fejlődnek, az siker — nekik is, a piacnak is, nekünk is. Ha valamelyikük bajba kerül és segítségre szorul, segítünk. Ez az iparág elfér mindenkinek.

---

## 3. Kikért létezik

Ha ezek közül bármelyik vagy, ez neked épül:

- **A hobbista**, aki a garázsban barkácsol, és nem akar évi több száz eurót fizetni azért, hogy egy polcrendszer BOM-ját kiszámolja.
- **A diák**, aki most tanul faipari tervezést, és nincs iskolai szoftverlicence.
- **A szabadúszó alkalmazott**, aki hétvégén konyhákat vállal kiegészítésképp.
- **A frissen induló egyéni vállalkozó**, aki ki akar lépni az alkalmazotti létből, de még nincs 20 000 EUR-ja szoftverbefektetésre.
- **A KKV asztalosmester**, akinek 5-15 emberes műhelye van, és szeretné, ha a beszerzés, tervezés, gyártás, számlázás egy helyen látszódna.
- **A nagyvállalat**, amelyik ERP-vel, BIM-mel, IFC-vel dolgozik, és integrálnia kell a kisebb beszállítóival.
- **A kereskedő, a beszállító, a gépgyártó**, aki értéket tud hozzáadni ehhez a hálózathoz — és üzleti haszonnal jár belőle.
- **A tervező-iroda és a belsőépítész**, aki projekteken dolgozik, és szüksége van a faipari partnereivel egy közös nyelvre.

Egy rendszerben. Egy bejelentkezéssel. Egy adatstruktúrában.

---

## 4. Mit nem csinálunk (soha)

Ez a lista ugyanolyan fontos, mint amit csinálunk. Ezek az **anti-feature-ök** — tiltott funkciók, bármi áron:

- **Nem árulunk hirdetést.** A felület reklámmentes. Marad.
- **Nem adjuk el a felhasználói adatokat.** Nem névre, nem cégre, nem "aggregáltan pénzért", sehogy.
- **Nem építünk fogyasztói profilt marketing célra.** Nem követünk senkit az oldalon kívül, nem építünk viselkedési adatbázist értékesítésre.
- **Nem zárjuk rabságba az adatot.** Ha valaki holnap ki akar szállni és mindent magával vinni, megteheti, azonnal, szabványos formátumban.
- **Nem tartunk funkciót túszul.** Nem lesz "ha nem fizetsz 48 órán belül, elveszíted az adataid" e-mail. Soha. Ingyenes marad ingyenes, akkor is, ha évek múlva térsz vissza.
- **Nem teszünk paywall-t az alapfunkciókra.** A CAD-integráció, a BOM, a cutlist, az alap-nesting, a ProductTemplate-használat — ingyen van, marad.
- **Nem csinálunk dark pattern-t.** Nincs rejtett megújítás, nincs becsapós lemondási útvonal, nincs "véletlenül beállított" upgrade.
- **Nem osztjuk meg a felhasználóinkat a hatóságokkal a törvényes minimumon felül.** Pont annyi együttműködés, amennyi kötelező, és az is transzparensen dokumentálva.

Az **aggregált statisztika** kivétel: ha a rendszer működéséről készül összesített kép (pl. "ebben a hónapban X típusú projekt-ből Y készült"), azt **nyilvánosan megosztjuk mindenkivel**. Ha valaki látja, akkor mindenki látja.

---

## 5. Hogyan finanszírozza magát a rendszer

A SpaceOS-nak saját fenntartási költsége van — szerverek, fejlesztés, support. Ezt **nem a felhasználói bizalom kihasználásával** fedezzük. A modell a Blender Foundation / Mozilla / Sovereign Tech logikáját követi, több forrással:

**Rövidtáv (2026-2027) — indulás:**
- **Közösségi támogatás:** donation, Open Collective, Patreon-szerű havi support. Opcionális, mindig. Funkciót soha nem old fel.
- **Alapító önerő:** amíg a közösség kicsi, az építkezést a saját munka és idő fedezi.

**Középtáv (2027-2028) — első bevétel:**
- **Önkéntes enterprise support-díj:** olyan KKV-k, akiknek a rendszer már kritikus üzleti eszközük, választhatnak, hogy támogassák a fejlesztést. Nem kötelező. Semmilyen funkciót nem old fel.
- **Grant / pályázati források:** NGI (Next Generation Internet), Sovereign Tech Fund, Prototype Fund (EU), hazai GINOP és ekvivalens források — nyílt szoftverre.
- **Foundation felállítása:** a SpaceOS hosszútávon **non-profit jogi keretet** kap. A fejlesztés nem egy cég tulajdona, hanem egy közösségé.

**Hosszútáv (2028+) — ökoszisztéma-bevétel:**
- **Tranzakciós díj B2B marketplace-en:** amikor egy asztalos egy beszállítóhoz eladáskor teljesít, kis százalékos díj — de ezt a **beszállító fizeti**, nem a felhasználó, és a tranzakció csak akkor jön létre, ha mindkét fél önként részt vesz.
- **Integrációs partnerségi díj:** gépgyártók (CNC), ERP-szállítók, szakmai szervezetek fizetnek azért, hogy a rendszerükben első osztályú integráció legyen.
- **Enterprise service-réteg:** SLA, dedikált support, on-premise deployment — a nagyvállalati ügyfelek fizetnek a professzionális service-ért, nem a szoftverért.

**A fizetős rétegek soha nem szolgálnak ki a saját felhasználóink ellen.** Ha valamelyik enterprise partner kéri, hogy a többi felhasználónak rosszabb legyen az élménye, a választ egy szóval adjuk meg: **nem**.

---

## 6. Hogyan terjed a rendszer

A terjedés organikus, alulról felfelé. **Nem** szerzünk ügyfelet enterprise sales-szel, cold outreach-csel, pénzért vásárolt hirdetéssel. A mechanizmus így néz ki:

```
Egy hobbista / diák / szabadúszó felfedezi a rendszert.
  ↓
Munkát készít benne, megosztja az eredményt (PDF, link, rajz).
  ↓
A megrendelője látja, a beszállítója látja — ők is belépnek.
  ↓
Egy KKV asztalos észreveszi: "a beszerzőm live katalógust ad itt,
a vevőm itt küldi a specifikációt."  → belép.
  ↓
A KKV asztalos a beszállítóival beszél: "itt rendelek ezentúl."
  → a beszállító is belép.
  ↓
A nagyvállalat kap egy ajánlatkérést SpaceOS-en keresztül.
  → kénytelen integrálni.
  ↓
Piaci sztenderd.
```

A kritikus mérőszám ebben az útban nem az, hogy hány user van, hanem hogy **egy projektben hány résztvevő van**. Ha ez a szám 1.5-ről 3.0-ra emelkedik, a rendszer elindult. Ha 5.0 fölé, önjáró.

Ez a folyamat 3-5 év. Türelmesek vagyunk.

---

## 7. A szabadság, amit ígérünk

A "szabadság" itt nem üres marketing-szó. Négy konkrét szabadságot jelent:

**A CAD szabadsága.** Használd, amit tudsz, amit szeretsz, amit megengedhetsz magadnak. AutoCAD teljes, AutoCAD LT (korlátozottabb módon), BricsCAD, GstarCAD, ZWCAD, CADian, Inventor (Woodwork Design add-inel), SolidWorks (SWOOD add-inel), Fusion 360, SketchUp, Blender — mindegyikhez integrációt építünk, első osztályú minőségben. Később, amikor a saját asztali és webes alkalmazásunk is elkészül, azt is választhatod. Soha nem kötelezünk semmire.

**Az adat szabadsága.** Bármikor exportálhatod az összes adatodat, szabványos formátumban (JSON, DXF, IFC, STEP, CSV). Nem részenkénti "export", hanem **teljes**, egy kattintással. Ha holnap elhagysz minket, azt megteheted, minden adatoddal.

**Az árak szabadsága.** Az alapfunkciók ingyenesek. Amiért fizetsz (ha valaha fizetsz), az világos, átlátható, bármikor lemondható — és a lemondás után is megmarad minden adatod és az alapfunkciók.

**Az interoperabilitás szabadsága.** Nyílt szabványokat használunk (IFC, STEP, DXF, JSON), és az API-hoz nyílt dokumentáció tartozik. Ha valaki a mi rendszerünk tetejére akar építeni valamit — harmadik partner, lelkes fejlesztő, versenytárs integrátor —, az mehet, szabadon.

---

## 8. A hosszútáv: saját "tér"

A SpaceOS név nem véletlen. A "space" — a **tér** — egy rendszer-szintű fogalom, ami túlmutat a faiparon. A jelenlegi fókuszunk a faipari terek: szekrények, ajtók, konyhák, bútorok. De a rendszer tervezése eleve úgy történik, hogy **új szakterületek könnyen rácsatlakozhassanak**:

- **Belsőépítészek** — a tervezők és az asztalosok közös nyelve.
- **Építészek** — IFC / BIM híd a faipari gyártásig.
- **Lakberendezők, kiállításrendezők, színpadmérnökök** — minden, ami "térbe kerül valami".
- **Gépészmérnökök** — ha a rendszer faipari szereplői gépészeti alkatrészt is megrendelnek.

Ez a fluiditás — a "tér" koncepciójának rugalmas értelmezése — a rendszer egyik erőssége. A kommunikáció a szakmák között **ma** az egyik legkeményebb fal a projektekben: a belsőépítész Revit-ben gondolkodik, az asztalos AutoCAD-ben, a megrendelő Excel-ben, a szerelő telefonon. A SpaceOS feloldja ezeket a falakat — mindenki a saját eszközén dolgozik, a rendszer tolmácsol közöttük.

### Brand-architektúra

A brand-struktúra ezt a többrétegűséget követi:

| Szint | Név | Szerep |
|---|---|---|
| **Kernel** | **SpaceOS** | A rendszer, a mag, a kapcsolat, a platform. A gépekhez szól. |
| **Szakági brand (HU)** | **asztalostech.hu** | A faipari interfész a magyar közönséghez. |
| **Szakági brand (int.)** | **JoineryTech** (joinerytech.hu) | A faipari interfész a nemzetközi közönséghez. |
| **Jövőbeli szakági brandek** | belsőépítészet, építészet, színpadtervezés, gépészet | Ahogy a közösség bővíti. |

Minden szakági brand a SpaceOS kernelre épül, közös adatmodellel, közös identitással, közös audit-láncolattal. **A szakági brand-ek a közönséghez szólnak, a kernel a gépekhez.**

### Saját asztali és webes alkalmazás — függetlenségi garancia

A **saját** asztali és webes alkalmazás **nem "versenyfunkció"**, hanem **stratégiai garancia**.

Ma: a CabinetBilder és a rokon plugin-család építi a közösséget a meglévő CAD-rendszerek mellett. Ez a **leggyorsabb belépési pont** — nem kényszerítünk senkit új szoftver tanulására, a saját eszközében dolgozik.

Holnap: ha bármelyik CAD-gyártó úgy dönt, hogy a mi integrációnkat ellehetetleníti (licenc-korlátozás, API-változtatás, partner-kizárás), a közösségnek akkor is meg kell maradnia egy működő eszközparkkal. A saját alkalmazás **a függetlenség biztosítéka** — az lesz az, amire a hálózat támaszkodhat, függetlenül attól, hogy milyen külső döntés születik.

Ez a fejlesztés hosszú — **2-4 év a teljes paritásig** —, de az első napról elkezdünk hozzá. A plug-inek **ma** építik a közösséget; a saját alkalmazás **holnap** biztosítja a közösség függetlenségét.

---

## 9. Versenytársakról

*"Nagyon örülnék, ha egy ilyen hatást ki tudnánk váltani. Ráférne a piacra egy felfordulás. Jobb lenne, ha újra látnák az embereket a pénztárcák mögött. Ha kérik, segítünk nekik. Remek termékeik vannak."*
*— Gábor, 2026 április*

Ez a pozíció. Ez marad.

A MaxCut, a Microvellum, a CabinetVision, az Imos, a TopSolid Wood, a Swood, a Woodwork Design — mindegyik egy-egy nagyobb probléma egy-egy részére született válasz. Okos emberek építették, sok évtizednyi tudást adtak neki. Tisztelet. Ha a SpaceOS megjelenése miatt **a saját termékük jobb lesz**, az számunkra siker — nem vereség. A piac elfér mindenkinek. Az asztalosokon segítve a versenyt is ösztönözzük, és ez természetes dolog.

Nem a versenytársak ellen építjük a rendszert. **A kizártságért, a költség-barrier-ért, a digitális privilégium-rendszer ellen** építjük. Ha ezt egy versenytárs is elkezdi csinálni: köszönjük, és közösen haladjunk.

---

## 10. AI mint DNS

Az AI nem egy feature a rendszerben. Nem tervezünk "AI Assistant"-ot chatbot-ikonnal. Nem reklámozzuk "AI-powered"-ként magunkat. Nem alakítunk ki okos pop-upokat azért, hogy a tech-demókon jól nézzen ki.

De **az AI a rendszer DNS-e**, mert a rendszert úgy építjük, hogy egy AI-ügynök ugyanolyan elsőrangú kliens legyen, mint egy ember. Minden funkció, minden API, minden dokumentáció úgy készül, hogy egy LLM is megértse, használja, magyarázza, automatizálja.

### Konkrétan ez mit jelent

- Minden REST endpoint **OpenAPI schema**-val érkezik, human-readable leírásokkal minden mezőn.
- Minden hibaüzenet **érthető** — nem csak egy hibakód, hanem egy lépéslista a javításhoz, olvasható magyarul és angolul.
- Minden workflow **MCP-kompatibilis** (Model Context Protocol) — egy agent közvetlenül tud dolgozni a rendszerrel, nem szükséges UI-scraping.
- A ProductTemplate-ek, a FrontMatter-sémák, az audit-esemény-struktúrák **leírhatók természetes nyelvvel** — nem csak gépi struktúrával.
- A CLI és a GUI működése **parallel** — amit ember meg tud csinálni a felületen, parancssorból és API-n keresztül is elérhető, ugyanazon a jogosultsági modellel.

### Miért ez a helyes út

Ez azt jelenti, hogy **egy felhasználó ki tudja nyitni a saját AI-eszközét (ChatGPT, Claude, Gemini, bármi) és azt mondhatja**:

> *"Generálj egy konyha BOM-ot a SpaceOS-omban 280×65×220 mérettel, tölgy furnér, Blum pántok, lapszabászatot a legközelebbi beszállítómhoz küldd."*

Az AI ezt elvégezheti **anélkül, hogy a user rákattintana egyetlen gombra is.**

Nem azért, mert ezt reklámozzuk. Azért, mert **a rendszer ilyen**.

Ez a jövő munkafolyamata, és ha a SpaceOS nem erre épül, öt év múlva elavult lesz. Ha erre épül, öt év múlva **az egyetlen olyan faipari platform, amit egy AI-agent érdemben használni tud** — és ez önmagában versenyelőny, amit senki nem fog tudni utánunk építeni hirtelen, mert az utólagos retrofit iszonyatos, a kezdettől-jól-építés triviális.

---

## 11. Hogyan használd ezt a dokumentumot

Ez az iránytű. Amikor egy döntést kell hozni — architektúra, feature, üzleti modell, brand, partner, prioritás —, a sorrend ez:

1. **Olvasd el a releváns tézist.** (2. szakasz)
2. **Nézd meg az anti-feature-listát.** (4. szakasz)
3. **Ha a döntés ellentmond a manifesztumnak, a döntés változik meg** — nem a manifesztum.
4. **Ha egy új helyzet nem szerepel a manifesztumban, és komoly következménye van**, a manifesztumot bővítjük — de csak megfontolt, dokumentált ADR-rel.

Ez a dokumentum **élő**. A jövőbeli verziók (v1.1, v2.0) rögzítik a fontos új döntéseket. De a 10 alaptézis (2. szakasz) csak akkor változhat, ha a vízió maga változik — és az nagyon ritka kell, hogy legyen.

---

## Záró szó

A faipar 1000 éves szakma. A szoftveripar 70 éves. A kettő közötti kapcsolat még mindig új — és még mindig alakítható.

**Ne a szoftvercégek alakítsák. Alakítsák azok, akik a faipart csinálják. És az az eszköz, amin ezt csinálják, legyen ingyenes, szabad, és az övék.**

Ez a SpaceOS.

---

*Manifesztum v1.0 — 2026-04-23 · A következő revízió akkor, amikor egy alaptézis igazolhatóan hiányzik vagy egy új, fontos döntés került a rendszerbe.*
