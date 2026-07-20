// import { Injectable } from '@angular/core';
// import { HttpClient, HttpErrorResponse } from '@angular/common/http';
// import { Observable, throwError } from 'rxjs';
// import { catchError } from 'rxjs/operators';
// import { environment } from '../../../environments/environment';
// import { MarketHoliday } from '../models/market-holiday.model';

// @Injectable({ providedIn: 'root' })
// export class HolidaysService {
//   private base = `${environment.apiBaseUrl}/api/holidays`;

//   constructor(private http: HttpClient) {}

//   getForYear(year: number): Observable<MarketHoliday[]> {
//     return this.http.get<MarketHoliday[]>(`${this.base}/${year}`).pipe(catchError(this.handle));
//   }

//   isTradingDay(date: string): Observable<{ date: string; isTradingDay: boolean }> {
//     return this.http.get<{ date: string; isTradingDay: boolean }>(`${this.base}/is-trading-day?date=${date}`).pipe(catchError(this.handle));
//   }

//   refresh(): Observable<any> {
//     return this.http.post(`${this.base}/refresh`, {}).pipe(catchError(this.handle));
//   }

//   private handle(err: HttpErrorResponse) {
//     const message = err.error?.message ?? err.error?.error ?? err.message ?? 'Unknown error';
//     return throwError(() => ({ status: err.status, message, detail: JSON.stringify(err.error) }));
//   }
// }
