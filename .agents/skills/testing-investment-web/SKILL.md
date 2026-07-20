---
name: testing-investment-web
description: Test the MutualFundInvestment-Web investor (Passbook) page end-to-end. Use when verifying investor/family-portfolio UI changes without standing up the full .NET backend.
---

# Testing MutualFundInvestment-Web (Investor Passbook)

The app is an Angular 20 standalone app in `MutualFundInvestment-Web/`. It talks to three
.NET services (Investment `:7003`, Scheme `:63946`, Auth `:7001`) each backed by SQL Server.
Standing up the real backend is heavy (4 solutions + SQL Server + seeded NAV/holdings), so for
frontend testing prefer a **mock API**.

## Prereqs / environment
- Angular CLI 20 needs Node >= 20.19. Base image may ship 20.18 → use Node 22:
  `export NVM_DIR="$HOME/.nvm" && . "$NVM_DIR/nvm.sh" && nvm use 22` (install with `nvm install 22`).
- `cd MutualFundInvestment-Web && npm install`, then `npx ng serve` (http://localhost:4200).
- Build check: `npx ng build --configuration development`. No lint/test script is configured.

## Mock-API testing pattern (recommended for frontend-only changes)
1. Write a tiny Node `http` mock server (no deps) with permissive CORS on one port (e.g. 4300)
   serving the endpoints the page uses:
   - `GET /api/familyportfolio` → `FamilyOverviewDto` (has `members: MemberSummaryDto[]` with
     per-period `yesterday/thisWeek/oneMonth/oneYear` `QuickReturnDto`s).
   - `GET /api/familyportfolio/{userId}` → `MemberHoldingsDto` (`holdings: HoldingCardDto[]`,
     one row per SIP order, each with period returns).
   - `GET /api/portfolio/holdings` → `HoldingDto[]` (raw orders; drives the ledger drill-down).
   - `POST /api/jobs/snapshot` → any 200 JSON (Job Snapshot button).
   - `GET /api/family` → `AuthFamilyGroupDto[]` (Auth API; supplies relationship labels).
   - `POST /api/auth/login` → `TokenResponseDto` — **accessToken must be a real (unsigned) JWT**
     `header.payload.signature` where payload has a future `exp` and `sub/role`. The auth guard
     only base64-decodes the token and checks `exp` (no signature check), so an unsigned JWT works.
2. Temporarily point `src/environments/environment.ts` `investmentApiUrl`/`schemeApiUrl`/`authApiUrl`
   all at `http://localhost:4300`. **Revert this before finishing** (`git checkout -- ...`) — never commit it.
3. Log in via the real `/login` form (any email/password) → app stores the mock JWT → navigate to
   `/investor`. `QuickReturnDto` uses `periodGainAmount` (₹) + `returnPercent` (%); the P&L donut reads `periodGainAmount`.

## Data shaping to exercise the known bugs
- Give one member ≥2 SIP orders in the **same** scheme → proves cards dedupe by `schemeCode`
  (member view should show 1 card per scheme, ledger should still show every order row).
- Include a losing scheme (negative gains) to check gain/loss colouring and donut arcs.
- Family aggregate must render owner-tagged cards (regression: previously "No schemes held").

## What to verify (UI)
- Family view lists all schemes; member view collapses duplicate orders into one card.
- "Returns by Period" & "P&L by Period" donuts render arcs + 4 legend rows at cover AND per card.
- "↻ Run Snapshot" fires one `POST /api/jobs/snapshot` (grep the mock log) + success toast.
- Clicking a card opens the ledger with per-order rows + Total footer.

## Orders (Passbook ledger) page — `/orders`
Same app/mock pattern. The Orders page uses these mock endpoints (add to the same mock server):
- `GET /api/orders` (or the path `OrderService` calls) → `InvestmentOrderDto[]` — investor, scheme,
  amount, paymentMode, orderDate, status (`Requested|Assigned|Submitted|Verified|Active`), purchaseNAV,
  units, folioNumber. Seed multiple investors + a scheme with several folios.
- `POST /api/orders` → echo back the created order with a new `orderNumber` and status `Requested`.
- stage-advance endpoints for Assign/Submit/Verify → return the updated order.

Cover layout: investor title `<select>` on its own line; a `.controls-row` beneath holds Scheme +
Order Date Range + "+ New order" on one line (regression: these must NOT wrap onto separate lines).

### New Order modal — gotchas worth testing
- The Folio field switches to a **native `<select>`** once investor+scheme are chosen. A native
  select's initial `[value]` binding fires **no change event**, so if the component doesn't proactively
  `setValue` a default folio, `folioNumber` stays empty and the form is silently invalid → "Log order"
  appears broken. Verify: after picking investor+scheme, folio shows an existing folio pre-selected.
- Verify invalid submit gives feedback (a toast naming missing fields), not a silent no-op.
- Happy path: investor+scheme → folio auto-fills; enter Amount + Purchase NAV → "Units to be allotted"
  = amount/nav; "Log order" → success toast, modal closes, new Requested row prepended, Total Orders +1.
- Drawer (⋮): action card advances Requested→Assigned→Submitted→Verified; each step updates the
  vertical timeline, the row status stamp, and swaps the action card to the next step.

## Devin Secrets Needed
- None for mock-based testing. Real end-to-end would need SQL Server credentials + the three API
  services running (connection strings live in each `*.API/appsettings*.json`).
