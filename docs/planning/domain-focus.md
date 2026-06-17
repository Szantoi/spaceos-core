# Planning Domain Fókusz

A Haiku scanner ezt a fájlt olvassa hogy tudja milyen szempontokat tartson szem előtt.
Módosítható bármikor — a következő scan-futásnál életbe lép.

## Aktív domain fókusz

```
domain: manufacturing
```

## Szempont lista

- **Felhasználói érték**: Milyen funkció segíti legjobban az asztalosipar napi munkáját?
- **Backend kapcsolhatóság**: Van-e már meglévő endpoint ami még nincs bekötve a frontendbe?
- **Iparági minták**: Mi az ami más ERP/MES rendszerekben standard, de SpaceOS-ben hiányzik?
- **Mobil első**: A funkciónak működnie kell kis képernyőn a gyártócsarnoban is
- **Offline tűrés**: Ha az internet kimegy a gyárban, a kritikus funkciók degradáltan működnek

## Elérhető domain értékek

| Domain | Fókusz |
|---|---|
| `manufacturing` | Gyártás, műhely, gépek, munkalapok |
| `sales` | CRM, ajánlatok, ügyfelek, előrejelzés |
| `logistics` | Szállítás, raktár, készlet |
| `finance` | Számlák, kifizetések, kontrolling |
| `quality` | NCR, audit, minőségellenőrzés |
| `hr` | Jelenléti, műszak, alkalmazottak |
| `all` | Teljes rendszer áttekintés |
