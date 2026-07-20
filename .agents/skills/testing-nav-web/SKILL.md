---
name: testing-nav-web
description: Test the MutualFundNav-Web monitor dashboard (Dashboard, NAV History, Job Logs, Holiday Status, Kafka Events) end-to-end. Use when verifying Nav-Web UI changes without standing up the .NET NavAPI.
---

# Testing MutualFundNav-Web

Angular app (NgModule-based, **not** standalone) in `MutualFundNav-Web/`. It is a single-page
**real-time monitoring dashboard** rendered entirely by one component
(`src/app/features/monitor/mfnav-monitor.component.*`) — Dashboard, NAV History, Job Logs, Holiday
Status, Kafka Events. It talks to one .NET service (MutualFund.NavAPI) via `environment.apiBaseUrl`.
Standing up the real backend is heavy, so for frontend testing prefer a **mock API** (same approach
as `testing-scheme-web` / `testing-auth-web`).

## Prereqs / environment
- Angular CLI needs Node >= 20.19; base image may ship older → use Node 22:
  `export NVM_DIR="$HOME/.nvm" && . "$NVM_DIR/nvm.sh" && nvm use 22` (install with `nvm install 22`).
- `cd MutualFundNav-Web && npm install`, then `npx ng serve` (http://localhost:4200).
- Build check: `npx ng build --configuration development`. No lint/test script is configured.
- No routing and **no auth** — the whole dashboard is at `/`. No login/JWT needed.

## Mock-API testing pattern (recommended for frontend-only changes)
1. Write a tiny no-dep Node `http` mock server with permissive CORS. It must serve on the port in
   `environment.apiBaseUrl` (default `https://localhost:63944` — change scheme to `http` locally, see
   step 2). Endpoints the component uses (see `src/app/core/services/mfnav.service.ts`):
   - `GET /api/nav/latest` → `{ latestNavDate }`
   - `GET /api/nav/target-date` → `{ targetDate }`
   - `GET /api/nav/history` → `NavFileSummary[]` (`{ id, navDate, fileSizeBytes, recordCount,
     checksum, downloadedAt, isHoliday }`)
   - `POST /api/nav/trigger` and `POST /api/nav/trigger/{date}` → `{ date, wasStored, message }`
   - `GET /api/jobs/logs?count=` and `GET /api/jobs/logs/latest` → `JobLog[]` / `JobLog`
     (`{ id, jobName, startedAt, completedAt, isSuccess, errorMessage, details, elapsedSeconds }`)
   - `GET /api/Holidays/today` → `{ date, status, isHoliday }`
   - `GET /api/Holidays/is-trading-day?date=` → `{ date, isTradingDay, dayOfWeek }`
   - `GET /api/kafka/logs?count=` → `KafkaPublishLog[]` (`{ id, topic, eventType, messageKey,
     messageSizeBytes, isSuccess, errorMessage, publishedAt, elapsedMs, ... partition, offset }`)
   Include at least one **failed** JobLog (with `details` stack) and one failed Kafka row so the
   error-expand and red stamp pills are exercised. A working mock lived at `~/mock-nav-api/server.js`
   during the restyle test session — recreate it if gone (it is not committed).
2. Temporarily point `src/environments/environment.ts` `apiBaseUrl` at `http://localhost:63944`
   (note `http`, not `https`, to avoid TLS for the mock). **Revert before finishing**
   (`git checkout -- src/environments/environment.ts`) — never commit it.

## What to verify (functionality — unchanged by the restyle)
- Dashboard: 4 stat cards populate; "Next Run In" countdown ticks each second; auto-refresh every 30s.
- NAV History: the "Search by date…" box filters on the **raw ISO `navDate`** (e.g. `07-06` matches
  `2026-07-06`; the display string "06 Jul" will NOT match); clicking column headers sorts (icon
  `↕`→`↓`/`↑`).
- Job Logs: clicking a **FAIL** row expands a detail row with full error message + stack; the count
  chips show `✓ N` / `✗ N`; the count `<select>` (20/50/100) reloads.
- Manual Trigger: "Trigger Latest NAV" posts and shows a green success result banner.
- Holiday Status / Kafka Events: tables render; "Check Date" / count selects reload.

## Passbook restyle signatures (PR #8 — visual regression tells)
The theme should look like Investment-Web's Order/Investor passbook, NOT the old **dark** dashboard
(`#0f1117` bg, blue `#3b82f6` accent, Inter/JetBrains Mono):
- Header = dark **ink** (`#1B2A4A`) cover band, gold-outlined `MF` badge, uppercase steel eyebrow
  (`brand-sub`) ABOVE a cream **Fraunces** serif title, perforated dotted bottom edge.
- Body on **parchment** (`#FAF6EC`); sections = white paper cards with a gold left-accent title bar.
- Stat cards have a **gold** (`#C08A2E`) left border (green/red variants for status cards); countdown
  is gold **IBM Plex Mono**.
- Tables = ink header row with cream uppercase labels, zebra rows, IBM Plex Mono numbers.
- Job Logs / Kafka count chips = **dashed** stamp pills (green gain `#2F6F62` / dark-red loss
  `#9C3B26`); buttons are pills (ink primary, gold-outline secondary/refresh).
Fonts load from Google Fonts in `index.html`; passbook `:root` tokens live in `src/styles.scss` and
the component palette is the SCSS vars at the top of `mfnav-monitor.component.scss`. A broken build
shows the old dark palette / sans title — a clear pass/fail tell in screenshots.

## Devin Secrets Needed
- None for mock-based testing. Real end-to-end would need MutualFund.NavAPI running (+ its DB / Kafka;
  connection details in `MutualFund.NavAPI/appsettings*.json`).
