
# Create-TaskFiles.ps1
# Creates all TASK-XX-YY.md files and implementation-summary/ placeholders for EPIC-09..17

$base = "c:\Users\szant\Documents\Development\JoineryTech.McpServer\Docs"
$m    = "$base\mcp-context-server\delivery\mcp-maintenance\milestones"
$pm   = "$base\mcp-pm-engine\delivery\pm-engine\milestones"

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

# ── EPIC-09 ──────────────────────────────────────────────────────────────────
Write-Host "=== EPIC-09 ==="
$e09 = "$m\milestone_02\epic_09"
New-ImplDir $e09
Write-Task "$e09\tasks\TASK-09-05.md" "TASK-09-05" "Unit teszt: seeder idempotencia" "EPIC-09" "mcp-maintenance" "P2" "backend_developer" "TASK-09-03" `
    "Unit tesztek a seeder idempotenciajanak ellenorzesere es az AgentDb CRUD metódusainak validalasara. In-memory SQLite DB hasznalando a tesztekben." `
    "- [ ] AgentDb.getRole(domain, role) unit teszt zold`n- [ ] AgentDb.getRoleSchema visszaadja a parsolt mcp_tool_permissions tombot`n- [ ] Seeder ketszer futtatva rekordszam nem valtozik`n- [ ] npm test utan mind zold"

# ── EPIC-10 ──────────────────────────────────────────────────────────────────
Write-Host "=== EPIC-10 ==="
$e10 = "$m\milestone_02\epic_10"
New-ImplDir $e10
Write-Task "$e10\tasks\TASK-10-01.md" "TASK-10-01" "Dependency: EPIC-09 lezarva" "EPIC-10" "mcp-maintenance" "P0" "orchestrator" "EPIC-09" `
    "EPIC-10 elofeltétele: EPIC-09 (AgentDb) lezárva és az agent.db feltöltve. Ez nem fejlesztési feladat, hanem dependency marker." `
    "- [ ] EPIC-09 fsm_state: CLOSED_DONE`n- [ ] AgentDb.getRole + getRoleSchema mukodik a feltoltott DB-vel"
Write-Task "$e10\tasks\TASK-10-02.md" "TASK-10-02" "bootstrap_agent tool regisztralasa" "EPIC-10" "mcp-maintenance" "P1" "backend_developer" "TASK-10-01" `
    "A bootstrap_agent MCP tool regisztralasa a mcpServer.ts-ben zod schema validacioval. A tool legyen elerheto RBAC-on kivulrol (public tool)." `
    "- [ ] server.tool('bootstrap_agent', ...) regisztralva mcpServer.ts-ben`n- [ ] Zod schema: domain, role (kotelez), intent (optional), context (optional)`n- [ ] A tool lathato az MCP tool listaban`n- [ ] Ha domain/role hianyzik -> strukturalt hibavalasz"
Write-Task "$e10\tasks\TASK-10-03.md" "TASK-10-03" "BootstrapService.ts payload logika" "EPIC-10" "mcp-maintenance" "P1" "backend_developer" "TASK-10-02" `
    "Kulonallo BootstrapService.ts service osztaly implementalasa, amely osszeallitja a bootstrap payloadt az AgentDb-bol." `
    "- [ ] src/mcp/BootstrapService.ts letezik`n- [ ] bootstrap(params) metodus visszaad BootstrapPayload tipust`n- [ ] Tartalmaz: role_content, runbook_content, allowed_tools, identity, session_id`n- [ ] Unit tesztelheto (AgentDb mock-olható)"
Write-Task "$e10\tasks\TASK-10-04.md" "TASK-10-04" "Intent-alapu workflow + template csatolas" "EPIC-10" "mcp-maintenance" "P1" "backend_developer" "TASK-10-03" `
    "A request_task intent kezelesének implementalasa: a server a track alapjan csatolja a megfelelo workflow-t es template-et a bootstrap payloadhoz." `
    "- [ ] intent='request_task' + track='delivery' -> workflow_content + task_template`n- [ ] intent='request_task' + track='discovery' -> discovery workflow + hypothesis template`n- [ ] intent='resume_task' -> fsm_state + current workflow"
Write-Task "$e10\tasks\TASK-10-05.md" "TASK-10-05" "Session creation integralas" "EPIC-10" "mcp-maintenance" "P1" "backend_developer" "TASK-10-03" `
    "A SessionManager integralasa a bootstrap folyamatba: minden bootstrap_agent hivas uj session_id-t general." `
    "- [ ] SessionManager.createSession(role) meghivva minden bootstrap hiban`n- [ ] session_id visszakerül a payloadba`n- [ ] Session letrehozasi hiba nem akadalyozza a bootstrap-ot (graceful fallback)"
