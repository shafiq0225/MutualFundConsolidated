import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AssignPermissionDto, PermissionDto, UserPermissionDto } from '../models/permission.model';

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly api = `${environment.authApiUrl}/api/permissions`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<PermissionDto[]> {
    return this.http.get<PermissionDto[]>(this.api);
  }

  getUserPermissions(userId: string): Observable<UserPermissionDto> {
    return this.http.get<UserPermissionDto>(`${this.api}/user/${userId}`);
  }

  assign(dto: AssignPermissionDto): Observable<unknown> {
    return this.http.post(`${this.api}/assign`, dto);
  }

  revoke(dto: AssignPermissionDto): Observable<unknown> {
    return this.http.delete(`${this.api}/revoke`, { body: dto });
  }
}
