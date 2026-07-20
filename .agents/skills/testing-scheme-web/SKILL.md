---
name: testing-scheme-web
description: Test the MutualFundScheme-Web pages (NAV Comparison Report, Scheme Enrollment, Scheme Details) end-to-end. Use when verifying Scheme-Web UI changes without standing up the .NET backend.
---

# Testing MutualFundScheme-Web

Angular 20 standalone app in `MutualFundScheme-Web/`. It talks to one .NET service
(MutualFund.SchemeAPI, HTTPS `:63946`) backed by SQL Server. Standing up the real backend is
heavy, so for frontend testing prefer a **mock API** (same approach as `testing-investment-web`).

## Prereqs / environment
- Angular CLI 20 needs Node >= 20.19; base image may ship older → use Node 22:
  `export NVM_DIR="$HOME/.nvm" && . "$NVM_DIR/nvm.sh" && nvm use 22` (install with `nvm install 22`).
- `cd MutualFundScheme-Web && npm install`, then `npx ng serve` (http://localhost:4200).
- Build check: `npx ng build --configuration development`. No lint/test script is configured.

## Mock-API testing pattern (recommended for frontend-only changes)
1. Write a tiny no-dep Node `http` mock server with permissive CORS on a spare port (e.g. 4300)
   serving the endpoints the pages use (base path is `environment.apiUrl`):
   - `GET /api/holiday-status` → `HolidayStatusDto` `{ isHoliday, message }` (return `isHoliday:false`
     unless testing the holiday banner).
   - `GET /api/navcomparison/daily` and `GET /api/navcomparison?startDate&endDate`
     → `NavComparisonResponseDto` `{ startDate, endDate, message, schemes: SchemeComparisonDto[] }`.
     Each scheme has `rank` (1..3 render gold TOP badges), `fundName/schemeCode/schemeName`, and a
     `history: NavHistoryDto[]` (first entry = start NAV, last = end NAV; `isGrowth`/`percentage`
     drive the perf stamp).
   - `GET /api/navcomparison/{code}/details` → `SchemeDetailsDto` (current/previous NAV, week
     return, `oneMonth/threeMonth/sixMonth/oneYear/threeYear` `PeriodReturnDto`, and `navHistory:
     NavPointDto[]` for the sparkline — give ~40 points so charts render).
   - `GET /api/schemeenrollment` (and `/approved`) → `SchemeEnrollmentDto[]`; `POST` echoes a new row;
     `PUT /api/schemeenrollment/{code}` returns the updated row (also used by activate/deactivate).
   - `PUT /api/fundapproval/{fundCode}?isApproved=` → any 200 JSON.
   A working mock server lived at `~/mock-scheme-api/server.js` during the restyle test session —
   recreate it if gone (it is not committed).
2. Temporarily point `src/environments/environment.ts` `apiUrl` at `http://localhost:4300`.
   **Revert before finishing** (`git checkout -- src/environments/environment.ts`) — never commit it.
3. There is currently **no auth** on Scheme-Web, so no login/JWT is needed (unlike Investment-Web).

## Routes
- `/nav` — NAV Comparison Report (ledger table; rows link to details).
- `/schemes` — Scheme Enrollment (stat cards, table, Enroll/Edit modals, activate/deactivate).
- `/nav/scheme/{code}` — Scheme Details (NAV hero, Chart.js sparkline, period-return cards).

## What to verify (functionality)
- NAV: search box filters rows and the "N schemes" counter updates; date-range Search / Today reload.
- Enrollment: Enroll modal opens; empty submit is blocked with "… is required" errors (no row added);
  status filter (All/Active/Inactive) narrows the list.
- Details: hero shows current NAV + daily/week change; sparkline and 5 period cards render.

## Passbook restyle signatures (PR #4 — visual regression tells)
The theme should look like Investment-Web's Order/Investor passbook, NOT the old blue/Inter look:
- Page headers = dark **ink** (`#1B2A4A`) cover band, **Fraunces** serif title, perforated dotted
  bottom edge; body on **parchment** (`#FAF6EC`).
- Tables = ink header row, zebra rows, **IBM Plex Mono** numbers.
- Status = **dashed** stamp pills (green gain `#2F6F62` / dark-red loss `#9C3B26`).
- Stat cards have a **gold** (`#C08A2E`) left border; NAV rank badges are gold TOP 1/2/3.
- Scheme Details charts use gain `#2F6F62` / loss `#9C3B26` (not bright `#22c55e`/blue).
Fonts load from Google Fonts in `index.html`; tokens live in `src/styles/_variables.scss` +
`src/styles.scss` (`:root`). Most restyling flows from these tokens, so a broken build shows the
old palette/sans font — a clear pass/fail tell in screenshots.

## Devin Secrets Needed
- None for mock-based testing. Real end-to-end would need SQL Server credentials + MutualFund.SchemeAPI
  running (connection string in `MutualFund.SchemeAPI/appsettings*.json`).
