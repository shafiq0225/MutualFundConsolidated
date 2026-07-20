import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

// ── Models ────────────────────────────────────────────────────────────────

export interface NavFileSummary {
    id: number;
    navDate: string;
    fileSizeBytes: number;
    recordCount: number;
    checksum: string;
    downloadedAt: string;
    isHoliday: boolean;
}

export interface TriggerResult {
    date: string;
    wasStored: boolean;
    message?: string;
}

export interface TargetDateResult { targetDate: string; }
export interface LatestNavResult { latestNavDate: string; }

export interface HolidayTodayResult {
    date: string;
    status: string;
    isHoliday: boolean;
}

export interface TradingDayResult {
    date: string;
    isTradingDay: boolean;
    dayOfWeek: string;
}

export interface JobLog {
    id: number;
    jobName: string;
    startedAt: string;
    completedAt: string | null;
    isSuccess: boolean;
    errorMessage: string | null;
    details: string | null;
    elapsedSeconds: number;
}

export interface KafkaPublishLog {
    id: number;
    topic: string;
    eventType: string;
    messageKey: string;
    messageSizeBytes: number;
    isSuccess: boolean;
    errorMessage: string | null;
    publishedAt: string;
    elapsedMs: number;
    triggerSource: string;
    navDate: string | null;
    partition: number;
    offset: number;
    createdAt?: string;
    updatedAt?: string;
}

// ── Service ───────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class MfNavService {
    private nav = `${environment.apiBaseUrl}/api/nav`;
    private jobs = `${environment.apiBaseUrl}/api/jobs`;
    private holidays = `${environment.apiBaseUrl}/api/Holidays`;
    private kafka = `${environment.apiBaseUrl}/api/kafka`;

    constructor(private http: HttpClient) { }

    // NAV
    getLatest(): Observable<LatestNavResult> { return this.http.get<LatestNavResult>(`${this.nav}/latest`).pipe(catchError(this.handle)); }
    getTargetDate(): Observable<TargetDateResult> { return this.http.get<TargetDateResult>(`${this.nav}/target-date`).pipe(catchError(this.handle)); }
    getHistory(): Observable<NavFileSummary[]> { return this.http.get<NavFileSummary[]>(`${this.nav}/history`).pipe(catchError(this.handle)); }
    trigger(): Observable<TriggerResult> { return this.http.post<TriggerResult>(`${this.nav}/trigger`, {}).pipe(catchError(this.handle)); }
    triggerForDate(d: string): Observable<TriggerResult> { return this.http.post<TriggerResult>(`${this.nav}/trigger/${d}`, {}).pipe(catchError(this.handle)); }

    // Jobs
    getLogs(count = 50): Observable<JobLog[]> { return this.http.get<JobLog[]>(`${this.jobs}/logs?count=${count}`).pipe(catchError(this.handle)); }
    getLatestLog(): Observable<JobLog> { return this.http.get<JobLog>(`${this.jobs}/logs/latest`).pipe(catchError(this.handle)); }

    // Holidays
    getHolidayToday(): Observable<HolidayTodayResult> { return this.http.get<HolidayTodayResult>(`${this.holidays}/today`).pipe(catchError(this.handle)); }
    checkTradingDay(date: string): Observable<TradingDayResult> { return this.http.get<TradingDayResult>(`${this.holidays}/is-trading-day?date=${date}`).pipe(catchError(this.handle)); }

    // Kafka
    getKafkaLogs(count = 50): Observable<KafkaPublishLog[]> { return this.http.get<KafkaPublishLog[]>(`${this.kafka}/logs?count=${count}`).pipe(catchError(this.handle)); }

    private handle(err: HttpErrorResponse) {
        const message = err.error?.message ?? err.error?.error ?? err.message ?? 'Unknown error';
        return throwError(() => ({ status: err.status, message, detail: JSON.stringify(err.error) }));
    }
}