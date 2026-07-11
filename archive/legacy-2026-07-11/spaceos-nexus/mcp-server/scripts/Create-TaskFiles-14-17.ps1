
# Create-TaskFiles-14-17.ps1
# Creates all TASK-XX-YY.md files and implementation-summary/ for EPIC-14..17 (and 18-20)

$base = "c:\Users\szant\Documents\Development\JoineryTech.McpServer\Docs"
$pm = "$base\mcp-pm-engine\delivery\pm-engine\milestones"

function Write-Task($filePath, $id, $title, $epic, $scope, $priority, $role, $depends, $description, $criteria) {
    $dir = Split-Path $filePath
    if (!(Test-Path $dir)) { New-Item -ItemType Directory -Force $dir | Out-Null }
    $dep = if ($depends) { "`ndepends_on: $depends" } else { "" }
    $content = "---`nid: $id`ntitle: `"$title`"`ntype: task`nepic: $epic`nscope: $scope`nstatus: pending`npriority: $priority`nrole: $role`ncreated: 2026-03-04$dep`nfsm_state: `"BACKLOG_READY`"`nfsm_retry_count: 0`n---`n`n# ${id}: $title`n`n## Description`n`n$description`n`n## Acceptance Criteria`n`n$criteria`n"
    Set-Content -Path $filePath -Value $content -Encoding UTF8
    Write-Host "  Created: $(Split-Path $filePath -Leaf)"
}

function New-ImplDir($epicPath) {
    $d = "$epicPath\implementation-summary"
    if (!(Test-Path $d)) { New-Item -ItemType Directory -Force $d | Out-Null }
    $g = "$d\.gitkeep"
    if (!(Test-Path $g)) { "" | Set-Content $g }
}

# ── EPIC-14 ──────────────────────────────────────────────────────────────────
Write-Host "=== EPIC-14 ==="
$e14 = "$pm\milestone_03\epic_14"
New-ImplDir $e14
Write-Task "$e14\tasks\TASK-14-01.md" "TASK-14-01" "PM séma végelegesítése és tervezése" "EPIC-14" "pm-engine" "P1" "backend_developer" "" `
    "A PM hierarchia SQLite sémájának véglegesítése: programs, projects, milestones, epics, tasks, task_status_log táblák és indexek tervezése." `
    "- [ ] Az összes PM tábla DDL definiálva és dokumentálva`n- [ ] FK constraint-ek helyes hierarchia betartatáshoz`n- [ ] task_status_log tábla audit trail-hez`n- [ ] Tech Lead jóváhagyja az architektúrát"
Write-Task "$e14\tasks\TASK-14-02.md" "TASK-14-02" "PM táblák hozzáadása AgentDb.ts-hez" "EPIC-14" "pm-engine" "P1" "backend_developer" "TASK-14-01" `
    "Az AgentDb.ts kiterjesztése PM táblák inicializálásával és CRUD metódusok hozzáadásával." `
    "- [ ] initPmSchema() metódus létrehozza a PM táblákat`n- [ ] getProgramState(programId): ProgramState implementálva`n- [ ] getNextTasks(projectId, limit): Task[] implementálva`n- [ ] updateTaskStatus(taskSlug, newStatus, agentId): void implementálva`n- [ ] TypeScript strict typing"
Write-Task "$e14\tasks\TASK-14-03.md" "TASK-14-03" "seed-pm-db.ts: state.md fájlok parse + DB feltöltés" "EPIC-14" "pm-engine" "P1" "backend_developer" "TASK-14-02" `
    "A seed-pm-db.ts szkript implementálása, amely a Docs/ alatti state.md fájlokat parszolja és feltölti a PM táblákat." `
    "- [ ] scripts/seed-pm-db.ts létezik és futtatható`n- [ ] Docs/**/state.md fájlokat beolvassa és parszolja YAML frontmatter-rel`n- [ ] programs, projects, milestones, epics, tasks táblák feltöltve`n- [ ] INSERT OR REPLACE szemantika (idempotens)`n- [ ] Befejezéskor statisztika logolva (sorok száma)"
Write-Task "$e14\tasks\TASK-14-04.md" "TASK-14-04" "seed-pm-db.ts integrálása a fő seeder-be" "EPIC-14" "pm-engine" "P1" "backend_developer" "TASK-14-03" `
    "A seed-pm-db.ts integrálása a scripts/seed-agent-db.ts-be, hogy egy parancs (`npm run seed`) mindent feltöltsön." `
    "- [ ] npm run seed lefuttatja seed-agent-db.ts + seed-pm-db.ts-t`n- [ ] Helyes sorrend: agent DB először, PM DB utána (FK constraint-ek miatt)`n- [ ] Hiba az egyik részben nem állítja le a másikat (graceful error)"