Write-Task "$e10\tasks\TASK-10-06.md" "TASK-10-06" "E2E teszt: identify payload" "EPIC-10" "mcp-maintenance" "P1" "backend_developer" "TASK-10-02" `
    "E2E Playwright teszt: bootstrap_agent identify intent valasz tartalmaz role_content, runbook_content, allowed_tools, identity, session_id mezoket." `
    "- [ ] bootstrap-agent.test.ts: identify intent E2E teszt zold`n- [ ] A valasz strukturalt JSON tartalmaz minden kotelez mezoket`n- [ ] Hibas role eseten strukturalt hibavalasz (nem 500)"
Write-Task "$e10\tasks\TASK-10-07.md" "TASK-10-07" "E2E teszt: request_task payload" "EPIC-10" "mcp-maintenance" "P2" "backend_developer" "TASK-10-04" `
    "E2E teszt: request_task intent (delivery + discovery track) valasz tartalmazza a megfelelo workflow + template mezoket." `
    "- [ ] request_task delivery track E2E teszt zold`n- [ ] request_task discovery track E2E teszt zold`n- [ ] Mindket teszt npm test utan zold"

# ── EPIC-11 ──────────────────────────────────────────────────────────────────
Write-Host "=== EPIC-11 ==="
$e11 = "$m\milestone_02\epic_11"
New-ImplDir $e11
Write-Task "$e11\tasks\TASK-11-01.md" "TASK-11-01" "Dependency: EPIC-09 lezarva" "EPIC-11" "mcp-maintenance" "P0" "orchestrator" "EPIC-09" `
    "EPIC-11 elofeltétele: EPIC-09 lezarva, AgentDb es role_schemas tabla elerheto. Ez dependency marker, nem fejlesztesi feladat." `
    "- [ ] EPIC-09 fsm_state: CLOSED_DONE`n- [ ] role_schemas tabla feltoltve az osszes meglevo role-ra"
Write-Task "$e11\tasks\TASK-11-02.md" "TASK-11-02" "AgentDb.findSchemaByRoleName metodus" "EPIC-11" "mcp-maintenance" "P1" "backend_developer" "TASK-11-01" `
    "Az AgentDb.findSchemaByRoleName(domain, role) metodus implementalasa, amely visszaadja a parsolt mcp_tool_permissions tombott." `
    "- [ ] findSchemaByRoleName(domain, role): string[] | null implementalva`n- [ ] Visszaadja a JSON-parsolt mcp_tool_permissions tombott`n- [ ] Nem talalt role eseten null visszaadasa (nem exception)"
Write-Task "$e11\tasks\TASK-11-03.md" "TASK-11-03" "RbacFilter refactor: YAML-scan eltavolitasa" "EPIC-11" "mcp-maintenance" "P1" "backend_developer" "TASK-11-02" `
    "A RbacFilter osztaly refactoralasa: az YAML-fajlrendszer scan eltavolítasa, helyette AgentDb-alapu lekérdezés." `
    "- [ ] RbacFilter konstruktorabol eltavolitva a fajlrendszer-scan logika`n- [ ] loadAllPermissions() metodus eltavolitva vagy ures stub-ba alakitva`n- [ ] AgentDb fugg injektalhato (DI)"
Write-Task "$e11\tasks\TASK-11-04.md" "TASK-11-04" "RbacFilter.getAllowedTools DB lekérdezésre" "EPIC-11" "mcp-maintenance" "P1" "backend_developer" "TASK-11-03" `
    "A RbacFilter.getAllowedTools(domain, role) metodus DB-alapu implementalasa cache-szel." `
    "- [ ] getAllowedTools(domain, role) AgentDb.findSchemaByRoleName-t hiv`n- [ ] Eredmeny in-memory cache-elt (session-szintu)`n- [ ] Cache invalidalhato (pl. test teardown-ban)"
Write-Task "$e11\tasks\TASK-11-05.md" "TASK-11-05" "DI frissítese: mcpServer.ts + index.ts" "EPIC-11" "mcp-maintenance" "P1" "backend_developer" "TASK-11-04" `
    "A mcpServer.ts es index.ts dependency injection bedrotazasanak frissitese, hogy az RbacFilter AgentDb-t kapjon." `
    "- [ ] RbacFilter AgentDb instance-t kap konstruktorban`n- [ ] mcpServer.ts frissitett DI-val indul`n- [ ] index.ts helyes peldanyositasi sorrend: AgentDb -> RbacFilter -> McpServer"
Write-Task "$e11\tasks\TASK-11-06.md" "TASK-11-06" "Unit teszt: RbacFilter DB mockkal" "EPIC-11" "mcp-maintenance" "P1" "backend_developer" "TASK-11-04" `
    "Unit tesztek az RbacFilter helyes mukodesenek ellenorzesere, DB mock hasznalasaval (nem fajlrendszerrel)." `
    "- [ ] RbacFilter unit teszt AgentDb mock-kal`n- [ ] Helyes role -> helyes toolset visszaadasa`n- [ ] Ismeretlen role -> ures tomb (nem exception)`n- [ ] npm test utan zold"
