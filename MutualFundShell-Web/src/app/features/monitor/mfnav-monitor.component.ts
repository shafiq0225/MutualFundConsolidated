import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { interval, Subscription, forkJoin, of, Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { JobLog, KafkaPublishLog, HolidayTodayResult, LatestNavResult, MfNavService, NavFileSummary, TargetDateResult, TradingDayResult } from 'src/app/core/services/mfnav.service';

@Component({
    selector: 'app-mfnav-monitor',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './mfnav-monitor.component.html',
    styleUrls: ['./mfnav-monitor.component.scss']
})
export class MfNavMonitorComponent implements OnInit, OnDestroy {

    // ── State ──────────────────────────────────────────────────────────────

    loading = true;
    globalError: string | null = null;

    // Dashboard
    latestNavDate: string | null = null;
    targetDate: string | null = null;
    latestJob: JobLog | null = null;
    countdown = '';
    triggering = false;
    triggerResult: { success: boolean; message: string } | null = null;
    customTriggerDate = '';

    // NAV History
    navHistory: NavFileSummary[] = [];
    navFiltered: NavFileSummary[] = [];
    navSearch = '';
    navSortCol = 'navDate';
    navSortDir = -1;
    navError: string | null = null;

    // Job Logs
    logs: JobLog[] = [];
    logsCount = 50;
    expandedLogId: number | null = null;
    logsError: string | null = null;

    // Holidays
    holidayToday: HolidayTodayResult | null = null;
    holidayTodayError: string | null = null;
    tradingCheckDate = new Date().toISOString().slice(0, 10);
    tradingDayResult: TradingDayResult | null = null;
    tradingDayError: string | null = null;

    // Kafka
    kafkaLogs: KafkaPublishLog[] = [];
    kafkaCount = 50;
    kafkaError: string | null = null;

    private subs = new Subscription();

    constructor(private svc: MfNavService) { }

    // ── Lifecycle ──────────────────────────────────────────────────────────

    ngOnInit(): void {
        this.loadAll();
        this.subs.add(interval(30000).subscribe(() => this.loadAll()));
        this.subs.add(interval(1000).subscribe(() => this.updateCountdown()));
        this.updateCountdown();
    }

    ngOnDestroy(): void { this.subs.unsubscribe(); }

    // ── Load ───────────────────────────────────────────────────────────────

    loadAll(): void {
        this.loading = true;
        this.globalError = null;

        const wrap = <T>(obs: Observable<T>, fallback: T, onError: (err: any) => void): Observable<T> =>
            obs.pipe(catchError((err: any) => {
                onError(err);
                return of(fallback);
            }));

        forkJoin({
            latest: wrap<LatestNavResult | null>(this.svc.getLatest(), null, (err) => { this.globalError = `Failed to load latest: [${err.status}] ${err.message}`; }),
            target: wrap<TargetDateResult | null>(this.svc.getTargetDate(), null, (err) => { this.globalError = `Failed to load target date: [${err.status}] ${err.message}`; }),
            job: wrap<JobLog | null>(this.svc.getLatestLog(), null, (err) => { this.globalError = `Failed to load latest job: [${err.status}] ${err.message}`; }),
            history: wrap<NavFileSummary[]>(this.svc.getHistory(), [], (err) => { this.globalError = `Failed to load history: [${err.status}] ${err.message}`; }),
            logs: wrap<JobLog[]>(this.svc.getLogs(this.logsCount), [], (err) => { this.logsError = `[${err.status}] ${err.message}`; }),
            holidayToday: wrap<HolidayTodayResult | null>(this.svc.getHolidayToday(), null, (err) => { this.holidayTodayError = `[${err.status}] ${err.message}`; }),
            kafka: wrap<KafkaPublishLog[]>(this.svc.getKafkaLogs(this.kafkaCount), [], (err) => { this.kafkaError = `[${err.status}] ${err.message}`; })
        }).subscribe({
            next: ({ latest, target, job, history, logs, holidayToday, kafka }) => {
                this.latestNavDate = latest?.latestNavDate ?? null;
                this.targetDate = target?.targetDate ?? null;
                this.latestJob = job;
                this.navHistory = history;
                this.applyNavFilter();
                this.logs = logs;
                this.holidayToday = holidayToday;
                this.kafkaLogs = kafka;
                this.loading = false;
            },
            error: () => {
                // This should not happen because each observable handles its own errors.
                this.loading = false;
            }
        });
    }

    reloadLogs(): void {
        this.svc.getLogs(this.logsCount).subscribe({
            next: (data) => { this.logs = data; this.logsError = null; },
            error: (err) => { this.logsError = `[${err.status}] ${err.message}`; }
        });
    }

    reloadKafka(): void {
        this.svc.getKafkaLogs(this.kafkaCount).subscribe({
            next: (data) => { this.kafkaLogs = data; this.kafkaError = null; },
            error: (err) => { this.kafkaError = `[${err.status}] ${err.message}`; }
        });
    }

    loadHolidayToday(): void {
        this.svc.getHolidayToday().subscribe({
            next: (data) => { this.holidayToday = data; this.holidayTodayError = null; },
            error: (err) => { this.holidayTodayError = `[${err.status}] ${err.message}`; }
        });
    }

    checkTradingDay(): void {
        if (!this.tradingCheckDate) {
            this.tradingDayError = 'Please select a date to check.';
            return;
        }

        this.svc.checkTradingDay(this.tradingCheckDate).subscribe({
            next: (data) => { this.tradingDayResult = data; this.tradingDayError = null; },
            error: (err) => { this.tradingDayError = `[${err.status}] ${err.message}`; }
        });
    }

    // ── Trigger ────────────────────────────────────────────────────────────

    trigger(date?: string): void {
        this.triggering = true;
        this.triggerResult = null;
        const call = date ? this.svc.triggerForDate(date) : this.svc.trigger();
        call.subscribe({
            next: (res) => {
                this.triggering = false;
                this.triggerResult = {
                    success: true,
                    message: res.wasStored
                        ? `✓ NAV downloaded for ${res.date}. ${res.message ?? ''}`
                        : `ℹ Already stored for ${res.date}.`
                };
                this.loadAll();
            },
            error: (err) => {
                this.triggering = false;
                this.triggerResult = { success: false, message: `✗ Trigger failed [${err.status}]: ${err.message}` };
            }
        });
    }

    // ── NAV History ────────────────────────────────────────────────────────

    applyNavFilter(): void {
        const q = this.navSearch.toLowerCase();
        this.navFiltered = this.navHistory
            .filter(r => !q || r.navDate.includes(q))
            .sort((a, b) => {
                const av = (a as any)[this.navSortCol];
                const bv = (b as any)[this.navSortCol];
                return av < bv ? this.navSortDir : av > bv ? -this.navSortDir : 0;
            });
    }

    navSort(col: string): void {
        if (this.navSortCol === col) this.navSortDir *= -1;
        else { this.navSortCol = col; this.navSortDir = -1; }
        this.applyNavFilter();
    }

    navSortIcon(col: string): string {
        if (this.navSortCol !== col) return '↕';
        return this.navSortDir === -1 ? '↓' : '↑';
    }

    // ── Job Logs ───────────────────────────────────────────────────────────

    toggleLog(id: number): void {
        this.expandedLogId = this.expandedLogId === id ? null : id;
    }

    get successCount(): number { return this.logs.filter(l => l.isSuccess).length; }
    get failureCount(): number { return this.logs.filter(l => !l.isSuccess).length; }

    duration(log: JobLog): string {
        if (log.elapsedSeconds == null) return '—';
        return log.elapsedSeconds < 60
            ? `${log.elapsedSeconds.toFixed(2)}s`
            : `${(log.elapsedSeconds / 60).toFixed(1)}m`;
    }

    // ── Countdown ──────────────────────────────────────────────────────────

    private updateCountdown(): void {
        const now = new Date();
        
        // Convert client time to IST (UTC + 5:30)
        const utc = now.getTime() + (now.getTimezoneOffset() * 60000);
        const istNow = new Date(utc + (3600000 * 5.5));
        
        const istNext = new Date(istNow);
        istNext.setHours(5, 0, 0, 0);
        
        if (istNow >= istNext) {
            istNext.setDate(istNext.getDate() + 1);
        }
        
        const diff = istNext.getTime() - istNow.getTime();
        const h = Math.floor(diff / 3600000);
        const m = Math.floor((diff % 3600000) / 60000);
        const s = Math.floor((diff % 60000) / 1000);
        this.countdown = `${h}h ${m}m ${s}s`;
    }

    // ── Getters ────────────────────────────────────────────────────────────

    get jobStatusClass(): string {
        if (!this.latestJob) return 'unknown';
        return this.latestJob.isSuccess ? 'success' : 'failure';
    }

    get jobStatusLabel(): string {
        if (!this.latestJob) return 'No runs yet';
        return this.latestJob.isSuccess ? 'Success' : 'Failed';
    }

    get kafkaSuccessCount(): number { return this.kafkaLogs.filter(k => k.isSuccess).length; }
    get kafkaFailCount(): number { return this.kafkaLogs.filter(k => !k.isSuccess).length; }

    // ── Formatters ─────────────────────────────────────────────────────────

    formatDate(dt: string | null): string {
        if (!dt) return '—';
        return new Date(dt).toLocaleDateString('en-IN', {
            day: '2-digit', month: 'short', year: 'numeric',
            timeZone: 'Asia/Kolkata'
        });
    }

    formatDateTime(dt: string | null): string {
        if (!dt) return '—';
        return new Date(dt).toLocaleString('en-IN', {
            day: '2-digit', month: 'short', year: 'numeric',
            hour: '2-digit', minute: '2-digit', second: '2-digit',
            timeZone: 'Asia/Kolkata'
        });
    }

    formatTime(dt: string | null): string {
        if (!dt) return '';
        return new Date(dt).toLocaleTimeString('en-IN', {
            hour: '2-digit',
            minute: '2-digit',
            timeZone: 'Asia/Kolkata'
        });
    }

    formatBytes(bytes: number): string {
        if (!bytes) return '—';
        if (bytes < 1024) return `${bytes} B`;
        if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} KB`;
        return `${(bytes / 1048576).toFixed(2)} MB`;
    }

    getHolidayTodayLabel(result: HolidayTodayResult): string {
        const status = result.status?.toLowerCase() ?? '';
        if (status.includes('non-trading')) return 'Non-Trading Day';
        if (result.isHoliday) return 'Holiday';
        return 'Trading Day';
    }

    isHolidayTodayTrading(result: HolidayTodayResult): boolean {
        const status = result.status?.toLowerCase() ?? '';
        if (status.includes('non-trading')) return false;
        return !result.isHoliday;
    }
}