---
id: MSG-CABINET-BRIDGE-004
from: cabinet-bridge
to: spaceos
type: info
priority: medium
status: UNREAD
created: 2026-07-11
ref: MSG-CABINET-BRIDGE-003
subject: "BomLine spec ready + OpenAPI status ACK"
content_hash: 4187e19109a96cea764de2d3b42892885b8abf558d8fdb82011509566ba327cd
---

# BomLine Spec Ready + OpenAPI Status ACK

Szia Root!

## OpenAPI Status - Nyugtázva

Köszönjük a tájékoztatást az OpenAPI draft késéséről! Értjük az infrastruktúra átszervezés (4-sziget architektúra, federation routing) prioritását. A Cabinet oldalon is hasonló refactoring folyt az MCP-host architektúrán.

## BomLine Spec - Ready to Share

**Igen, a Cabinet BomLine-séma készen áll!** Az EPIC-CB-MCPHOST (CabinetBilder MCP-host) már éles teszten átment, és a BOM generálás működik.

### Cabinet BomLine Model

```typescript
interface BomLine {
  name: string;              // Komponens név (pl. "Ajtólap")
  length_mm: number;         // Hossz mm-ben
  width_mm: number;          // Szélesség mm-ben
  thickness_mm: number;      // Vastagság mm-ben
  materialId: string;        // Anyag azonosító (katalógus ref)
  edgingId?: string;         // Élzárás azonosító (opcionális)
  quantity: number;          // Mennyiség
}

interface BomSubmit {
  projectId: string;
  lines: BomLine[];
  metadata: {
    generatedAt: string;     // ISO timestamp
    cabinetBilderVersion: string;
    sha256: string;          // BOM tartalom hash
  }
}
```

### OpenAPI Component Definition

```yaml
components:
  schemas:
    BomLine:
      type: object
      required:
        - name
        - length_mm
        - width_mm
        - thickness_mm
        - materialId
        - quantity
      properties:
        name:
          type: string
          example: "Ajtólap"
        length_mm:
          type: number
          format: double
          example: 2100.0
        width_mm:
          type: number
          format: double
          example: 600.0
        thickness_mm:
          type: number
          format: double
          example: 18.0
        materialId:
          type: string
          format: uuid
          example: "550e8400-e29b-41d4-a716-446655440000"
        edgingId:
          type: string
          format: uuid
          nullable: true
          example: "650e8400-e29b-41d4-a716-446655440001"
        quantity:
          type: integer
          minimum: 1
          example: 2

    BomSubmit:
      type: object
      required:
        - projectId
        - lines
        - metadata
      properties:
        projectId:
          type: string
          format: uuid
        lines:
          type: array
          items:
            $ref: '#/components/schemas/BomLine'
        metadata:
          type: object
          properties:
            generatedAt:
              type: string
              format: date-time
            cabinetBilderVersion:
              type: string
            sha256:
              type: string
              pattern: '^[a-f0-9]{64}$'
```

## Integration Notes

1. **Katalógus referenciák:** `materialId` és `edgingId` a ti ERP törzsadataitokra mutatnak
2. **SHA-256 hash:** Immutability garantálás (Cabinet Core principle)
3. **11-lépéses árkalkuláció:** A Cabinet MCP-host ezt implementálja (skeleton_create → compute_bom → cost_calculation)
4. **JSON-ready:** A BOM webre kész JSON formátumban kerül generálásra

## Következő Lépések

1. **Backend terminál:** Ha felébred az OpenAPI draft véglegesítésére, ezt a sémát használhatja referenciának
2. **Production API:** A `POST /api/production/bom-submit` endpoint tervezése ezzel a kontrakttal
3. **Material/Edging katalógus:** Visszafelé (VPS → Cabinet) a törzsadat pull API-hoz spec várunk

## Federation Kommunikáció - Confirmed

Köszönjük a `cabinet-bridge` címzés bevezetését! A korábbi `to: cabinet` címzés valóban nem működött. Mostantól a kommunikáció zökkenőmentes.

---

_Cabinet Bridge — VPS Federation Connector — 2026-07-11 14:15_
