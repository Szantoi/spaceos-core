// src/routes/test.route.ts
import { Router, Request, Response } from 'express';
import axios from 'axios';
import { testGuard } from '../middleware/testGuard';
import { env } from '../config/env';

export const testRouter = Router();

// ─── Types ────────────────────────────────────────────────────────────────────

interface SeededEntities {
  orders: number;
  cuttingSheets: number;
  panelStocks: number;
  suppliers: number;
}

type SeedFn = (tenantId: string) => Promise<SeededEntities>;

// ─── Keycloak Direct Access Grant token helper ────────────────────────────────

async function getSeedToken(): Promise<string> {
  const kcTokenUrl =
    env.KC_TOKEN_URL ?? `${env.JWT_ISSUER}/protocol/openid-connect/token`;
  const params = new URLSearchParams({
    grant_type:    'client_credentials',
    client_id:     env.TEST_RUNNER_CLIENT_ID     ?? 'test-runner',
    client_secret: env.TEST_RUNNER_CLIENT_SECRET ?? '',
  });
  const r = await axios.post<{ access_token: string }>(
    kcTokenUrl,
    params.toString(),
    { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } },
  );
  return r.data.access_token;
}

// ─── Seed profiles ────────────────────────────────────────────────────────────

const seedProfiles: Record<string, SeedFn> = {
  'empty-v1': async () => ({ orders: 0, cuttingSheets: 0, panelStocks: 0, suppliers: 0 }),

  'doorstar-smoke-v1': async (tenantId) => {
    const token = await getSeedToken();
    const auth = `Bearer ${token}`;

    // ① Create Facility in Kernel (required parent for FlowEpic)
    const facilityRes = await axios.post<{ id: string }>(
      `${env.KERNEL_BASE_URL}/api/tenants/${tenantId}/facilities`,
      { name: `Doorstar Gyártó ${Date.now()}` },
      { headers: { Authorization: auth }, timeout: 10_000 },
    );
    const facilityId = facilityRes.data.id ?? facilityRes.data;

    // ② Create FlowEpic under Facility
    const epicRes = await axios.post<{ id: string }>(
      `${env.KERNEL_BASE_URL}/api/facilities/${facilityId}/flow-epics`,
      { title: 'Seed: Smoke Order' },
      { headers: { Authorization: auth }, timeout: 10_000 },
    );
    const flowEpicId = epicRes.data.id ?? epicRes.data;

    // ③ Create DoorOrder in Joinery
    await axios.post(
      `${env.JOINERY_BASE_URL}/api/orders`,
      {
        flowEpicId,
        projectId:   'SEED-SMOKE-001',
        projectName: 'E2E Smoke Order',
        clientName:  'E2E Test Client',
      },
      { headers: { Authorization: auth }, timeout: 10_000 },
    );

    return { orders: 1, cuttingSheets: 0, panelStocks: 0, suppliers: 0 };
  },

  'doorstar-cutting-ready-v1': async (tenantId) => {
    const token = await getSeedToken();
    const auth = `Bearer ${token}`;

    // ① Create Facility in Kernel (required parent for FlowEpic)
    const facilityRes = await axios.post<{ id: string }>(
      `${env.KERNEL_BASE_URL}/api/tenants/${tenantId}/facilities`,
      { name: `Doorstar Gyártó ${Date.now()}` },
      { headers: { Authorization: auth }, timeout: 10_000 },
    );
    const facilityId = facilityRes.data.id ?? facilityRes.data;

    // ② Create FlowEpic under Facility
    const epicRes = await axios.post<{ id: string }>(
      `${env.KERNEL_BASE_URL}/api/facilities/${facilityId}/flow-epics`,
      { title: 'Seed: Cutting Ready Order' },
      { headers: { Authorization: auth }, timeout: 10_000 },
    );
    const flowEpicId = epicRes.data.id ?? epicRes.data;

    // ③ Create DoorOrder in Joinery (Draft state)
    const orderRes = await axios.post<{ id: string }>(
      `${env.JOINERY_BASE_URL}/api/orders`,
      {
        flowEpicId,
        projectId:   'SEED-CUT-001',
        projectName: 'E2E Cutting Ready Order',
        clientName:  'Doorstar E2E Client',
      },
      { headers: { Authorization: auth }, timeout: 10_000 },
    );
    const doorOrderId = orderRes.data.id ?? orderRes.data;

    // ③b Add item to DoorOrder (submit requires ≥1 item)
    await axios.post(
      `${env.JOINERY_BASE_URL}/api/orders/${doorOrderId}/items`,
      {
        sorszam:              '001',
        name:                 'Seed ajtó',
        quantity:             1,
        doorType:             'Sikban',
        openingDirection:     'Left',
        wallOpeningWidth:     920,
        doorWidth:            900,
        wallOpeningHeight:    2120,
        doorHeight:           2100,
        wallOpeningThickness: 150,
        doorThickness:        45,
      },
      { headers: { Authorization: auth }, timeout: 10_000 },
    );

    // ④ Submit DoorOrder (Draft → Submitted)
    await axios.post(
      `${env.JOINERY_BASE_URL}/api/orders/${doorOrderId}/submit`,
      {},
      { headers: { Authorization: auth }, timeout: 10_000 },
    );

    // ⑤ Create CuttingSheet linked to the DoorOrder
    await axios.post(
      `${env.CUTTING_BASE_URL}/api/cutting/sheets`,
      {
        orderReference: doorOrderId,
        lines: [
          { partName: 'Door Panel A', materialType: 'HDF', widthMm: 900, heightMm: 2100, thicknessMm: 4, quantity: 1 },
          { partName: 'Door Panel B', materialType: 'HDF', widthMm: 900, heightMm: 2100, thicknessMm: 4, quantity: 1 },
        ],
      },
      { headers: { Authorization: auth }, timeout: 10_000 },
    );

    // ⑥ Create 5 PanelStock entries in Inventory via inbound movements
    const panelDefs = [
      { materialType: 'HDF',  thickness: 4,  area: 2.6928, reference: 'SEED-HDF-4-A' },
      { materialType: 'HDF',  thickness: 4,  area: 2.6928, reference: 'SEED-HDF-4-B' },
      { materialType: 'MDF',  thickness: 18, area: 2.6928, reference: 'SEED-MDF-18-A' },
      { materialType: 'MDF',  thickness: 18, area: 2.6928, reference: 'SEED-MDF-18-B' },
      { materialType: 'MDF',  thickness: 25, area: 2.6928, reference: 'SEED-MDF-25-A' },
    ];
    const now = new Date().toISOString();
    for (const panel of panelDefs) {
      await axios.post(
        `${env.INVENTORY_BASE_URL}/api/inventory/movements/inbound`,
        { ...panel, panelCount: 1, occurredAt: now },
        { headers: { Authorization: auth }, timeout: 10_000 },
      );
    }

    // ⑦ Create Supplier in Procurement (PROCUREMENT-006 — graceful if not deployed)
    let supplierCount = 0;
    try {
      await axios.post(
        `${env.PROCUREMENT_BASE_URL}/api/procurement/suppliers`,
        { name: 'Faanyag Kft.', contactEmail: 'rendeles@faanyag.hu' },
        { headers: { Authorization: auth }, timeout: 10_000 },
      );
      supplierCount = 1;
    } catch {
      // Graceful: INFRA-140 deploy pending — supplier seeding skipped
    }

    return { orders: 1, cuttingSheets: 1, panelStocks: 5, suppliers: supplierCount };
  },

  // Stub — extended in future sprint
  'doorstar-order-lifecycle-v1': async () => ({ orders: 3, cuttingSheets: 0, panelStocks: 0, suppliers: 0 }),
};

