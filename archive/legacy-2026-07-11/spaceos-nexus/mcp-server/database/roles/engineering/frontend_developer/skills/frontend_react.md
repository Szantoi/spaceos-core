---
id: skill-frontend_react
title: "Frontend / React & TypeScript Skill"
type: skill
scope: global
keywords: ["react", "typescript", "vite", "hooks", "component", "tanstack-query"]
created_by: "System"
last_updated: 2026-02-14
---

# ⚛️ Frontend / React & TypeScript Skill

**Summary:** Ez a skill biztosítja a React 18+ és TypeScript fejlesztéséhez szükséges szabványokat, komponens mintákat és best practice-eket a projektben.

## 📋 Mikor töltsd be?

- **UI Fejlesztés**: Új komponens, Page, Layout készítése.
- **Logika**: Custom Hook írása, State management.
- **API Integráció**: Adatlekérdezés (TanStack Query), Form kezelés.
- **Hibakeresés**: React renderelési hibák, TypeScript típusproblémák.

---

## 🏗️ Architektúra és Szabályok

A frontend kód a **Feature-based** vagy **Component-based** struktúrát követi.

1. **Components (`src/components`)**:
   - ✅ "Buta" (Presentational) komponensek.
   - ❌ Nem tartalmaznak üzleti logikát vagy közvetlen API hívást.
   - ✅ Újrafelhasználhatóak (Button, Card, Input).
2. **Features (`src/features`)**:
   - ✅ Üzleti funkciók (pl. `Auth`, `Projects`).
   - ✅ Tartalmazzák a saját komponenseiket, hook-jaikat és API hívásaikat.
3. **Hooks (`src/hooks`)**:
   - ✅ Megosztott logika (pl. `useDebounce`, `useAuth`).
4. **Api (`src/api` vagy `src/services`)**:
   - ✅ Axios/Fetch definíciók és típusok.

### 📏 Kódolási Konvenciók

- **Functional Components**: Csak function komponenseket használunk (nem class).
- **TypeScript**: Minden prop és state legyen szigorúan típusos (nincs `any`).
- **Custom Hooks**: A komplex logikát (pl. adatlekérés, form state) emeld ki hook-ba.
- **CSS**: CSS Modules vagy Utility classes (pl. Tailwind) használata (projekt függő). Kerüld az inline style-t.

---

## 💻 Kód Minták (N-shot Patterns)

### 1. Komponens Minta (TypeScript)

```tsx
import { ReactNode } from 'react';
import styles from './Button.module.css'; // Vagy Tailwind osztályok

// 1. Props Interface definiálása
interface ButtonProps {
  label: string;
  onClick: () => void;
  variant?: 'primary' | 'secondary';
  disabled?: boolean;
  icon?: ReactNode;
}

// 2. Komponens implementáció
export const Button = ({
  label,
  onClick,
  variant = 'primary',
  disabled = false,
  icon
}: ButtonProps) => {

  return (
    <button
      className={`${styles.btn} ${styles[variant]}`}
      onClick={onClick}
      disabled={disabled}
      type="button"
    >
      {icon && <span className={styles.icon}>{icon}</span>}
      <span>{label}</span>
    </button>
  );
};
```

### 2. TanStack Query Minta (Adatlekérés)

```tsx
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getProjects, createProject } from '../api/projectApi';

export const useProjectsQuery = () => {
  return useQuery({
    queryKey: ['projects'],
    queryFn: getProjects,
  });
};

export const useCreateProjectMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createProject,
    onSuccess: () => {
      // Lista frissítése
      queryClient.invalidateQueries({ queryKey: ['projects'] });
    },
  });
};
```

---

## ⚠️ Gyakori Hibák és Megoldások

| Hiba | Ok | Megoldás |
| :--- | :--- | :--- |
| `Props validation failed` | Hiányzó vagy rossz típusú prop. | Ellenőrizd az Interface definíciót. |
| `Too many re-renders` | State frissítés a render ciklusban. | Használj `useEffect`-et vagy eseménykezelőt. |
| `Object is possibly 'undefined'` | TypeScript strict null check. | Használj Optional Chaining-et (`data?.title`) vagy Guard-ot. |
| `Hook called conditionally` | Hook hívás `if` blokkban. | Hook-okat mindig a komponens tetején hívj. |
