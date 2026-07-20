import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { LayoutStateService } from '../../core/services/layout-state.service';
import { AuthCookieService } from '../../core/services/auth-cookie.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  /** Shows a small "Soon" badge — the route still navigates to a placeholder page. */
  soon: boolean;
  /**
   * Permission code an Employee needs to see this item; Admin always
   * passes. Omit entirely for items open to every role (e.g. NAV
   * Comparison, which is AllRoles at the API too — see
   * NavComparisonController).
   */
  requiresPermission?: string;
}

@Component({
  selector: 'shell-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  readonly layout = inject(LayoutStateService);
  private readonly authCookie = inject(AuthCookieService);
  private readonly router = inject(Router);

  private readonly allNavItems: NavItem[] = [
    { label: 'Dashboard', icon: 'fa-gauge-high', route: '/dashboard', soon: true },
    { label: 'User', icon: 'fa-user', route: '/user', soon: false, requiresPermission: 'user.manage' },
    { label: 'Pending Approvals', icon: 'fa-hourglass-half', route: '/pending-approvals', soon: false, requiresPermission: 'user.manage' },
    { label: 'Family Groups', icon: 'fa-people-roof', route: '/family-groups', soon: false, requiresPermission: 'family.manage' },
    { label: 'Scheme', icon: 'fa-list-check', route: '/scheme', soon: false, requiresPermission: 'scheme.manage' },
    { label: 'NAV Comparison', icon: 'fa-chart-line', route: '/nav-comparison', soon: false },
    { label: 'Orders', icon: 'fa-receipt', route: '/orders', soon: false, requiresPermission: 'order.view' },
    { label: 'Portfolio', icon: 'fa-wallet', route: '/portfolio', soon: false, requiresPermission: 'investor.view' }
  ];

  // Recomputed on every NavigationEnd — same reasoning as TopbarComponent's
  // `user` signal: a plain one-time field read wouldn't reflect login (or
  // an Admin granting/revoking scheme.manage) without a hard refresh.
  navItems = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      startWith(null),
      map(() => this.visibleNavItems())
    ),
    { initialValue: this.visibleNavItems() }
  );

  private visibleNavItems(): NavItem[] {
    return this.allNavItems.filter(
      (item) => !item.requiresPermission || this.authCookie.canManage(item.requiresPermission)
    );
  }
}