import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';

import { NavService } from '../../core/services/nav.service';
import { HolidayService } from '../../core/services/holiday.service';
import {
  NavComparisonResponseDto,
  SchemeComparisonDto,
  NavHistoryDto
} from '../../core/models/nav.model';
import { HolidayStatusDto } from '../../core/models/holiday-status.model';

import { HolidayBannerComponent } from '../../shared/components/holiday-banner/holiday-banner.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { NavGrowCountPipe } from '../../shared/pipes/nav-grow-count.pipe';

@Component({
  selector: 'app-nav',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    HolidayBannerComponent,
    LoadingSpinnerComponent,
    NavGrowCountPipe
  ],
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.scss']
})
export class NavComponent implements OnInit {
  navData: NavComparisonResponseDto | null = null;
  filtered: SchemeComparisonDto[] = [];
  loading = true;

  searchCtrl = new FormControl('');
  startDate = new FormControl('');
  endDate = new FormControl('');
  expandedSchemes = new Set<string>();

  holidayStatus: HolidayStatusDto | null = null;

  constructor(
    private router: Router,
    private navService: NavService,
    private holidayService: HolidayService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadHolidayStatus();
    this.loadDaily();
    this.searchCtrl.valueChanges
      .subscribe(() => this.applyFilter());
  }

  private loadHolidayStatus(): void {
    this.holidayService.getStatus().subscribe({
      next: (status) => {
        // Only show banner if it's a holiday
        this.holidayStatus = status.isHoliday ? status : null;
        this.cdr.detectChanges();
      },
      error: () => {
        // Silently ignore — banner simply won't show
      }
    });
  }

  loadDaily(): void {
    this.loading = true;
    this.navService.getDaily().subscribe({
      next: (data) => {
        this.navData = data;
        this.filtered = data.schemes;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.toastr.error('Failed to load NAV data.');
        this.cdr.detectChanges();
      }
    });
  }

  loadByDateRange(): void {
    const start = this.startDate.value;
    const end = this.endDate.value;

    if (!start || !end) {
      this.toastr.warning('Please select both dates.');
      return;
    }
    if (new Date(start) > new Date(end)) {
      this.toastr.warning('Start date cannot be after end date.');
      return;
    }

    this.loading = true;
    this.navService.getByDateRange(start, end).subscribe({
      next: (data) => {
        this.navData = data;
        this.filtered = data.schemes;
        this.loading = false;
        this.applyFilter();
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.toastr.error('No NAV data for the selected range.');
        this.cdr.detectChanges();
      }
    });
  }

  applyFilter(): void {
    if (!this.navData) return;
    const term = this.searchCtrl.value?.toLowerCase() || '';
    this.filtered = term
      ? this.navData.schemes.filter(s =>
        s.schemeName.toLowerCase().includes(term) ||
        s.schemeCode.toLowerCase().includes(term) ||
        s.fundName.toLowerCase().includes(term))
      : this.navData.schemes;
  }

  clearSearch(): void {
    this.searchCtrl.setValue('');
  }

  // ── Row helpers ───────────────────────────────────────────────
  getStartEntry(scheme: SchemeComparisonDto): NavHistoryDto | null {
    return scheme.history.length > 0 ? scheme.history[0] : null;
  }

  getEndEntry(scheme: SchemeComparisonDto): NavHistoryDto | null {
    return scheme.history.length > 0
      ? scheme.history[scheme.history.length - 1]
      : null;
  }

  getPerformance(scheme: SchemeComparisonDto): string {
    const last = this.getEndEntry(scheme);
    return last?.percentage ?? '0.00';
  }

  isGrowth(scheme: SchemeComparisonDto): boolean {
    return this.getEndEntry(scheme)?.isGrowth ?? false;
  }

  isTopRank(scheme: SchemeComparisonDto): boolean {
    return scheme.rank <= 3;
  }

  getRankLabel(rank: number): string {
    switch (rank) {
      case 1: return 'TOP 1';
      case 2: return 'TOP 2';
      case 3: return 'TOP 3';
      default: return '';
    }
  }

  // Group schemes by rank for display
  groupedSchemes(): { rank: number; schemes: SchemeComparisonDto[] }[] {
    const groups = new Map<number, SchemeComparisonDto[]>();
    for (const s of this.filtered) {
      if (!groups.has(s.rank)) groups.set(s.rank, []);
      groups.get(s.rank)!.push(s);
    }
    return Array.from(groups.entries())
      .sort((a, b) => a[0] - b[0])
      .map(([rank, schemes]) => ({ rank, schemes }));
  }

  get startDateLabel(): string {
    if (!this.navData?.schemes?.[0]?.history?.[0]) return '';
    return new Date(this.navData.schemes[0].history[0].date)
      .toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: '2-digit' });
  }

  get endDateLabel(): string {
    const scheme = this.navData?.schemes?.[0];
    if (!scheme?.history?.length) return '';
    const last = scheme.history[scheme.history.length - 1];
    return new Date(last.date)
      .toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: '2-digit' });
  }

  toggleScheme(schemeCode: string): void {
    if (this.expandedSchemes.has(schemeCode)) {
      this.expandedSchemes.delete(schemeCode);
    } else {
      this.expandedSchemes.add(schemeCode);
    }
  }

  isExpanded(schemeCode: string): boolean {
    return this.expandedSchemes.has(schemeCode);
  }

  viewSchemeDetails(schemeCode: string): void {
    this.router.navigate(['/nav-comparison/scheme', schemeCode]);
  }
}
