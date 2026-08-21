import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { AuthCookieService } from '../services/auth-cookie.service';
import { environment } from '../../../environments/environment';
import { catchError, switchMap, throwError, BehaviorSubject, filter, take, Observable } from 'rxjs';

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const authCookie = inject(AuthCookieService);

  const token = authCookie.getToken() || authService.getAccessToken();
  const isGatewayRequest = req.url.startsWith(environment.apiUrl);
  const isAuthEndpoint = req.url.includes('/api/auth/login') ||
                         req.url.includes('/api/auth/register') ||
                         req.url.includes('/api/auth/refresh') ||
                         req.url.includes('/api/auth/demo-login');
  const isRetry = req.headers.has('X-Token-Retry');

  let authReq = req;
  if (token && isGatewayRequest && !isAuthEndpoint) {
    authReq = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }

  return next(authReq).pipe(
    catchError((error) => {
      if (
        error instanceof HttpErrorResponse && 
        error.status === 401 && 
        isGatewayRequest && 
        !isAuthEndpoint && 
        !isRetry
      ) {
        return handle401Error(authReq, next, authService);
      }
      return throwError(() => error);
    })
  );
};

function handle401Error(
  req: HttpRequest<unknown>, 
  next: HttpHandlerFn, 
  authService: AuthService
): Observable<HttpEvent<unknown>> {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    const refreshToken = authService.getRefreshToken();
    if (!refreshToken) {
      isRefreshing = false;
      document.cookie = 'mf_access_token=; path=/; max-age=0; SameSite=Lax';
      window.location.href = '/login';
      return throwError(() => new Error('No refresh token available'));
    }

    return authService.refreshToken().pipe(
      switchMap((res) => {
        isRefreshing = false;
        refreshTokenSubject.next(res.accessToken);

        return next(req.clone({
          setHeaders: { 
            Authorization: `Bearer ${res.accessToken}`,
            'X-Token-Retry': 'true'
          }
        }));
      }),
      catchError((err) => {
        isRefreshing = false;
        refreshTokenSubject.next(null);
        
        // Clear tokens and redirect only when the refresh token is genuinely invalid/expired
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        document.cookie = 'mf_access_token=; path=/; max-age=0; SameSite=Lax';

        window.location.href = '/login';
        return throwError(() => err);
      })
    );
  } else {
    return refreshTokenSubject.pipe(
      filter((token) => token !== null),
      take(1),
      switchMap((token) => {
        return next(req.clone({
          setHeaders: { 
            Authorization: `Bearer ${token}`,
            'X-Token-Retry': 'true'
          }
        }));
      })
    );
  }
}
