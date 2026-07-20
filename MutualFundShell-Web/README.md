# MutualFundShell-Web

Shell (container) Angular 20 app — top bar + sidebar + router-outlet — that
hosts the other micro frontends. Runs on **port 4200**, does not change any
other app's port or the gateway config.

## What's wired up now

| Sidebar item      | Route                | Status                                             |
|--------------------|-----------------------|-----------------------------------------------------|
| Dashboard          | `/dashboard`          | Placeholder                                          |
| User               | `/user`               | Placeholder                                          |
| Pending Approvals  | `/pending-approvals`  | Placeholder                                          |
| Family Groups      | `/family-groups`      | Placeholder                                          |
| Scheme             | `/scheme`              | **Functional** — mounts `<scheme-list-element>`      |
| NAV Comparison     | `/nav-comparison`      | **Functional** — mounts `<scheme-nav-element>`       |
| Orders             | `/orders`              | Placeholder                                          |
| Portfolio          | `/portfolio`           | Placeholder                                          |

## How the Scheme/NAV integration works

1. `core/config/remote.config.ts` lists each remote's origin and where its
   custom-elements bundle lives (`scheme` → `http://localhost:4205/elements/main.js`).
2. `core/services/webcomponent-loader.service.ts` injects that `<script type="module">`
   tag into `<head>` the first time a route needing it is visited, and caches
   the promise so it only loads once.
3. `features/scheme-host/scheme-list-host.component.ts` and
   `scheme-nav-host.component.ts` wait on that promise, then render
   `<scheme-list-element>` / `<scheme-nav-element>` with `CUSTOM_ELEMENTS_SCHEMA`.

This is the Web Components (custom elements) approach agreed for the initial
phase — Module Federation / Native Federation stays a later-phase option for
apps that don't already exist as standalone SPAs.

## Local dev setup

```bash
npm install
npm start          # http://localhost:4200
```

For `/scheme` and `/nav-comparison` to actually render content, MutualFundScheme-Web
needs its custom-elements bundle built at least once (see that project's README
section below — `npm run build:elements`). Re-run `npm run watch:elements` there
while developing so `public/elements/main.js` stays fresh; `ng serve` on 4205
picks it up as a static asset automatically because it lands under `public/`.

## Design system

Sidebar/topbar/placeholder styling reuses the exact passbook tokens from
MutualFundScheme-Web (`src/styles/_variables.scss` copied verbatim, plus the
same `:root` CSS custom properties in `src/styles.scss`) — navy/parchment/gold,
Fraunces display headings, IBM Plex Sans/Mono body text — so the shell and
every embedded micro frontend read as one application.

## Open item

The plan's one open confirmation — should MutualFundScheme-Web work standalone
*and* as a Web Component host — was answered **both**. See
`MutualFundScheme-Web/src/main.elements.ts` for how that's implemented
(dual entry points, one project, no behavior change to `ng serve` on 4205).
