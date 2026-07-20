import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NavComparisonResponseDto } from '../models/nav.model';

@Injectable({ providedIn: 'root' })
export class NavService {
    private readonly api = `${environment.apiUrl}/api/navcomparison`;

    constructor(private http: HttpClient) { }

    getDaily(): Observable<NavComparisonResponseDto> {
        return this.http.get<NavComparisonResponseDto>(`${this.api}/daily`);
    }

    getByDateRange(startDate: string, endDate: string): Observable<NavComparisonResponseDto> {
        const params = new HttpParams()
            .set('startDate', startDate)
            .set('endDate', endDate);
        return this.http.get<NavComparisonResponseDto>(this.api, { params });
    }
}
