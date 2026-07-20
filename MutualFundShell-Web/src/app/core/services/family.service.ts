import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AddFamilyMemberDto, CreateFamilyGroupDto, FamilyGroupDto } from '../models/family.model';

@Injectable({ providedIn: 'root' })
export class FamilyService {
  private readonly api = `${environment.authApiUrl}/api/family`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<FamilyGroupDto[]> {
    return this.http.get<FamilyGroupDto[]>(this.api);
  }

  getById(groupId: number): Observable<FamilyGroupDto> {
    return this.http.get<FamilyGroupDto>(`${this.api}/${groupId}`);
  }

  create(dto: CreateFamilyGroupDto): Observable<FamilyGroupDto> {
    return this.http.post<FamilyGroupDto>(this.api, dto);
  }

  addMember(groupId: number, dto: AddFamilyMemberDto): Observable<FamilyGroupDto> {
    return this.http.post<FamilyGroupDto>(`${this.api}/${groupId}/members`, dto);
  }

  removeMember(groupId: number, userId: string): Observable<unknown> {
    return this.http.delete(`${this.api}/${groupId}/members/${userId}`);
  }
}
