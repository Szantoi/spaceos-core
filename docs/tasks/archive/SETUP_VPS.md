# SETUP — VPS (Linux)
## SpaceOS Multi-Repo Workspace — Authoritative Side

> **Cél:** A VPS lesz a **single source of truth** minden non-Windows kódra és minden globális dokumentumra. Itt fut a fő Claude Code agent (#1).
>
> **Becsült idő:** 30–45 perc
> **Előfeltétel:** Meglévő `spaceos-docs` repo a GitHub-on, működő SSH kulcs, .NET 8/10 SDK telepítve

---

## 0. Mit fogsz létrehozni / megváltoztatni

| Művelet | Hatás |
|---|---|
| Új repo: `spaceos-modules-cabinet` (GitHub + lokál klón) | Modules.Cabinet domain kód itt él |
| Új mappa-struktúra: `~/dev/spaceos/` | Egységesített workspace gyökér |
| `cabinetbilder-autocad` repo klónozása read-only-ra | VPS agent olvashassa, ha kell |
| GitHub Packages NuGet feed beállítása | A Phase A-ban kibocsátott csomagok ide kerülnek |
| GitHub Actions workflow a `spaceos-modules-cabinet`-en | `dotnet pack` + auto-publish push-ra |
| `CLAUDE.md` szabályok az új repo-ban | Agent #1 munkamegosztásához |

**Mit NEM csinálsz** ebben a setup-ban:
- Még nem kezded a Phase A architektúra implementációt — az az arch-planner output után jön
- Nem mozgatod a meglévő spaceos-kernel, spaceos-portal repo-kat — maradnak ahol vannak
- Nem nyúlsz a meglévő `spaceos-docs` tartalmához

---

## 1. Workspace-mappa létrehozása

```bash
# Ha még nincs ilyen struktúrád, hozd létre. Ha van, igazítsd ehhez.
mkdir -p ~/dev/spaceos
cd ~/dev/spaceos
```

A meglévő repo-id (kernel, portal, docs) **ide kerüljenek alá**, ha másutt vannak. Ha már itt vannak, lépj tovább.

```bash
ls -la
# Várt eredmény (vagy hasonló):
#   spaceos-docs/    (meglévő)
#   spaceos-kernel/  (meglévő)
#   spaceos-portal/  (meglévő)
```

---

## 2. Új repo létrehozása: `spaceos-modules-cabinet`

### 2.1. GitHub-on

Hozd létre a repo-t a GitHub-on:
- Név: **`spaceos-modules-cabinet`**
- Visibility: **Private** (vagy ahogy a többit kezeled)
- Ne inicializáld README-vel (mi tölti)

### 2.2. Lokális klónozás és skeleton

```bash
cd ~/dev/spaceos
git clone git@github.com:<your-username>/spaceos-modules-cabinet.git
cd spaceos-modules-cabinet

# Skeleton solution
dotnet new sln -n SpaceOS.Cabinet

# Mappastruktúra
mkdir -p src tests docs/sessions docs/architecture docs/adr docs/codebase-status
mkdir -p .github/workflows .claude
```

### 2.3. README.md

```bash
cat > README.md << 'EOF'
# SpaceOS Modules.Cabinet

Platform-független domain motor a SpaceOS asztalosipari vertikum (Cabinet) számára.

## Részei

- `SpaceOS.Cabinet.Domain` — Skeleton, BaseCuboid, Part, Connection
- `SpaceOS.Cabinet.Geometry` — AffineTransform, frames, vectors
- `SpaceOS.Cabinet.Machining` — MachiningFeature, Subjects, Hardware-ref
- `SpaceOS.Cabinet.Construction` — Rule engine, default rule-ok
- `SpaceOS.Cabinet.Semantics` — Inference service, PartRole
- `SpaceOS.Cabinet.Catalog` — Federated catalog (Phase B)
- `SpaceOS.Cabinet.Assembly` — Assembly Documentation, FlowEpic-derivation (Phase B)

## Kapcsolódó repo-k

- `spaceos-kernel` — FlowEpic, AggregateSnapshot, Tenant infra
- `spaceos-docs` — globális SpaceOS dokumentáció (Manifesto, MFT, ADR)
- `cabinetbilder-autocad` — AutoCAD adapter (Windows-on fejlesztve)

## Architektúra

A jelenlegi tervdokumentum: `docs/architecture/PhaseA_Architecture_v4.md`
(Phase A arch-planner kimenete, generálandó.)

A 16 axióma teljes leírása: `docs/sessions/SpaceOS_Cabinet_Core_Session_20260425.docx`
EOF
```

### 2.4. CLAUDE.md (Claude Code agent #1 context)

```bash
cat > CLAUDE.md << 'EOF'
# CLAUDE.md — SpaceOS Modules.Cabinet (VPS)

This file guides Claude Code agent #1 (running on VPS) when working in this repo.

## Repo identity

- **Name:** spaceos-modules-cabinet
- **Role:** Platform-independent domain core for the Cabinet vertical
- **Authoritative side:** VPS (this machine). Never edit this repo from Windows.

## Sibling repos in `~/dev/spaceos/`

- `spaceos-docs/` — Global docs. Read for context. Edit OK (authoritative here on VPS).
- `spaceos-kernel/` — Kernel domain. Read for FlowEpic/Snapshot integration. Edit OK.
- `spaceos-portal/` — Portal frontend. Mostly read-only context for this repo.
- `cabinetbilder-autocad/` — AutoCAD adapter, READ-ONLY MIRROR here on VPS.
  Do NOT edit. Authoritative side is the Windows desktop.

## Architecture (Phase A scope)

This repo implements axioms A1–A11 in Phase A:
- Affine matrices for all spatial data (A1)
- Two reference frames: PartFrame, AssemblyFrame (A2)
- BaseCuboid as cabinet root (A3, A4)
- Default joint = face-edge butt (A5)
- Machining on Plane | Edge | Connection (A6)
- Semantic inference (gravity + topology) (A7)
- Platform-independent core (A8)
- TenantStandard scope (A9)
- Selective recomputation (A10)
- Warning, never block (A11)

Phase B (later) adds A12–A16: catalog, assembly documentation, FlowEpic scope-extension.

Full design context: `docs/sessions/SpaceOS_Cabinet_Core_Session_20260425.docx`

## Architectural rules (non-negotiable)

1. No public setters on aggregates
2. All business logic in domain — never in handlers
3. Every mutation raises a domain event
4. `PopDomainEvents()` + `DispatchAsync()` at end of every mutating handler
5. Lists go through Ardalis.Specification, no raw repo calls
6. `Result<T>` return type on every handler
7. `ConfigureAwait(false)` on every production async call
8. `AsNoTracking()` on every read-only repo method

## Approved packages (NuGet)

MediatR, FluentValidation, Ardalis.Result, Ardalis.Specification, EF Core 8, xUnit v3, Moq.
Any addition must be discussed before introduced.

## Output format conventions

- File path as first line in every code block
- Diff preferred over full file
- Companion test file for every new handler
- No TODO/FIXME in committed code

## Build & test

```bash
dotnet build
dotnet test
dotnet pack -c Release  # Phase A vége felé
```
EOF
```

### 2.5. .gitignore

```bash
cat > .gitignore << 'EOF'
# .NET
bin/
obj/
*.user

# IDE
.vs/
.vscode/
.idea/

# OS
.DS_Store
Thumbs.db

# NuGet
*.nupkg
*.snupkg
.nuget/

# Test results
TestResults/
coverage.xml
EOF
```

### 2.6. Initial commit

```bash
git add .
git commit -m "Initial scaffold: skeleton solution, docs structure, CLAUDE.md, README"
git push -u origin main
```

---

## 3. Mai session-record bemásolása

A `SpaceOS_Cabinet_Core_Session_20260425.docx` fájl a mai beszélgetésből — ez a Phase A architektúra alapja. Itt a helye.

```bash
# Töltsd le a Claude.ai-ról a fájlt, vagy SCP-vel mozgasd ide:
# scp <local>:SpaceOS_Cabinet_Core_Session_20260425.docx vps:~/dev/spaceos/spaceos-modules-cabinet/docs/sessions/

cd ~/dev/spaceos/spaceos-modules-cabinet
ls docs/sessions/
# Várt: SpaceOS_Cabinet_Core_Session_20260425.docx

git add docs/sessions/SpaceOS_Cabinet_Core_Session_20260425.docx
git commit -m "Pre-arch-planner session record (16 axioms, 18 ODs, Phase A/B plan)"
git push
```

---

## 4. GitHub Actions workflow — NuGet auto-publish

Ez a workflow minden push-ra builddel és — ha tag-elsz — közzétesz NuGet csomagokat a GitHub Packages feed-jére.

```bash
cat > .github/workflows/build-and-publish.yml << 'EOF'
name: Build and Publish

on:
  push:
    branches: [main]
    tags: ['v*.*.*']
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Test
        run: dotnet test --configuration Release --no-build --verbosity normal

      - name: Pack (only on tag)
        if: startsWith(github.ref, 'refs/tags/v')
        run: dotnet pack --configuration Release --no-build --output ./nupkgs

      - name: Publish to GitHub Packages (only on tag)
        if: startsWith(github.ref, 'refs/tags/v')
        run: |
          dotnet nuget add source --username ${{ github.actor }} \
            --password ${{ secrets.GITHUB_TOKEN }} \
            --store-password-in-clear-text \
            --name github \
            "https://nuget.pkg.github.com/${{ github.repository_owner }}/index.json"
          dotnet nuget push "./nupkgs/*.nupkg" \
            --source github \
            --api-key ${{ secrets.GITHUB_TOKEN }}
EOF

git add .github/workflows/build-and-publish.yml
git commit -m "CI: build + test on push, pack + publish on tag"
git push
```

**Megjegyzés:** A workflow csak `vX.Y.Z` tag-re publikál (pl. `v0.1.0-alpha.1`). Ez biztosítja, hogy ne minden commit gyártson NuGet verziót. A Phase A során te döntöd el, mikor érdemes új csomagverziót kibocsátani.

**Verziózási konvenció (javasolt):**
- `v0.1.0-alpha.N` — Phase A iterációk (instabil API)
- `v0.1.0` — Phase A vége, AutoCAD adapter sikeresen integrálódott
- `v0.2.0-alpha.N` — Phase B iterációk
- `v1.0.0` — Doorstar Soft Launch

---

## 5. CabinetBilder read-only mirror (opcionális)

Ha az Agent #1 (VPS) néha látni akarja, hogy mi van a Windows-on írt `cabinetbilder-autocad` repo-ban (debug, integráció ellenőrzés, kódolvasás), klónozd ide is.

```bash
cd ~/dev/spaceos
git clone git@github.com:<your-username>/cabinetbilder-autocad.git

# Read-only védelem
cd cabinetbilder-autocad/.git/hooks
cat > pre-commit << 'EOF'
#!/bin/sh
echo "ERROR: cabinetbilder-autocad is authoritatively written on Windows."
echo "This is a READ-ONLY mirror on VPS. Aborting commit."
exit 1
EOF
chmod +x pre-commit
```

Ettől kezdve a VPS-en `git commit` lehetetlen ebbe a repo-ba — csak `git pull`. A Windows agent a forráshely.

---

## 6. spaceos-docs ellenőrzése (csak meggyőződés)

A meglévő `spaceos-docs` repo-d **a VPS-en authoritative** marad. Itt írod a globális dokumentumokat (Manifesto, MFT, ADR-ek). Csak ellenőrizd, hogy minden rendben:

```bash
cd ~/dev/spaceos/spaceos-docs
git status
# Várt: clean working tree, up to date with origin/main

git log --oneline -5
# Várt: a legutóbbi 5 commit
```

Ha bármi rendetlenség van, rendezd el most. A Windows oldal a következő setup során fog innen pull-olni.

---

## 7. NuGet feed — előkészítés a Windows számára

A GitHub Packages feed URL-je a következő lesz:
```
https://nuget.pkg.github.com/<your-username>/index.json
```

A Windows-os Setup dokumentum részletezi, hogyan állítja be a `nuget.config`-ot. Itt csak rögzítsd magadnak, hogy:

- **Username:** GitHub username
- **Password:** GitHub Personal Access Token (PAT) **read:packages** jogosultsággal
  - Ha még nincs PAT-od, hozd létre most: https://github.com/settings/tokens
  - **Token mentse el biztonságosan** — Windows oldal kérni fogja

---

## 8. Záró ellenőrzés

```bash
cd ~/dev/spaceos
ls -la
# Várt minimum:
#   spaceos-docs/                  (writable)
#   spaceos-kernel/                (writable)
#   spaceos-modules-cabinet/       (writable, ÚJ)
#   spaceos-portal/                (writable)
#   cabinetbilder-autocad/         (read-only mirror, opcionális)

cd spaceos-modules-cabinet
ls -la
# Várt:
#   .claude/
#   .git/
#   .github/workflows/build-and-publish.yml
#   .gitignore
#   CLAUDE.md
#   README.md
#   SpaceOS.Cabinet.sln
#   docs/
#   src/
#   tests/

ls docs/sessions/
# Várt: SpaceOS_Cabinet_Core_Session_20260425.docx
```

**Ha minden megvan, a VPS-oldali setup KÉSZ.**

---

## 9. Mit jelent "kész"

| Komponens | Állapot |
|---|---|
| `spaceos-modules-cabinet` repo a GitHub-on | ✓ létrehozva |
| Lokális klón a VPS-en | ✓ kész |
| Skeleton solution | ✓ üres, készen áll a Phase A-ra |
| `CLAUDE.md` agent context | ✓ kitöltve |
| Mai session-record `docs/sessions/`-ben | ✓ commit-olva |
| GitHub Actions CI/CD | ✓ aktív, tag-publikálásra felkészítve |
| NuGet PAT generálva | ✓ Windows oldali setup-hoz |
| `cabinetbilder-autocad` mirror VPS-en | ✓ read-only (opcionális) |

**Következő lépés:** elvégezned kell a Windows oldali setup-ot is. Utána indul a Phase A arch-planner.

---

## Hibakeresés

| Probléma | Megoldás |
|---|---|
| `dotnet new sln` "command not found" | Telepítsd a .NET SDK-t: `sudo apt install dotnet-sdk-8.0` |
| GitHub Actions workflow hibázik első push-ra | Az első push még nem tag — csak build + test fut. Ez normális. |
| `git push` permission denied | SSH kulcs nincs hozzárendelve a GitHub-hoz. Lásd: https://docs.github.com/en/authentication |
| A CabinetBilder mirror pre-commit hook nem lefut | Ellenőrizd, hogy `chmod +x pre-commit` lefutott |
