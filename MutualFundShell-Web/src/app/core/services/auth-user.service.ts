import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthUserDto } from '../models/auth-user.model';

/**
 * Calls MutualFund.Auth.API directly — no gateway yet.
 * Source of truth for "who can be an investor" — replaces the old
 * approach of deriving investors from existing orders (which was
 * empty on a fresh DB with no orders yet).
 */
@Injectable({ providedIn: 'root' })
export class AuthUserService {
  private readonly api = `${environment.authApiUrl}/api/users`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<AuthUserDto[]> {
    return this.http.get<AuthUserDto[]>(this.api);
  }

  /** Approved, active "User" role accounts — valid investors for orders. */
  getInvestors(): Observable<AuthUserDto[]> {
    return this.getAll().pipe(
      map(users => users.filter(u =>
        u.roleName === 'User' &&
        u.statusName === 'Approved' &&
        u.isActive))
    );
  }
}
