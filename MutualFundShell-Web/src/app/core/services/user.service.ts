import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RejectUserDto, UpdateRoleDto, UserDto } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly api = `${environment.authApiUrl}/api/users`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(this.api);
  }

  getById(userId: string): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.api}/${userId}`);
  }

  getPending(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(`${this.api}/pending`);
  }

  getMyProfile(): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.api}/me`);
  }

  approve(userId: string): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.api}/${userId}/approve`, {});
  }

  reject(userId: string, dto: RejectUserDto): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.api}/${userId}/reject`, dto);
  }

  updateRole(userId: string, dto: UpdateRoleDto): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.api}/${userId}/role`, dto);
  }
}
