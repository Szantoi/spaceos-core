# Keresztértékelés: Sonnet-B véleménye Sonnet-A tervéről

## Egyetértés (Konsenzus mag)

1. **Nesting viz backend KÉSZ audit** — mindketten látjuk, hogy a `catalogType` enrichment kritikus (N+1 query elkerülés)
2. **@dnd-kit/core választás** — egyetértünk a lib-ben (maintained, accessibility)
3. **11 napos timeframe realitás** — Sonnet-A 11d vs. én 10.5d → azonos nagyságrend

## Ahol Sonnet-A erősebb

**Risk mapping precizitás** — A "Kockázat:" blokkok minden szekciónál konkrét mitigációt tartalmaznak (pl. batch fetch optimalizálás). Az én tervem ezt csak összegzett táblázatban adja.

**Frontend state snippet-ek** — A `useLocation<{ highlightPlanId }>` + scroll-to-row kód azonnal implementálható. Én csak koncepcionális flow-t írtam.

## Ahol gyengébb (konstruktív kritika)

**Sorrend indoklása felületes** — "gyors win" érv valid, DE: ha a machining backend nincs kész, a nesting viewer **izolált toy** marad. Én alulról építek (scheduling → nesting → workflow), mert a físikai műveleti réteg nélkül csak mock-cserét csinálsz.

**Capacity constraint hiánya** — Sonnet-A terv NEM tartalmaz `GET /machines/capacity` endpoint-ot. Így a drag-drop UI "működik", de **vak** — nem tudod, hogy a CNC-01-re 8 órás túlfoglalást csinálsz-e. Ez production környezetben critical bug forrás.

**Auto-assign nincs** — A design→cutting link manuális navigation marad. Az én tervem 1 nappal több BE-t kér, de **eliminálja** az operátor 3 kattintását.

## Mit vennék át?

A **ProductionPage highlight row animáció** kódot szó szerint — ez UX polish amit én csak "navigate with state" szinten írtam le.

## Végső álláspont

Sonnet-A terv **gyorsabb MVP**-t ad (nesting viz azonnal látható), de **nem scaling-ready**. Én tartom a TOP 3→1→2 sorrendet, mert a capacity constraint API **most** 2 nap, DACH belépéskor 2 **hét** lenne utólag beépíteni.

**Kompromisszum:** Fogadjuk el Sonnet-A frontend snippet-jeit, de scheduling backend legyen **első** (capacity API-val együtt).
