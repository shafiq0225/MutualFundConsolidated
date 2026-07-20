import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface LoginDto {
  email: string;
  password: string;
}

export interface TokenResponseDto {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface DecodedTokenClaims {
  unique_name?: string;
  name?: string;
  email?: string;
  role?: string;
  permissions?: string | string[];
  exp: number;
  firstName?: string;
  lastName?: string;
  sub?: string;
}

const COOKIE_NAME = 'mf_access_token';
const COOKIE_MAX_AGE_SECONDS = 60 * 60 * 8; // 8h

function setCookie(name: string, value: string, maxAgeSeconds: number): void {
  document.cookie = `${name}=${encodeURIComponent(value)}; path=/; max-age=${maxAgeSeconds}; SameSite=Lax`;
}

function clearCookie(name: string): void {
  document.cookie = `${name}=; path=/; max-age=0; SameSite=Lax`;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authApi = `${environment.apiUrl}/api/auth`;
  private readonly tokenKey = 'access_token';
  private readonly refreshTokenKey = 'refresh_token';
  
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  private currentUserSubject = new BehaviorSubject<DecodedTokenClaims | null>(null);
  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    this.checkAuthStatus();
    this.currentUserSubject.next(this.decodeStoredToken());
  }

  login(dto: LoginDto): Observable<TokenResponseDto> {
    return this.http.post<TokenResponseDto>(`${this.authApi}/login`, dto).pipe(
      tap(response => {
        this.setTokens(response.accessToken, response.refreshToken, response.expiresIn);
        this.isAuthenticatedSubject.next(true);
        this.currentUserSubject.next(this.decodeToken(response.accessToken));
      })
    );
  }

  logout(): Observable<{ message: string }> {
    const refreshToken = this.getRefreshToken();
    return this.http.post<{ message: string }>(`${this.authApi}/logout`, { refreshToken }).pipe(
      tap(() => {
        this.clearTokens();
        this.isAuthenticatedSubject.next(false);
        this.currentUserSubject.next(null);
      })
    );
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.refreshTokenKey);
  }

  private setTokens(accessToken: string, refreshToken: string, expiresIn?: number): void {
    localStorage.setItem(this.tokenKey, accessToken);
    localStorage.setItem(this.refreshTokenKey, refreshToken);
    setCookie(COOKIE_NAME, accessToken, expiresIn && expiresIn > 0 ? expiresIn : COOKIE_MAX_AGE_SECONDS);
  }

  private clearTokens(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.refreshTokenKey);
    clearCookie(COOKIE_NAME);
  }

  private checkAuthStatus(): void {
    this.isAuthenticatedSubject.next(this.hasValidToken());
  }

  private hasValidToken(): boolean {
    const token = this.getAccessToken();
    if (!token) return false;

    const expiry = this.getTokenExpiry(token);
    if (expiry === null || expiry * 1000 <= Date.now()) {
      this.clearTokens();
      return false;
    }
    return true;
  }

  private getTokenExpiry(token: string): number | null {
    const claims = this.decodeToken(token);
    return claims ? claims.exp : null;
  }

  isLoggedIn(): boolean {
    return this.hasValidToken();
  }

  isAuthenticated(): boolean {
    return this.hasValidToken();
  }

  getCurrentUser(): DecodedTokenClaims | null {
    return this.currentUserSubject.getValue();
  }

  isAdmin(): boolean {
    return this.getCurrentUser()?.role === 'Admin';
  }

  isEmployee(): boolean {
    return this.getCurrentUser()?.role === 'Employee';
  }

  hasPermission(code: string): boolean {
    const permissions = this.getCurrentUser()?.permissions;
    if (!permissions) return false;
    return Array.isArray(permissions) ? permissions.includes(code) : permissions === code;
  }

  canManage(permissionCode: string): boolean {
    return this.isAdmin() || this.hasPermission(permissionCode);
  }

  canAddOrders(): boolean {
    return this.isAdmin() || (this.isEmployee() && this.hasPermission('order.view') && this.hasPermission('order.add'));
  }

  canRunSnapshot(): boolean {
    return this.isAdmin() || (this.isEmployee() && this.hasPermission('investor.view') && this.hasPermission('investor.snapshot'));
  }

  private decodeStoredToken(): DecodedTokenClaims | null {
    const token = this.getAccessToken();
    if (!token) return null;
    const claims = this.decodeToken(token);
    if (claims && claims.exp * 1000 <= Date.now()) {
      this.clearTokens();
      return null;
    }
    return claims;
  }

  /** @deprecated Use getCurrentUser() instead */
  currentUser(): DecodedTokenClaims | null {
    return this.getCurrentUser();
  }

  private decodeToken(token: string): DecodedTokenClaims | null {
    try {
      const payload = token.split('.')[1];
      if (!payload) return null;
      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const json = atob(normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '='));
      return JSON.parse(json) as DecodedTokenClaims;
    } catch {
      return null;
    }
  }
}
