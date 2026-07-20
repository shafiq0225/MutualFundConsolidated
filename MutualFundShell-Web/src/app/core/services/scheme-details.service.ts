import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SchemeDetailsDto } from '../models/scheme-details.model';

@Injectable({ providedIn: 'root' })
export class SchemeDetailsService {
    private readonly api = `${environment.apiUrl}/api/navcomparison`;

    constructor(private http: HttpClient) { }

    getSchemeDetails(schemeCode: string): Observable<SchemeDetailsDto> {
        return this.http.get<SchemeDetailsDto>(
            `${this.api}/${schemeCode}/details`
        );
    }
}
