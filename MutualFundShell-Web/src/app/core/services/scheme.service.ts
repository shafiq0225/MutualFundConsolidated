import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
    SchemeEnrollmentDto,
    CreateSchemeEnrollmentDto,
    UpdateSchemeEnrollmentDto
} from '../models/scheme.model';

@Injectable({ providedIn: 'root' })
export class SchemeService {
    private readonly api = `${environment.apiUrl}/api/schemeenrollment`;

    constructor(private http: HttpClient) { }

    getAll(): Observable<SchemeEnrollmentDto[]> {
        return this.http.get<SchemeEnrollmentDto[]>(this.api);
    }

    getApproved(): Observable<SchemeEnrollmentDto[]> {
        return this.http.get<SchemeEnrollmentDto[]>(`${this.api}/approved`);
    }

    getByCode(schemeCode: string): Observable<SchemeEnrollmentDto> {
        return this.http.get<SchemeEnrollmentDto>(`${this.api}/${schemeCode}`);
    }

    create(dto: CreateSchemeEnrollmentDto): Observable<SchemeEnrollmentDto> {
        return this.http.post<SchemeEnrollmentDto>(this.api, dto);
    }

    update(schemeCode: string, dto: UpdateSchemeEnrollmentDto): Observable<SchemeEnrollmentDto> {
        return this.http.put<SchemeEnrollmentDto>(`${this.api}/${schemeCode}`, dto);
    }

    updateFundApproval(fundCode: string, isApproved: boolean): Observable<any> {
        return this.http.put(
            `${environment.apiUrl}/api/fundapproval/${fundCode}`,
            null,
            { params: { isApproved: isApproved.toString() } }
        );
    }
}
