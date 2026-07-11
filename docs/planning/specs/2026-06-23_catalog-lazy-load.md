---
id: SPEC-2026-06-23-LAZY
title: "Katalógus Termékkép Lazy-load Optimalizálása & Fallback Badge"
type: spec
priority: high
status: IMPLEMENTÁCIÓRA KÉSZ
source_idea: IDEA-20260623-003
assignee: frontend
model: sonnet
created: 2026-06-23
---

# Katalógus Termékkép Lazy-load Optimalizálása & Fallback Badge

## 1. Cél és Scope

**Probléma:** A `catalog-world-view.jsx` komponens termékkártyáin a képek egyszerre töltődnek be oldalbetöltéskor, ami:
- Magas mobiladatfogyasztást okoz
- Lassú initial renderelést eredményez
- Nincs kezelve a hiányzó kép esete

**Megoldás:**
1. HTML5 `loading="lazy"` attribútum alkalmazása a képekre
2. Tailwind-alapú skeleton loader a betöltés alatt
3. "Nincs kép" fallback badge hiányzó képekhez

**Scope:**
- `catalog-world-view.jsx` — `WCItemCard` komponens módosítása
- Csak UI/CSS változtatás, nincs state management módosítás
- Nincs új dependency

## 2. Architektúra

### Érintett komponensek

```
/docs/joinerytech/catalog-world-view.jsx
  └── WCItemCard() komponens (243-303. sor)
        └── Kép megjelenítő div (255-258. sor)
```

### Jelenlegi állapot (WCItemCard)

```jsx
<div className="aspect-[4/3] rounded-xl border border-stone-100 flex items-center justify-center overflow-hidden"
  style={{ background: color }}>
  {isHw && <Icon name="wrench" size={24} className="text-white/70 drop-shadow" />}
</div>
```

A jelenlegi implementáció csak háttérszínnel jeleníti meg a katalógus tételeket. Nincs valódi kép betöltés, de a struktúra készen áll képek befogadására.

### Javasolt implementáció

```jsx
function WCItemCard({ item, worldId, onItemClick }) {
  const [imgState, setImgState] = useWC("loading"); // loading | loaded | error
  const hasImage = item.image && item.image.trim() !== "";

  // ... existing code ...

  return (
    <div ...>
      {/* Kép/Placeholder container */}
      <div className="aspect-[4/3] rounded-xl border border-stone-100 flex items-center justify-center overflow-hidden relative"
        style={{ background: color }}>

        {/* Skeleton loader - látható amíg loading */}
        {hasImage && imgState === "loading" && (
          <div className="absolute inset-0 bg-gradient-to-r from-stone-200 via-stone-100 to-stone-200 animate-pulse" />
        )}

        {/* Valódi kép - lazy loaded */}
        {hasImage && imgState !== "error" && (
          <img
            src={item.image}
            alt={item.name}
            loading="lazy"
            onLoad={() => setImgState("loaded")}
            onError={() => setImgState("error")}
            className={`w-full h-full object-cover transition-opacity duration-300 ${imgState === "loaded" ? "opacity-100" : "opacity-0"}`}
          />
        )}

        {/* Fallback: nincs kép vagy betöltési hiba */}
        {(!hasImage || imgState === "error") && (
          <div className="absolute inset-0 flex flex-col items-center justify-center bg-stone-50/80">
            <Icon name="image" size={20} className="text-stone-300 mb-1" />
            <span className="text-[9px] font-medium text-stone-400 uppercase tracking-wide">Nincs kép</span>
          </div>
        )}

        {/* Hardware icon (ha szükséges) */}
        {isHw && !hasImage && (
          <Icon name="wrench" size={24} className="text-white/70 drop-shadow z-10" />
        )}
      </div>

      {/* ... rest of the card ... */}
    </div>
  );
}
```

## 3. State Management

**Nincs globális state változás.** A lazy-load állapot lokális komponens state:

| State | Leírás |
|-------|--------|
| `loading` | Kép betöltés alatt |
| `loaded` | Kép sikeresen betöltődött |
| `error` | Betöltési hiba / nincs kép |

**Implementáció:** React `useState` hook (már importálva mint `useWC`).

## 4. UI/UX Terv

### Tailwind osztályok

**Skeleton loader:**
```css
/* Animált gradient shimmer */
bg-gradient-to-r from-stone-200 via-stone-100 to-stone-200 animate-pulse
```

**"Nincs kép" badge:**
```css
/* Container */
bg-stone-50/80 flex flex-col items-center justify-center

/* Ikon */
text-stone-300 mb-1

/* Szöveg */
text-[9px] font-medium text-stone-400 uppercase tracking-wide
```

**Kép fade-in:**
```css
transition-opacity duration-300
/* loading: */ opacity-0
/* loaded: */ opacity-100
```

### Interakciók

1. **Oldal betöltés:** Skeleton animáció jelenik meg
2. **Viewport-ba görgetés:** `loading="lazy"` triggereli a betöltést
3. **Sikeres betöltés:** Fade-in effektus (300ms)
4. **Hiba / nincs kép:** "Nincs kép" badge jelenik meg

## 5. Definition of Done (DoD)

- [ ] `WCItemCard` komponens módosítva lazy-load támogatással
- [ ] Skeleton loader megjelenik betöltés alatt
- [ ] "Nincs kép" fallback badge működik
- [ ] `loading="lazy"` attribútum alkalmazva minden képre
- [ ] Mobil tesztelés: adatfogyasztás csökken (DevTools Network tab)
- [ ] Nincs console error
- [ ] Vizuális regresszió nincs (meglévő tételek nem változnak)

## 6. Becsült idő

**1 óra**

Részletezés:
- Komponens módosítás: 30 perc
- Tailwind stílusok finomhangolás: 15 perc
- Tesztelés (mobil/desktop): 15 perc

## 7. Terminál hozzárendelés

**frontend** — React/JSX komponens módosítás

## 8. Kockázatok és megjegyzések

1. **Kép URL forrás:** A jelenlegi `WCItemCard` nem használ képet (csak háttérszín). Ha a katalógus tételekhez nincs `item.image` mező, a "Nincs kép" badge fog megjelenni minden tételnél. Ez nem hiba, hanem elvárt viselkedés.

2. **Browser támogatás:** `loading="lazy"` 95%+ browser support (2024 adatok). Fallback: eager loading régi böngészőkben.

3. **Meglévő háttérszín:** A `color` háttérszín megmarad a kép mögött, így a töltés alatt is látható a tétel színe.

---

*Generálva: 2026-06-23 | Forrás idea: IDEA-20260623-003 | Architect terminál*
