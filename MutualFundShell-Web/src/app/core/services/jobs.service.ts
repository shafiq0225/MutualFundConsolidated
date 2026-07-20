// import { Injectable } from '@angular/core';
// import { HttpClient, HttpErrorResponse } from '@angular/common/http';
// import { Observable, throwError } from 'rxjs';
// import { catchError } from 'rxjs/operators';
// import { environment } from '../../../environments/environment';
// import { JobLog } from '../models/job-log.model';

// @Injectable({ providedIn: 'root' })
// export class JobsService {
//   private base = `${environment.apiBaseUrl}/api/jobs`;

//   constructor(private http: HttpClient) {}

//   getLogs(count: number = 50): Observable<JobLog[]> {
//     return this.http.get<JobLog[]>(`${this.base}/logs?count=${count}`).pipe(catchError(this.handle));
//   }

//   getLatestLog(): Observable<JobLog> {
//     return this.http.get<JobLog>(`${this.base}/logs/latest`).pipe(catchError(this.handle));
//   }

//   private handle(err: HttpErrorResponse) {
//     const message = err.error?.message ?? err.error?.error ?? err.message ?? 'Unknown error';
//     return throwError(() => ({ status: err.status, message, detail: JSON.stringify(err.error) }));
//   }
// }