Write-Task "$e11\tasks\TASK-11-07.md" "TASK-11-07" "E2E teszt: RBAC engedelyek betoltese" "EPIC-11" "mcp-maintenance" "P2" "backend_developer" "TASK-11-05" `
    "Meglevu E2E RBAC tesztek futtatasa az uj DB-alapu RbacFilter-rel. Minden meglevo teszt zoldnek kell maradni." `
    "- [ ] Osszes meglevo RBAC E2E teszt zold`n- [ ] Nincs regresszio az RBAC-ban`n- [ ] A szerver DB nelkul nem indul (helyes hibauzenette)"

# ── EPIC-12 ──────────────────────────────────────────────────────────────────
Write-Host "=== EPIC-12 ==="
$e12 = "$m\milestone_02\epic_12"
New-ImplDir $e12
Write-Task "$e12\tasks\TASK-12-01.md" "TASK-12-01" "SQLite séma kiterjesztés: epizódikus memória" "EPIC-12" "mcp-maintenance" "P1" "backend_developer" "" `
    "Az agent.db séma kiterjesztése epizódikus memória táblákkal: episodes, episode_highlights, episodes_fts (FTS5)." `
    "- [ ] episodes, episode_highlights, episodes_fts(FTS5) táblák DDL meg van írva`n- [ ] Séma forward-compatible (meglevo adatok nem érintett)`n- [ ] seed-agent-db.ts frissítve (új táblák init)`n- [ ] Tech Lead jóváhagyja a séma változást"
Write-Task "$e12\tasks\TASK-12-02.md" "TASK-12-02" "EpisodicMemoryService.ts implementálása" "EPIC-12" "mcp-maintenance" "P1" "backend_developer" "TASK-12-01" `
    "Az EpisodicMemoryService TypeScript osztály implementálása: storeExperience(), searchExperience(), reflectSession() metódusokkal." `
    "- [ ] src/mcp/EpisodicMemoryService.ts létezik`n- [ ] storeExperience(episode): episodeId implementálva`n- [ ] searchExperience(query): Episode[] implementálva (FTS5 + vector hibrid)`n- [ ] reflectSession(sessionId): SessionSummary implementálva`n- [ ] Unit tesztelheto (AgentDb mock-olható)"
Write-Task "$e12\tasks\TASK-12-03.md" "TASK-12-03" "VectorStore.ts kiterjesztése epizódikus indexszel" "EPIC-12" "mcp-maintenance" "P1" "backend_developer" "TASK-12-01" `
    "A VectorStore.ts kiterjesztése episodic collection kezeléssel: addEpisode() és searchEpisodes() metódusok." `
    "- [ ] VectorStore.addEpisode(episode) ChromaDB episodic collection-be szür`n- [ ] VectorStore.searchEpisodes(query, limit) szemantikus keresést végez`n- [ ] Az episodic kollekcio elválasztott a knowledge kollekcióktól"
Write-Task "$e12\tasks\TASK-12-04.md" "TASK-12-04" "MCP tool-ok regisztrálása: store/search/reflect" "EPIC-12" "mcp-maintenance" "P1" "backend_developer" "TASK-12-02" `
    "A store_experience, search_experience, reflect_session MCP tool-ok regisztralasa mcpServer.ts-ben." `
    "- [ ] store_experience tool regisztralva (RBAC-on belul, session-kötött)`n- [ ] search_experience tool regisztralva (olvasási jog szükséges)`n- [ ] reflect_session tool regisztralva (orchestrator role)`n- [ ] Zod input schema validáció mindhárom tool-hoz"
Write-Task "$e12\tasks\TASK-12-05.md" "TASK-12-05" "Unit teszt: storeExperience SQLite + ChromaDB" "EPIC-12" "mcp-maintenance" "P2" "backend_developer" "TASK-12-02" `
    "Unit teszt: storeExperience() mindkét rétegbe ír (SQLite + ChromaDB). In-memory SQLite és ChromaDB mock használandó." `
    "- [ ] storeExperience unit teszt zöld (happy path)`n- [ ] SQLite episodes táblában megjelenik a rekord`n- [ ] VectorStore.addEpisode meghívásra kerül`n- [ ] npm test után zöld"
Write-Task "$e12\tasks\TASK-12-06.md" "TASK-12-06" "Unit teszt: searchExperience hibrid keresés" "EPIC-12" "mcp-maintenance" "P2" "backend_developer" "TASK-12-03" `
    "Unit teszt: searchExperience() FTS5 + semantic hibrid keresés releváns epizódot ad vissza." `
    "- [ ] searchExperience unit teszt: releváns epizód megtalálható`n- [ ] FTS5 keresési útvonal lefedett`n- [ ] Semantic keresési útvonal lefedett (ChromaDB mock)`n- [ ] npm test után zöld"

