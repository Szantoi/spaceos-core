# Skill: Infrastructure Blocker Resolution Guide

## Overview

Structured diagnosis and resolution for infrastructure blockers (network, build, deploy). Reduces MTTR (Mean Time To Resolution) from hours to minutes.

## When to Use

- Build fails (NuGet, npm, Maven, Gradle)
- Network unreachable (api.nuget.org, npm registry, GitHub)
- Deployment blocked (VPS SSH, container registry, DNS)
- Service unavailable (database, cache, message queue)
- Session/process issues (tmux, screen, systemd)

## Decision Tree

```
Infrastructure Issue
  ├─ Build Problem?
  │  ├─ NuGet (C#/.NET)
  │  ├─ npm/yarn (Node.js)
  │  └─ Maven/Gradle (Java)
  │
  ├─ Network Problem?
  │  ├─ DNS (domain resolution)
  │  ├─ Firewall (port blocked)
  │  ├─ Proxy (authentication)
  │  └─ Connectivity (no internet)
  │
  ├─ Deployment Problem?
  │  ├─ VPS SSH access
  │  ├─ Container registry auth
  │  ├─ Environment variables
  │  └─ Service startup
  │
  └─ Other Problem?
     ├─ Database connection
     ├─ Cache (Redis) connection
     ├─ Message broker (RabbitMQ) connection
     └─ Session/process (tmux, screen)
```

---

## Case Study: NuGet Timeout (JoineryTech Week 2)

### Problem

```
dotnet restore
  → api.nuget.org timeout
  → Build cannot proceed
  → Backend Week 2-4 blocked
  → Impact: 24-48h delay
```

### Diagnosis Phase (5-10 minutes)

#### Step 1: Verify Network Access

```bash
# Check DNS resolution
nslookup api.nuget.org
ping api.nuget.org

# Check HTTP connectivity
curl -v https://api.nuget.org/v3/index.json
```

**Expected:** HTTP 200 response
**If timeout:** Network or firewall issue

#### Step 2: Check Firewall/Proxy

```bash
# List open ports
sudo netstat -tlnp | grep nuget

# Check firewall rules
sudo ufw status
sudo iptables -L

# Check proxy config (if applicable)
env | grep -i proxy
echo $HTTP_PROXY $HTTPS_PROXY
```

**Expected:** Port 443 (HTTPS) open, no proxy blocking
**If blocked:** Firewall rule or proxy auth needed

#### Step 3: Verify NuGet Config

```bash
# Check NuGet sources
dotnet nuget list source

# Test individual source
dotnet nuget push --dry-run <package> -s https://api.nuget.org/v3/index.json
```

**Expected:** Source list shows api.nuget.org active
**If error:** NuGet config corrupted or outdated

### Resolution Phase (15-60 minutes, depending on option)

#### Option A: Network/Firewall Fix (30-60 minutes)

**Owner:** VPS operator or DevOps

1. **Verify firewall allows HTTPS (443):**
   ```bash
   sudo ufw allow 443/tcp
   sudo iptables -A OUTPUT -p tcp --dport 443 -j ACCEPT
   ```

2. **If behind proxy, configure auth:**
   ```bash
   cat > ~/.nuget/NuGet/NuGet.Config <<EOF
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <packageSources>
       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
     </packageSources>
     <config>
       <add key="http_proxy" value="http://proxy.company.com:8080" />
       <add key="http_proxy.user" value="username" />
       <add key="http_proxy.password" value="password" />
     </config>
   </configuration>
   EOF
   ```

3. **Test fix:**
   ```bash
   dotnet restore
   ```

#### Option B: Local NuGet Cache (1-2 hours)

**Owner:** DevOps or Infrastructure

1. **Copy cached packages from another machine:**
   ```bash
   # On machine with working NuGet:
   tar -czf nuget-cache.tar.gz ~/.nuget/packages

   # Transfer to VPS
   scp nuget-cache.tar.gz vps:/tmp/

   # On VPS:
   mkdir -p ~/.nuget/packages
   tar -xzf /tmp/nuget-cache.tar.gz -C ~/
   ```

2. **Configure NuGet to use local cache first:**
   ```bash
   cat > ~/.nuget/NuGet/NuGet.Config <<EOF
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <packageSources>
       <add key="Local" value="~/.nuget/packages" />
       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
     </packageSources>
   </configuration>
   EOF
   ```

3. **Test:**
   ```bash
   dotnet restore
   ```

#### Option C: NuGet Mirror Setup (2-4 hours)

**Owner:** DevOps or Infrastructure

1. **Setup Azure Artifacts or MyGet mirror:**
   ```bash
   # Create account at https://dev.azure.com or https://www.myget.org
   # Configure organization + feed
   ```

2. **Sync packages from nuget.org:**
   ```bash
   # Azure Artifacts:
   # Dashboard → Package settings → Upstream source → Add nuget.org

   # MyGet:
   # Feed settings → Upstream sources → Add https://api.nuget.org/v3/index.json
   ```

3. **Configure local NuGet.Config:**
   ```bash
   cat > ~/.nuget/NuGet/NuGet.Config <<EOF
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <packageSources>
       <add key="CompanyMirror" value="https://pkgs.dev.azure.com/company/project/_packaging/feed/nuget/v3/index.json" />
       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
     </packageSources>
   </configuration>
   EOF
   ```

4. **Test:**
   ```bash
   dotnet restore
   ```

---

## Common Infrastructure Blockers

