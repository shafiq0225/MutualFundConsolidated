import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthFamilyGroupDto, AuthFamilyMemberDto } from '../models/auth-family.model';

/**
 * Calls MutualFund.Auth.API directly — no gateway yet.
 * Used purely to fetch relationship labels ("Self"/"Spouse"/"Mother"/
 * etc.) so the Angular layer can join them onto Investment's
 * financial data by UserId. Per decision: this join happens
 * client-side for now; the Gateway will own this later.
 */
@Injectable({ providedIn: 'root' })
export class AuthFamilyService {
  private readonly api = `${environment.authApiUrl}/api/family`;

  constructor(private http: HttpClient) {}

  getAllGroups(): Observable<AuthFamilyGroupDto[]> {
    return this.http.get<AuthFamilyGroupDto[]>(this.api);
  }

  getGroup(groupId: number): Observable<AuthFamilyGroupDto> {
    return this.http.get<AuthFamilyGroupDto>(`${this.api}/${groupId}`);
  }

  /**
   * Finds the family group containing the given userId (as Head or
   * dependent) and returns a lookup map of userId -> relationship info.
   * There's no "by userId" endpoint on AuthAPI yet, so this fetches
   * all groups and searches client-side.
   */
  getRelationshipMapForUser(
    anyUserIdInFamily: string
  ): Observable<Map<string, AuthFamilyMemberDto>> {
    return this.getAllGroups().pipe(
      map(groups => {
        const group = groups.find(g =>
          g.allMembers.some(m => m.userId === anyUserIdInFamily));

        const map = new Map<string, AuthFamilyMemberDto>();
        (group?.allMembers ?? []).forEach(m => map.set(m.userId, m));
        return map;
      })
    );
  }
}
