import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';

import { AnalyticsService } from '../../core/services/analytics.service';
import { AuthUserService } from '../../core/services/auth-user.service';
import { AuthCookieService } from '../../core/services/auth-cookie.service';
import {
  AnalyticsHubResponseDto,
  AnalyticsMonthOrderDto,
  MonthlyLedgerYearDto,
  YearSchemeBreakdownDto
} from '../../core/models/analytics.model';
import { AuthUserDto } from '../../core/models/auth-user.model';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

type AnalyticsTab = 'stepup' | 'year' | 'month' | 'h2h';

const MONTH_LABELS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinnerComponent],
  templateUrl: './analytics.component.html',
  styleUrls: ['./analytics.component.scss']
})
export class AnalyticsComponent implements OnInit {
  loading = true;
  activeTab: AnalyticsTab = 'stepup';

  canSwitchInvestor = false;
  investors: AuthUserDto[] = [];
  selectedInvestorId = 'family';

  data: AnalyticsHubResponseDto | null = null;
  selectedYearForBreakdown: number | 'all' = 0;
  selectedLedgerYear = 0;

  monthLabels = MONTH_LABELS;

  // Month cell drill-down modal
  drillModalOpen = false;
  drillLoading = false;
  drillTitle = '';
  drillOrders: AnalyticsMonthOrderDto[] = [];

  constructor(
    private analyticsService: AnalyticsService,
    private authUserService: AuthUserService,
    private authCookie: AuthCookieService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    const role = this.authCookie.getUser()?.raw['role'];
    this.canSwitchInvestor = role === 'Admin' || role === 'Employee';

    if (this.canSwitchInvestor) {
      this.authUserService.getInvestors().subscribe({
        next: (list) => { this.investors = list ?? []; },
        error: () => { /* non-fatal */ }
      });
    }

    this.loadAnalytics();
  }

  loadAnalytics(): void {
    this.loading = true;
    const filter = this.canSwitchInvestor ? this.selectedInvestorId : null;

    this.analyticsService.getAnalytics(filter).subscribe({
      next: (res) => {
        this.loading = false;
        this.data = res;
        if (res.years.length > 0) {
          const latest = res.years[res.years.length - 1];
          this.selectedYearForBreakdown = latest;
          this.selectedLedgerYear = latest;
        }
      },
      error: (err) => {
        this.loading = false;
        this.toastr.error(err.error?.error || 'Failed to load analytics.');
      }
    });
  }

  onInvestorChange(): void {
    this.loadAnalytics();
  }

  setTab(tab: AnalyticsTab): void {
    this.activeTab = tab;
  }

  get filteredYearBreakdowns(): YearSchemeBreakdownDto[] {
    if (!this.data) return [];
    if (this.selectedYearForBreakdown === 'all') {
      return [...this.data.yearWise.breakdowns].sort((a, b) => b.year - a.year);
    }
    return this.data.yearWise.breakdowns.filter(b => b.year === this.selectedYearForBreakdown);
  }

  get activeLedgerYear(): MonthlyLedgerYearDto | null {
    if (!this.data || this.data.monthlyLedger.length === 0) return null;
    return this.data.monthlyLedger.find(m => m.year === this.selectedLedgerYear)
      ?? this.data.monthlyLedger[0];
  }

  get stepUpCompareLabel(): string {
    if (!this.data) return '';
    const s = this.data.sipStepUp;
    const base = s.years.length >= 3 ? s.firstYear : s.prevYear;
    const comp = s.years.length >= 3 ? s.prevYear : s.currentYear;
    return `${base}→${comp}`;
  }

  getYearMonths(yr: number): number {
    return this.data?.yearMonthsElapsed?.[yr] ?? 12;
  }

  openMonthOrders(year: number, monthIndex: number, schemeCode: string, schemeName: string): void {
    const monthNum = monthIndex + 1;
    this.drillTitle = `${MONTH_LABELS[monthIndex]} ${year} — ${schemeName}`;
    this.drillModalOpen = true;
    this.drillLoading = true;
    this.drillOrders = [];

    const filter = this.canSwitchInvestor ? this.selectedInvestorId : null;
    this.analyticsService.getMonthOrders(year, monthNum, schemeCode, filter).subscribe({
      next: (orders) => {
        this.drillLoading = false;
        this.drillOrders = orders ?? [];
      },
      error: () => {
        this.drillLoading = false;
        this.toastr.error('Failed to load orders for this month.');
      }
    });
  }

  closeDrillModal(): void {
    this.drillModalOpen = false;
  }
}
