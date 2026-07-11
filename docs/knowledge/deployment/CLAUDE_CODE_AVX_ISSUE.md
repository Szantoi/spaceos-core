# Claude Code AVX CPU Instruction Issue

> **DISCOVERED:** 2026-07-08
> **IMPACT:** Claude Code 2.1.15+ nem fut QEMU VM-ekben AVX CPU instruction nélkül

---

## 🔴 PROBLÉMA

### Mi történt?

Claude Code **2.1.15+ verzióban** az Antropic átállt **Node.js-ről Bun runtime-ra**.
A **Bun runtime AVX CPU instructiont igényel**.

A SpaceOS VPS QEMU Virtual CPU 2.5+ **NEM támogatja az AVX-et**, ezért a Claude Code 2.1.15+ verziók:
- CLI parancsok végtelen várakozásban (hang)
- SIGILL (Illegal Instruction) crash
- Minden `claude` parancs nem reagál

### CPU Flags Hiány

**VAN a VPS-en:**
```
sse, sse2, mmx, pni (SSE3 subset)
```

**NINCS a VPS-en (Bun requirement):**
```
AVX, AVX2, SSE4.1, SSE4.2
```

**VPS CPU Info:**
```
Architecture: x86_64
CPU(s): 6
Model name: QEMU Virtual CPU version 2.5+
```

---

## ✅ MEGOLDÁSOK

### **Megoldás 1: NPM Installation (AJÁNLOTT — STABIL)**

**Használjuk az NPM-en keresztül telepített Claude Code-ot** (Node.js-based, nem Bun).

```bash
npm install -g @anthropic-ai/claude-code@2.0.62
```

**Előnyök:**
- ✅ Működik AVX nélkül
- ✅ Stabil, bevált verzió
- ✅ Azonnali megoldás

**Hátrányok:**
- ❌ Elmaradunk a legújabb verziótól
- ❌ Új Bun-based funkciók nem elérhetőek

**Státusz:** ✅ **JELENLEG EZT HASZNÁLJUK**

---

### **Megoldás 2: VM CPU Type → "host" (AVX Passthrough)**

**VPS provider-nél (Proxmox/QEMU) átállítjuk a CPU type-ot "host" módra.**

#### Proxmox példa:

```bash
# VM config (host oldalon)
qm set <VM_ID> --cpu host
```

#### Követelmények:

1. **Fizikai host CPU-nak támogatnia kell AVX-et**
   ```bash
   # Check on host (ha van SSH hozzáférés)
   grep -o 'avx[^ ]*' /proc/cpuinfo | sort -u
   ```

2. **VM teljes shutdown + start** (reboot nem elég!)
   ```bash
   # Full power cycle needed
   qm shutdown <VM_ID>
   qm start <VM_ID>
   ```

3. **Ellenőrzés a VM-ben:**
   ```bash
   cat /proc/cpuinfo | grep flags | grep avx
   ```

**Előnyök:**
- ✅ Futni fog a legújabb Claude Code (2.1.204+)
- ✅ Minden Bun-based funkció elérhető

**Hátrányok:**
- ❌ Fizikai host CPU-nak támogatnia kell AVX-et
- ❌ VM migráció más host-ra korlátozottabb lesz
- ❌ VPS provider engedélyeznie kell a CPU passthrough-t
- ❌ Újraindítás szükséges (downtime)

**Státusz:** ⏸️ **NEM TESZTELTÜK MÉG** (VPS provider-től függ)

---

### **Megoldás 3: Várunk Node.js Fallback-re**

Lehet hogy az Antropic visszahoz egy Node.js fallback-et vagy hybrid install-t a jövőben.

**Státusz:** ⏸️ **VÁRAKOZÁS** (nincs roadmap info)

---

## 📚 FORRÁSOK

**Official GitHub Issues:**
- [Issue #20019: Claude Code crashes with SIGILL on QEMU VM without AVX](https://github.com/anthropics/claude-code/issues/20019)
- [Issue #19981: Illegal instruction on virtualized environments lacking AVX](https://github.com/anthropics/claude-code/issues/19981)
- [Issue #19904: Bun crashes with "CPU lacks AVX support"](https://github.com/anthropics/claude-code/issues/19904)
- [Issue #19907: v2.1.15 crashes on CPUs without AVX (regression from 2.1.14)](https://github.com/anthropics/claude-code/issues/19907)

**Community Workarounds:**
- [Stackademic: Claude CLI "CPU lacks AVX support" Quick Fix](https://blog.stackademic.com/claude-cli-suddenly-crashing-cpu-lacks-avx-support-error-on-remote-vps-heres-the-quick-fix-18d05c4fae6d)
- [Christopher Hart: Claude Code Bun Crash on Proxmox VM](https://chrisjhart.com/Claude-Code-Bun-Crash-AVX-Proxmox/)

**QEMU/KVM Documentation:**
- [QEMU CPU model configuration](https://www.qemu.org/docs/master/system/i386/cpu.html)
- [Proxmox cpu-models.conf manual](https://pve.proxmox.com/wiki/Manual:_cpu-models.conf)
- [RamNode CPU Passthrough Guide](https://ramnode.com/support/documentation/legacy-kvm/cpu-passthrough)

---

## 🔧 DIAGNOSTIC COMMANDS

### Check Current CPU Flags
```bash
cat /proc/cpuinfo | grep flags | head -1
```

### Check AVX Support
```bash
grep -o 'avx[^ ]*' /proc/cpuinfo | sort -u
```

### Check Claude Code Version
```bash
claude --version
```

### Check Installation Method
```bash
which claude
ls -lh $(which claude)
```

### Rollback to Working Version
```bash
npm install -g @anthropic-ai/claude-code@2.0.62 --force
```

---

## 📝 NOTES

- **Discovered:** 2026-07-08 session — Root terminal
- **Impact:** ALL terminals using Claude Code 2.1.15+
- **Workaround implemented:** npm install 2.0.62 (stable)
- **Future consideration:** VPS provider CPU passthrough request

---

## 🎯 DÖNTÉS

**Jelenlegi stratégia:** Maradunk **Claude Code 2.0.62** (npm installation) verzióval.

**Indoklás:**
- Stabil, működik minden terminálban
- AVX nélkül is fut
- Későbbi VPS upgrade-nél újra értékeljük a 2.1.x+ lehetőségét

**Review date:** Q4 2026 (VPS CPU upgrade vagy provider váltás esetén)
