---
name: testing-auth-web
description: Test the MutualFundAuth-Web pages (Login, Register, Users, Pending Approvals, Family Groups) end-to-end. Use when verifying Auth-Web UI changes without standing up the .NET AuthAPI.
---

# Testing MutualFundAuth-Web

Angular 20 standalone app in `MutualFundAuth-Web/`. It talks to MutualFund.AuthAPI (base path is
`environment.authApiUrl`, default HTTPS `:7001`) backed by SQL Server. Standing up the real backend
is heavy, so for frontend testing prefer a **mock API** (same approach as `testing-scheme-web`).

## Prereqs / environment
- Angular CLI 20 needs Node >= 20.19; base image may ship older → use Node 22:
  `export NVM_DIR="$HOME/.nvm" && . "$NVM_DIR/nvm.sh" && nvm use 22` (install with `nvm install 22`).
- `cd MutualFundAuth-Web && npm install`, then `npx ng serve` (http://localhost:4200).
- Build check: `npx ng build --configuration development`. No lint/test script is configured.

## Auth model (important — differs from Scheme-Web)
- Protected routes use `authGuard`, which only checks `authService.isLoggedIn()` — i.e. presence of an
  `access_token` in localStorage. It does NOT validate the JWT signature.
- `authInterceptor` attaches `Authorization: Bearer <access_token>` if present.
- Easiest way past the guard: just **log in through the Login page** against the mock (mock returns a
  token → app stores `access_token`/`refresh_token` in localStorage → guard passes). No need to
  hand-seed localStorage or bypass the guard.
- Login: `POST /api/auth/login {email,password}` → `{ accessToken, refreshToken, expiresIn }`. The
  Sign In button stays disabled until both fields are valid (password min length 6).

## Mock-API testing pattern (recommended for frontend-only changes)
1. Write a tiny no-dep Node `http` mock with permissive CORS on the port `authApiUrl` points to.
   A working server lived at `~/mock-auth-api/server.js` during header-restyle testing — recreate if
   gone (not committed). Endpoints the pages use:
   - `POST /api/auth/login` → token DTO; `POST /api/auth/logout`; `POST /api/auth/register` →
     `{ id, fullName, email, status, message }`.
   - `GET /api/users` → `UserDto[]`; `GET /api/users/pending` → pending subset.
     `UserDto`: id, firstName/lastName/fullName, email, panNumber, role (num), roleName, userType,
     userTypeName, approvalStatus (num), statusName (Approved/Pending/Rejected), isActive, createdAt,
     approvedAt, lastLoginAt, rejectionReason. Include a mix of statuses so the 4 stat cards + role/
     status badges render.
   - `PUT /api/users/{id}/approve|reject|role` → echo the updated user.
   - `GET /api/family` → `FamilyGroupDto[]` (id, groupName, headUserName/Email/Pan, memberCount,
     members[], allMembers[], isActive); `POST /api/family`, `POST/DELETE .../members`.
   - `GET /api/permissions` → `PermissionDto[]`; `/permissions/user/{id}`, assign/revoke.
2. Temporarily point `src/environments/environment.ts` `authApiUrl` at the mock (e.g.
   `http://localhost:7005`). **Revert before finishing** (`git checkout -- src/environments/environment.ts`)
   — never commit it.

## Routes
- `/login`, `/register` — public.
- `/users` — All Users (stat cards, ledger table, search + role/status filters, row actions).
- `/users/pending` — Pending Approvals ("Approve All" header button, per-row approve/reject).
- `/family` — Family Groups (list of gold-left-border cards, "New Group" header button + modal).

## What to verify (functionality)
- Login: empty submit shows "Email is required"/"Password is required" and Sign In disabled; valid
  creds → "Login successful" toast → navigates to `/users` (proves guard).
- Users: typing in search narrows the ledger table; role/status selects filter; Clear resets.
- Pending: table lists pending users; Approve/Reject per row.
- Family: "New Group" opens a passbook modal (Group Name + Head-of-family select).

## Passbook cover-band header signatures (PR #6 — visual regression tells)
Auth-Web was already passbook-themed (ink/parchment/gold tokens, Fraunces + IBM Plex fonts, dark
ledger tables, dashed stamp badges, gold-left-border cards). PR #6 only changed `.page-head` (global
`src/styles.scss`) from a flat parchment header to the **ink cover band**:
- Header block is a dark **ink** (`#1B2A4A`) band with `border-radius`, cream text, an uppercase
  steel-blue **eyebrow** (`<span class="eyebrow">`) above the title, a **Fraunces** serif `h1`, and a
  radial-gradient **perforated dotted bottom edge** (`::after`).
- In-header action buttons (`.btn-primary` inside `.page-head`, e.g. "Approve All", "New Group")
  render as **gold-outlined pills** (transparent fill, gold border/text) — not solid ink.
A broken change shows the OLD flat dark-on-parchment header with no ink band / eyebrow / perforation,
which is a clear pass/fail tell in screenshots.

## Devin Secrets Needed
- None for mock-based testing. Real end-to-end would need SQL Server credentials + MutualFund.AuthAPI
  running (connection string in `MutualFund.AuthAPI/appsettings*.json`).