Write-Task "$e14\tasks\TASK-14-05.md" "TASK-14-05" "Unit teszt: PM seeder idempotencia" "EPIC-14" "pm-engine" "P2" "backend_developer" "TASK-14-03" `
    "Unit teszt: a PM seeder kétszer futtatva nem duplikál rekordot, és az összes PM tábla helyes adatokat tartalmaz." `
    "- [ ] Seeder kétszer futtatva rekordszám nem változik`n- [ ] getProgramState() helyesen adja vissza a hierarchiát`n- [ ] getNextTasks() BACKLOG_READY feladatokat ad vissza`n- [ ] npm test után zöld"

# ── EPIC-15 ──────────────────────────────────────────────────────────────────
Write-Host "=== EPIC-15 ==="
$e15 = "$pm\milestone_03\epic_15"
New-ImplDir $e15
Write-Task "$e15\tasks\TASK-15-01.md" "TASK-15-01" "PmService.ts implementálása" "EPIC-15" "pm-engine" "P1" "backend_developer" "" `
    "A PmService TypeScript osztály implementálása: getProjectState(), getNextTasks(), updateTaskStatus() metódusokkal." `
    "- [ ] src/mcp/PmService.ts létezik`n- [ ] getProjectState(projectId): ProjectState implementálva`n- [ ] getNextTasks(projectId, limit): Task[] implementálva`n- [ ] updateTaskStatus(slug, status, agentId): void implementálva`n- [ ] Unit tesztelheto (AgentDb mock-olható)"
Write-Task "$e15\tasks\TASK-15-02.md" "TASK-15-02" "AgentDb PM lekérdezési metódusok" "EPIC-15" "pm-engine" "P1" "backend_developer" "TASK-15-01" `
    "Az AgentDb.ts kiterjesztése komplex PM lekérdezési metódusokkal (JOIN-ok, state-szűrés, aggregáció)." `
    "- [ ] getProjectState() JOIN-t hajt végre programs → projects → milestones → epics`n- [ ] getNextTasks() BACKLOG_READY feladatokat szűr prioritás szerint`n- [ ] updateTaskStatus() task_status_log-ba auditot ír`n- [ ] TypeScript strict typing, nincs any"
Write-Task "$e15\tasks\TASK-15-03.md" "TASK-15-03" "PM tool-ok regisztrálása mcpServer.ts-ben" "EPIC-15" "pm-engine" "P1" "backend_developer" "TASK-15-01" `
    "A get_project_state, get_next_tasks, update_task_status MCP tool-ok regisztrálása mcpServer.ts-ben zod schema validációval." `
    "- [ ] get_project_state tool regisztrálva (pm_read jog szükséges)`n- [ ] get_next_tasks tool regisztrálva (pm_read jog szükséges)`n- [ ] update_task_status tool regisztrálva (pm_write jog szükséges)`n- [ ] Zod input schema validáció mindhárom tool-hoz"
Write-Task "$e15\tasks\TASK-15-04.md" "TASK-15-04" "RBAC schema.yaml frissítése PM jogokkal" "EPIC-15" "pm-engine" "P1" "backend_developer" "TASK-15-03" `
    "Az érintett role schema.yaml fájlok frissítése pm_read és pm_write engedélyekkel (orchestrator, tech_lead)." `
    "- [ ] orchestrator role schema.yaml tartalmaz pm_read + pm_write engedélyt`n- [ ] tech_lead role schema.yaml tartalmaz pm_read engedélyt`n- [ ] Más role-ok nem kapnak PM hozzáférést automatikusan`n- [ ] npm run seed után a DB is frissül"
Write-Task "$e15\tasks\TASK-15-05.md" "TASK-15-05" "Unit teszt: getProjectState mock DB-vel" "EPIC-15" "pm-engine" "P2" "backend_developer" "TASK-15-01" `
    "Unit teszt: PmService.getProjectState() mock AgentDb-vel helyes hierarchiát ad vissza." `
    "- [ ] getProjectState unit teszt zöld`n- [ ] Visszaadja a program → project → milestone → epic → task hierarchiát`n- [ ] Helyes fsm_state szűrés`n- [ ] npm test után zöld"
Write-Task "$e15\tasks\TASK-15-06.md" "TASK-15-06" "E2E teszt: Orchestrator scenario" "EPIC-15" "pm-engine" "P1" "backend_developer" "TASK-15-03" `
    "E2E Playwright teszt: Orchestrator agent get_next_tasks → update_task_status teljes flow lefedett." `
    "- [ ] bootstrap_agent(orchestrator) + get_next_tasks E2E teszt zöld`n- [ ] update_task_status(slug, IN_DEV) E2E teszt zöld`n- [ ] task_status_log-ban megjelenik az állapotváltás`n- [ ] npm test után zöld"