# ── EPIC-13 ──────────────────────────────────────────────────────────────────
Write-Host "=== EPIC-13 ==="
$e13 = "$m\milestone_03\epic_13"
New-ImplDir $e13
Write-Task "$e13\tasks\TASK-13-01.md" "TASK-13-01" "Fájlrendszer-hívó tool-ok listázása + refaktor-terv" "EPIC-13" "mcp-maintenance" "P1" "backend_developer" "" `
    "Az összes olyan MCP tool azonosítása mcpServer.ts-ben, amelyek közvetlenül a fájlrendszert olvassák. Refaktor-terv dokumentálása." `
    "- [ ] Lista dokumentálva: tool-ok neve + érintett fájlrendszer-hívás`n- [ ] Refaktor-terv: melyik AgentDb metódus váltja ki`n- [ ] Tech Lead jóváhagyja a tervet"
Write-Task "$e13\tasks\TASK-13-02.md" "TASK-13-02" "get_role, list_roles AgentDb-re átírva" "EPIC-13" "mcp-maintenance" "P1" "backend_developer" "TASK-13-01" `
    "A get_role, list_roles MCP tool-ok refaktorálása: fájlrendszer-hívás helyett AgentDb.getRole() / AgentDb.listRoles()." `
    "- [ ] get_role() AgentDb.getRole() metódust hív`n- [ ] list_roles() AgentDb.listRoles() metódust hív`n- [ ] Nincs fs.readFile / path.join a tool implementációban`n- [ ] Meglévő tesztek zöldek"
Write-Task "$e13\tasks\TASK-13-03.md" "TASK-13-03" "get_workflow, get_template AgentDb-re átírva" "EPIC-13" "mcp-maintenance" "P1" "backend_developer" "TASK-13-01" `
    "A get_workflow, get_template tool-ok refaktorálása AgentDb-alapúra." `
    "- [ ] get_workflow() AgentDb.getWorkflow() metódust hív`n- [ ] get_template() AgentDb.getTemplate() metódust hív`n- [ ] Nincs fájlrendszer-hívás egyik tool implementációban sem`n- [ ] Meglévő tesztek zöldek"
Write-Task "$e13\tasks\TASK-13-04.md" "TASK-13-04" "get_core AgentDb.getStandard-ra átírva" "EPIC-13" "mcp-maintenance" "P1" "backend_developer" "TASK-13-01" `
    "A get_core (standards) MCP tool refaktorálása AgentDb.getStandard()-alapúra." `
    "- [ ] get_core() AgentDb.getStandard(stdId) metódust hív`n- [ ] Nincs database/standards/ fájlrendszer-scan`n- [ ] Meglévő tesztek zöldek"
Write-Task "$e13\tasks\TASK-13-05.md" "TASK-13-05" "Seeder + ChromaDB újraindexelés integrálása" "EPIC-13" "mcp-maintenance" "P1" "backend_developer" "TASK-13-02" `
    "A seed-agent-db.ts kiterjesztése: seeder futtatása után automatikusan lefut a ChromaDB újraindexelés (indexKnowledgeBase())." `
    "- [ ] npm run seed -> SQLite feltöltés + ChromaDB újraindexelés egy parancsban`n- [ ] Hiba a ChromaDB részben nem állítja le a SQLite seedert`n- [ ] Statisztika logolva mindkét lépéshez"
Write-Task "$e13\tasks\TASK-13-06.md" "TASK-13-06" "Meglévő E2E tesztek futtatása regresszió-ellenőrzés" "EPIC-13" "mcp-maintenance" "P1" "backend_developer" "TASK-13-04" `
    "Az összes meglévő E2E teszt futtatása a refaktor után. Nincs engedélyezett regresszió." `
    "- [ ] Minden meglévő E2E teszt zöld a refaktor után`n- [ ] Nincs új SKIP vagy FAIL`n- [ ] Test report csatolva az implementation-summary-hoz"
Write-Task "$e13\tasks\TASK-13-07.md" "TASK-13-07" "Teszt: szerver database/ mappa nélkül indul" "EPIC-13" "mcp-maintenance" "P2" "backend_developer" "TASK-13-05" `
    "Integrációs teszt: a szerver csak agent.db jelenlétével indul, a database/ mappa hiányában minden MCP tool funkcionál." `
    "- [ ] database/ mappa ideiglenes átnevezése / gitignore-olása tesztkörnyezetben`n- [ ] Szerver indul agent.db-vel`n- [ ] get_role, get_workflow, get_template mind működnek`n- [ ] Teszt dokumentálva az implementation-summary-ban"

Write-Host "All EPIC-09..13 tasks created successfully!"
