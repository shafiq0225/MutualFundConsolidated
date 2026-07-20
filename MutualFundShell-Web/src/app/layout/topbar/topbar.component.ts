import { Component, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';

import { LayoutStateService } from '../../core/services/layout-state.service';
import { AuthCookieService } from '../../core/services/auth-cookie.service';

@Component({
  selector: 'shell-topbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.scss'
})
export class TopbarComponent {
  readonly layout = inject(LayoutStateService);

  private readonly router = inject(Router);
  private readonly authCookie = inject(AuthCookieService);

  // NOTE: We deliberately do NOT read Title.getTitle() here. The Router
  // fires NavigationEnd and *then* calls titleStrategy.updateTitle() on the
  // very next line (see Router's transition handling) — so any subscriber
  // to NavigationEnd that reads the Title service synchronously always
  // sees the *previous* route's title, one navigation behind. That was the
  // cause of the "Scheme" page showing "NAV Comparison" and vice versa.
  // Reading the title straight off the resolved route snapshot avoids the
  // race entirely.
  pageTitle = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      startWith(null),
      map(() => this.resolveTitle(this.router.routerState.snapshot.root) || 'MutualFund')
    ),
    { initialValue: 'MutualFund' }
  );

  private resolveTitle(snapshot: ActivatedRouteSnapshot): string | undefined {
    let node: ActivatedRouteSnapshot | null = snapshot;
    let title: string | undefined;
    while (node) {
      title = node.title ?? title;
      node = node.firstChild;
    }
    return title;
  }

  // Recomputed on every NavigationEnd, same reasoning as pageTitle above:
  // after a successful login the shell navigates via router.navigateByUrl
  // (see LoginHostComponent), which doesn't recreate TopbarComponent (it
  // lives outside the routed outlet), so a one-time field read at
  // construction would stay stale until a hard refresh.
  user = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      startWith(null),
      map(() => this.authCookie.getUser())
    ),
    { initialValue: this.authCookie.getUser() }
  );

  menuOpen = false;

  toggleMenu(): void {
    this.menuOpen = !this.menuOpen;
  }

  closeMenu(): void {
    this.menuOpen = false;
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.menuOpen = false;
  }

  logout(): void {
    // Stay inside the shell — /login renders auth-login-element, the same
    // embedded login used for the shell's start page (see app.routes.ts /
    // LoginHostComponent). Previously this redirected out to Auth-Web's
    // own origin, which took the user out of the shell entirely.
    //
    // The cookie is shared across ports (see AuthCookieService), so the
    // shell can clear it directly rather than depending on Auth-Web to do
    // it. Known gap: this doesn't invalidate the refresh token server-side
    // (that call needs the refresh token, which only lives in Auth-Web's
    // own localStorage, not in anything the shell can read) — the access
    // cookie being gone is enough to gate the shell and every embedded
    // remote, but the refresh token itself stays valid until it expires.
    document.cookie = 'mf_access_token=; path=/; max-age=0; SameSite=Lax';
    this.menuOpen = false;
    this.router.navigateByUrl('/login');
  }
}