# ── EPIC-16 ──────────────────────────────────────────────────────────────────
Write-Host "=== EPIC-16 ==="
$e16 = "$pm\milestone_03\epic_16"
New-ImplDir $e16
Write-Task "$e16\tasks\TASK-16-01.md" "TASK-16-01" "REST API végpontok tervezése + OpenAPI spec" "EPIC-16" "pm-engine" "P1" "backend_developer" "" `
    "A PM REST API végpontok megtervezése és OpenAPI/Swagger spec dokumentálása." `
    "- [ ] OpenAPI spec draft dokumentálva (GET /api/program, /api/projects, /api/tasks, PATCH /api/tasks/:slug/status)`n- [ ] Auth stratégia dokumentálva (API key header)`n- [ ] Tech Lead jóváhagyja a tervet"
Write-Task "$e16\tasks\TASK-16-02.md" "TASK-16-02" "pmRouter.ts + pmController.ts GET végpontok" "EPIC-16" "pm-engine" "P1" "backend_developer" "TASK-16-01" `
    "A pmRouter.ts és pmController.ts implementálása: GET végpontok a projekt hierarchia lekérdezéséhez." `
    "- [ ] src/mcp/pmRouter.ts létezik Express Router-rel`n- [ ] GET /api/program, /api/projects, /api/milestones/:id, /api/epics/:id, /api/tasks implementálva`n- [ ] PmService-t hív (nem közvetlen DB hívás)`n- [ ] TypeScript strict typing"
Write-Task "$e16\tasks\TASK-16-03.md" "TASK-16-03" "API key autentikáció middleware" "EPIC-16" "pm-engine" "P1" "backend_developer" "TASK-16-02" `
    "Express middleware az API key alapú autentikációhoz a PM REST API-hoz." `
    "- [ ] src/mcp/apiKeyAuth.ts middleware létezik`n- [ ] X-API-Key header ellenőrzés`n- [ ] API key konfigurálható env var-ból (PM_API_KEY)`n- [ ] Érvénytelen kulcs → 401 válasz`n- [ ] A middleware alkalmazva a pmRouter-re"
Write-Task "$e16\tasks\TASK-16-04.md" "TASK-16-04" "PATCH /api/tasks/:slug/status implementálása" "EPIC-16" "pm-engine" "P1" "backend_developer" "TASK-16-02" `
    "A PATCH /api/tasks/:slug/status REST végpont implementálása task állapot frissítéséhez." `
    "- [ ] PATCH /api/tasks/:slug/status → updateTaskStatus() hívás`n- [ ] Request body: { status, agent_id } validálva`n- [ ] Érvénytelen státusz → 400 válasz`n- [ ] Nem létező task → 404 válasz`n- [ ] task_status_log audit bejegyzés létrejön"
Write-Task "$e16\tasks\TASK-16-05.md" "TASK-16-05" "src/index.ts regisztrálás + CORS konfiguráció" "EPIC-16" "pm-engine" "P1" "backend_developer" "TASK-16-03" `
    "A pmRouter regisztrálása src/index.ts-ben, CORS konfiguráció és PORT beállítás." `
    "- [ ] app.use('/api', apiKeyAuth, pmRouter) regisztrálva`n- [ ] CORS konfiguráció: ALLOWED_ORIGINS env var-ból`n- [ ] A REST API a szerver indulásával elérhető`n- [ ] Startup log: REST PM API available at /api"
Write-Task "$e16\tasks\TASK-16-06.md" "TASK-16-06" "E2E teszt: GET /api/program" "EPIC-16" "pm-engine" "P1" "backend_developer" "TASK-16-02" `
    "E2E teszt: GET /api/program visszaadja a helyes program struktúrát, auth nélkül 401-et ad." `
    "- [ ] GET /api/program valid API key-jel → helyes JSON struktúra`n- [ ] GET /api/program API key nélkül → 401`n- [ ] A válasz tartalmaz legalább program.id, projects[] mezőket`n- [ ] npm test után zöld"
Write-Task "$e16\tasks\TASK-16-07.md" "TASK-16-07" "Teszt: state.md fájlok törlése után API működik" "EPIC-16" "pm-engine" "P2" "backend_developer" "TASK-16-05" `
    "Integrációs teszt: a Docs/**state.md fájlok hiányában az API DB-ből kiszolgál." `
    "- [ ] state.md fájlok ideiglenes eltávolítása tesztkörnyezetben`n- [ ] GET /api/program → ugyanolyan választ ad`n- [ ] Dokumentálva: a rendszer fájlrendszer-független PM adatokhoz"