### 1. NuGet (C#/.NET)

| Issue | Diagnosis | Resolution |
|-------|-----------|------------|
| **Timeout** | `curl api.nuget.org` fails | Firewall rule, proxy, DNS |
| **401 Unauthorized** | Private feed auth needed | Add credentials to NuGet.Config |
| **No matching version** | Package version not in feed | Check version number, use mirror |

### 2. npm/yarn (Node.js)

| Issue | Diagnosis | Resolution |
|-------|-----------|------------|
| **Registry unreachable** | `npm registry get https://registry.npmjs.org/` | Firewall, proxy, DNS |
| **SSL certificate error** | `npm config set strict-ssl false` (temp) | Update ca-certificates or use mirror |
| **Package not found** | Package removed or private | Check npm.org, use @scoped packages |

### 3. Maven/Gradle (Java)

| Issue | Diagnosis | Resolution |
|-------|-----------|------------|
| **Repository timeout** | `mvn dependency:resolve` hangs | Firewall, proxy config |
| **Private repo auth** | Settings.xml not configured | Add credentials to ~/.m2/settings.xml |

### 4. Network/DNS

| Issue | Diagnosis | Resolution |
|-------|-----------|------------|
| **Host unreachable** | `ping api.nuget.org` fails | Check DNS (`nslookup`) or firewall |
| **Connection refused** | `telnet api.nuget.org 443` fails | Service down or port blocked |
| **Slow/intermittent** | Occasional timeouts | Check bandwidth, latency, proxy |

### 5. VPS/Deployment

| Issue | Diagnosis | Resolution |
|-------|-----------|------------|
| **SSH timeout** | `ssh -v user@vps.com` hangs | Firewall port 22, DNS resolution |
| **Docker pull fails** | Registry auth or network issue | Check Docker credentials, firewall |
| **Database connection** | Connection string or firewall | Verify host/port, security groups |

### 6. Session/Process (tmux, systemd)

| Issue | Diagnosis | Resolution |
|-------|-----------|------------|
| **Session missing** | `tmux ls` shows no session | Session crashed or not started |
| **Service won't start** | `systemctl status service` shows error | Check logs, permissions, config |

---

## Escalation Path

### Level 1: Self-Diagnosis (5-10 minutes)

**Do:**
- Run diagnostic commands (`ping`, `curl`, `telnet`, `nslookup`)
- Check config files (NuGet.Config, .npmrc, settings.xml)
- Review recent changes (proxy settings, firewall rules)

**If still blocked → Escalate to Level 2**

### Level 2: Team Lead / DevOps (15-30 minutes)

**Do:**
- Apply Option A (firewall/proxy fix) or Option B (local cache)
- Check recent infrastructure changes
- Coordinate with other teams if shared resource

**If still blocked → Escalate to Level 3**

### Level 3: Root / Infrastructure Team (1-4 hours)

**Do:**
- Setup Option C (NuGet mirror) or alternative
- Investigate systemic issues (data center network, DNS)
- Document resolution for future reference

---

## Parallel Development Workaround

### If Infrastructure Blocker Cannot Be Resolved in 24h

**Strategy:** Separate code development from build/test

#### Backend Example (NuGet Timeout)

```
Backend Team:
│
├─ Phase 1: Code-only development (no build)
│  ├─ Write Command handlers (C# code)
│  ├─ Write Query handlers (C# code)
│  ├─ Write validators (C# code)
│  └─ Write integration tests (code only)
│
└─ Phase 2: Build + test (once NuGet fixed)
   ├─ dotnet restore
   ├─ dotnet build
   ├─ dotnet test
   └─ Verify all Phase 1 code passes
```

**Timeline:**
- Day 1-2: Code 80% of Week 2-3 work (no build)
- Day 3: NuGet fixed
- Day 4: Build + test in 2-3 hours (parallel to Frontend)

**Net impact:** 24-48h infrastructure blocker → 0h project delay

---

## Prevention Checklist

**Infrastructure Team:**

- [ ] Weekly NuGet mirror sync (if using mirror)
- [ ] Monthly firewall rules audit
- [ ] Quarterly DNS propagation test
- [ ] Bi-weekly VPS connectivity check
- [ ] Monthly certificate expiration review
- [ ] Automated alerts: Package registry timeout (>30s)
- [ ] Automated alerts: VPS SSH connection failures
- [ ] Documented runbook for common issues

**Development Team:**

- [ ] NuGet.Config checked into repo
- [ ] .npmrc checked into repo (no secrets)
- [ ] Maven settings.xml template provided
- [ ] Environment variables documented (.env.example)
- [ ] Docker credentials managed securely
- [ ] Backup package sources configured (local cache + mirror)

---

## Related Patterns

- **Parallel Development Workaround** (Code-only development while infrastructure resolves)
- **Mock API Strategy** (Frontend can proceed while Backend infrastructure blocked)
- **Infrastructure Priority Escalation** (24h timeout → VPS operator escalation)

## References

- NuGet Documentation: https://learn.microsoft.com/en-us/nuget/
- npm Registry: https://registry.npmjs.org/
- Azure Artifacts: https://learn.microsoft.com/en-us/azure/devops/artifacts/
- MyGet: https://www.myget.org/

---

**Skill Created:** 2026-07-04
**Source:** JoineryTech Week 2 NuGet timeout (case study)
**MTTR Reduction:** Hours → Minutes (diagnosis + resolution)
**Applicable:** All infrastructure blockers (network, build, deploy)
