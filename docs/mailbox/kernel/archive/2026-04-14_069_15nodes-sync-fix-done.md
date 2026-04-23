---
id: MSG-KERNEL-069-DONE
from: kernel
to: root
type: done
priority: medium
status: READ
ref: MSG-KERNEL-069
created: 2026-04-14
---

# MSG-KERNEL-069-DONE — POST /nodes/register 500 fix — DevRsaKeyManager

## Gyökérok

**`DevRsaKeyManager.CreateOrLoad()` statikus field initializer crash** VPS deploy után:

```
keys/dev-private-key.pem → nincs a publish könyvtárban
  → File.Exists("keys/dev-private-key.pem") = false
  → Directory.CreateDirectory("keys") + File.WriteAllText(...)
  → ha a publish dir read-only → IOException
  → TypeInitializationException (permanent!)
  → minden RegisterNode hívás 500
```

A `.csproj`-ban nem volt `CopyToOutputDirectory` konfigurálva a key fájlhoz — `dotnet publish` nem másolta a `keys/` könyvtárat. A statikus field initializer silently failed = örökre 500 a `/nodes/register`-en.

## Javítás

### 1. `DevRsaKeyManager.CreateOrLoad()` — exception-safe

```csharp
private static RSA CreateOrLoad()
{
    try
    {
        if (File.Exists(keyPath))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(keyPath));
            return rsa;
        }
    }
    catch { /* corrupted key → regenerate */ }

    var newRsa = RSA.Create(2048);
    try
    {
        Directory.CreateDirectory("keys");
        File.WriteAllText(keyPath, newRsa.ExportPkcs8PrivateKeyPem());
    }
    catch { /* read-only dir → in-memory only */ }

    return newRsa;
}
```

### 2. `SpaceOS.Kernel.Api.csproj` — key file publish output

```xml
<None Include="keys/dev-private-key.pem" CopyToOutputDirectory="PreserveNewest" />
```

A key file mostantól mindig jelen lesz a publish könyvtárban. A `CreateOrLoad()` betölti → nem ír fájlt → read-only dir sem probléma.

## Változtatott fájlok

| Fájl | Változás |
|---|---|
| `SpaceOS.Infrastructure/Auth/DevRsaKeyManager.cs` | Exception-safe CreateOrLoad() |
| `SpaceOS.Kernel.Api/SpaceOS.Kernel.Api.csproj` | keys/dev-private-key.pem CopyToOutputDirectory |

Commit: `37951c8` → pushed `develop`

## Tesztek

1077 teszt zöld (883 unit + 101 integration + 93 API), 0 failed, 4 skip

## Security review

- `dev-private-key.pem` forrásban van → E2E/dev csak: OK (nincs éles kulcs)
- Production: Key Vault-backed INodeAuthService köt be → `DevRsaKeyManager` nincs érintve
- In-memory fallback: node JWT-k egy process lifetime-on belül érvényesek — E2E-hez elegendő
- `catch` blokkok: nem nyelnek el szenzitív adatot, nem logolnak titkot

## Várható E2E hatás deploy után

- `POST /bff/nodes/register` → 200 (első regisztráció) vagy 409 (ismételt) — mindkettő elfogadott a tesztben
- 15-nodes-sync teszt → PASS (ha `nodesAvailable = true`)

## Kockázatok / kérdések

- Az in-memory fallback esetén node JWT aláírás változik service-restart után → node auth invalidálódhat. Ez csak fejlesztői/E2E szcenárióban releváns; a `CopyToOutputDirectory` fix megakadályozza ezt normál deploy esetén.
- Meglévő VPS-en a `keys/` könyvtárban más fájl (`dev-private-key.pem`) lehet — ha az korrupt, a fallback új in-memory kulcsot generál. Elfogadható.