# ── EPIC-17 ──────────────────────────────────────────────────────────────────
Write-Host "=== EPIC-17 ==="
$e17 = "$pm\milestone_04\epic_17"
New-ImplDir $e17
Write-Task "$e17\tasks\TASK-17-01.md" "TASK-17-01" "Marketing domain role csomag tervezése" "EPIC-17" "pm-engine" "P1" "backend_developer" "" `
    "A marketing domain minimális, de valószerű role csomagjának megtervezése (campaign_manager). Megalapozza a domain-agnosztikus tesztet." `
    "- [ ] Role struktúra terv: campaign_manager.role.md, schema.yaml, runbook.md`n- [ ] Legalább 3 MCP tool engedély a schema.yaml-ban`n- [ ] Tech Lead jóváhagyja a minimális terjedelmet`n- [ ] Knowledge article témája meghatározva"
Write-Task "$e17\tasks\TASK-17-02.md" "TASK-17-02" "database/roles/marketing/campaign_manager/ létrehozása" "EPIC-17" "pm-engine" "P1" "backend_developer" "TASK-17-01" `
    "A marketing domain campaign_manager role fájljainak létrehozása." `
    "- [ ] database/roles/marketing/campaign_manager/campaign_manager.role.md létezik`n- [ ] database/roles/marketing/campaign_manager/campaign_manager.schema.yaml létezik`n- [ ] database/roles/marketing/campaign_manager/campaign_manager.runbook.md létezik`n- [ ] A fájlok tartalma valószerű (nem placeholder)"
Write-Task "$e17\tasks\TASK-17-03.md" "TASK-17-03" "database/knowledge/marketing/ knowledge article" "EPIC-17" "pm-engine" "P1" "backend_developer" "TASK-17-01" `
    "Legalább egy knowledge article létrehozása a marketing domain-hez a ChromaDB indexeléshez." `
    "- [ ] database/knowledge/marketing/campaign_management.knowledge.md létezik`n- [ ] A tartalom legalább 200 szavas, valószerű`n- [ ] npm run seed után indexelésre kerül a ChromaDB-be"
Write-Task "$e17\tasks\TASK-17-04.md" "TASK-17-04" "npm run seed futtatása — hibamentes, row count nő" "EPIC-17" "pm-engine" "P1" "backend_developer" "TASK-17-02" `
    "A seed-agent-db.ts futtatása a marketing domain hozzáadása után: hibamentes, row count nő a roles táblában." `
    "- [ ] npm run seed hibamentes futás`n- [ ] roles tábla: campaign_manager bejegyzés megjelenik`n- [ ] role_schemas tábla: campaign_manager schema megjelenik`n- [ ] ChromaDB knowledge_marketing kollekcio létrejön"
Write-Task "$e17\tasks\TASK-17-05.md" "TASK-17-05" "E2E teszt: bootstrap_agent marketing domain" "EPIC-17" "pm-engine" "P1" "backend_developer" "TASK-17-04" `
    "E2E teszt: bootstrap_agent('marketing', 'campaign_manager') teljes és valószerű kontextust ad." `
    "- [ ] bootstrap_agent(marketing, campaign_manager) identity, role_content, runbook, allowed_tools tartalmazza`n- [ ] A allowed_tools lista megfelel a schema.yaml-ban definiáltaknak`n- [ ] Nincs hardcoded engineering domain feltételezés a logikában"
Write-Task "$e17\tasks\TASK-17-06.md" "TASK-17-06" "E2E teszt: store_experience + search_experience marketing-ben" "EPIC-17" "pm-engine" "P1" "backend_developer" "TASK-17-05" `
    "E2E teszt: store_experience és search_experience marketing domain kontextusban működik." `
    "- [ ] store_experience marketing session-ben sikeres`n- [ ] search_experience visszaadja a tárolt tapasztalatot`n- [ ] Az episodic kollekcioban marketing tag jelenik meg"
Write-Task "$e17\tasks\TASK-17-07.md" "TASK-17-07" "Kód audit: nincs hardcoded domain/fájlútvonal" "EPIC-17" "pm-engine" "P2" "backend_developer" "TASK-17-05" `
    "Kód audit: a core logikában (AgentDb, RbacFilter, BootstrapService, EpisodicMemoryService) nincs hardcoded 'engineering' domain vagy fájlútvonal." `
    "- [ ] grep -r 'engineering' src/ – csak teszt fixture-ökben szerepelhet`n- [ ] grep -r 'database/roles' src/ – nincs közvetlen fájlrendszer hivatkozás`n- [ ] Audit eredménye dokumentálva az implementation-summary-ban"

Write-Host ""
Write-Host "All EPIC-14..17 tasks created successfully!"