// ─── Router ───────────────────────────────────────────────────────────────────

testRouter.use('/tenants/:tenantId/*', testGuard);
testRouter.use('/tenants/:tenantId', testGuard);

testRouter.post('/tenants/:tenantId/reset', async (req: Request, res: Response) => {
  const tenantId = req.params['tenantId'] as string;
  const { seedProfile } = req.body as { seedProfile: string };

  // Profil validáció
  const seedFn = seedProfiles[seedProfile];
  if (!seedFn) {
    res.status(400).json({
      error:     'Bad request',
      message:   `Unknown seed profile: ${seedProfile}`,
      available: Object.keys(seedProfiles),
    });
    return;
  }

  const internalHeader = { 'X-SpaceOS-Internal': 'true' };
  const deleteModules = [
    { name: 'joinery',     url: `${env.JOINERY_BASE_URL}/internal/orders/by-tenant/${tenantId}?confirm=true` },
    { name: 'cutting',     url: `${env.CUTTING_BASE_URL}/internal/cutting-sheets/by-tenant/${tenantId}?confirm=true` },
    { name: 'inventory',   url: `${env.INVENTORY_BASE_URL}/internal/panel-stocks/by-tenant/${tenantId}?confirm=true` },
    { name: 'procurement', url: `${env.PROCUREMENT_BASE_URL}/internal/purchase-orders/by-tenant/${tenantId}?confirm=true` },
    { name: 'kernel',      url: `${env.KERNEL_BASE_URL}/internal/flow-epics/by-tenant/${tenantId}?confirm=true` },
  ];

  // 1. TÖRLÉS — minden modul (graceful: ha nincs endpoint, folytat)
  const deletedCounts: Record<string, unknown> = {};
  for (const mod of deleteModules) {
    try {
      const r = await axios.delete(mod.url, { headers: internalHeader });
      deletedCounts[mod.name] = (r.data as Record<string, unknown>).deletedCounts;
    } catch (err: unknown) {
      const status = axios.isAxiosError(err) ? err.response?.status : null;
      console.warn(`Test reset: ${mod.name} cleanup failed (${status ?? 'network error'}) — continuing`);
      deletedCounts[mod.name] = { error: `cleanup failed: ${status ?? 'network'}` };
    }
  }

  // 2. SEED — profil alapján
  let seededEntities: SeededEntities;
  try {
    seededEntities = await seedFn(tenantId);
  } catch (err: unknown) {
    const status = axios.isAxiosError(err) ? err.response?.status : null;
    res.status(502).json({
      error:   'Seed failed',
      message: `Seed profile '${seedProfile}' failed: ${status ?? 'network error'}`,
    });
    return;
  }

  res.json({
    tenantId,
    seedProfile,
    resetAt: new Date().toISOString(),
    deletedCounts,
    seededEntities,
  });
});
