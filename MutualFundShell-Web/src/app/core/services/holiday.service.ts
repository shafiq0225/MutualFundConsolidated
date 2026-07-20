import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { environment } from '../../../environments/environment';
import { HolidayStatusDto } from '../models/holiday-status.model';

@Injectable({ providedIn: 'root' })
export class HolidayService {
  private readonly api = `${environment.apiUrl}/api/holiday-status`;

  // Cached for the session — cleared on logout
  private cache$: Observable<HolidayStatusDto> | null = null;

  constructor(private http: HttpClient) {}

  getStatus(): Observable<HolidayStatusDto> {
    if (!this.cache$) {
      this.cache$ = this.http
        .get<HolidayStatusDto>(this.api)
        .pipe(shareReplay(1));
    }
    return this.cache$;
  }

  clearCache(): void {
    this.cache$ = null;
  }
}
