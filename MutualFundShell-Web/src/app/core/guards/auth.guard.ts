import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthCookieService } from '../services/auth-cookie.service';

// Mirrors MutualFundAuth-Web's own auth.guard.ts, but checks the shared
// mf_access_token cookie (via AuthCookieService) instead of AuthService,
// since the shell has no AuthService of its own — the cookie is the only
// thing it shares with Auth-Web.
export const authGuard: CanActivateFn = (route, state) => {
  const authCookie = inject(AuthCookieService);
  const router = inject(Router);

  if (authCookie.getUser()) {
    return true;
  }

  router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
  return false;
};

/**
 * Gates a route behind a specific permission (Admin always passes — see
 * AuthCookieService.canManage). Hiding the sidebar link isn't enough on
 * its own since a user can still type the URL directly; without this,
 * they'd land on a page where every API call 401s instead of getting a
 * clear redirect. Apply *in addition to* authGuard, e.g.:
 *   canActivate: [authGuard, requiresPermission('scheme.manage')]
 */
export function requiresPermission(code: string): CanActivateFn {
  return (route, state) => {
    const authCookie = inject(AuthCookieService);
    const router = inject(Router);

    if (authCookie.canManage(code)) {
      return true;
    }

    router.navigate(['/dashboard']);
    return false;
  };
}
