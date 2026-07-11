# Planner-A értékelése Planner-B tervéről

## 1. Miben erősebb Planner-B?

- **Image CDN + WebP/AVIF:** Professzionális production-ready megoldás, az én lazy-load-om csak félmegoldás
- **BroadcastChannel multi-tab sync:** Erre nem is gondoltam, pedig valódi UX javítás
- **Command Pattern undo support:** Azonnal enterprise-grade megoldás, nálam csak basic PATCH
- **Haptic feedback:** Mobilon tényleg jobb UX, modern best practice

## 2. Miben gyengébb?

- **Túloptimalizált:** AVIF + blurhash + Suspense túl sok az első iterációhoz
- **Infrastruktúra dependencia:** CDN/S3 setup blokkoló, nálam zero external dependency
- **Backend estimate hiányos:** "0.5 nap" képoptimalizáláshoz nála is kell backend, de nem részletezte
- **Validáció complexity:** Assembly reorder validációt későbbre halasztja ("unsafe mode"), de ez production risk

## 3. Mit vennék át?

- **BroadcastChannel API** a filter sync-hez – azonnal implementálható
- **Picture element WebP fallback** – progressive enhancement helyett
- **Gesture-first DnD approach** – jobb mint az én basic drag handle-em

## 4. Egyetértés (konsenzus mag)

- **localStorage a filter perzisztenciához** – mindketten ezt választottuk
- **Dnd-kit library** – azonos technológiai döntés
- **Transaction-based assembly reorder backend** – mindketten PATCH endpoint-ot javaslunk

## 5. Nézeteltérés

**Prioritási sorrend:** Az ő fordított logikája ("smallest increment") tetszetős, DE:
- Képoptimalizálás CDN-nel **nem 0.5 nap**, hanem 2-3 (infrastructure + backend + migration)
- Az én sorrendem **valóban inkrementális**: lazy-load működik CDN nélkül is, később bővíthető WebP-re
- Assembly drag-drop **ne legyen utolsó** – üzleti érték szempontjából ez a TOP 1, nem a képoptimalizálás

**Végső ítélet:** Planner-B technológiailag progresszívebb, de **én tartom reálisabbnak** az időbecsléseket és a függőségkezelést.