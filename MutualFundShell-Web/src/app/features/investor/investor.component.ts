import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { FamilyPortfolioService } from '../../core/services/family-portfolio.service';
import { HoldingsService } from '../../core/services/holdings.service';
import { AuthFamilyService } from '../../core/services/auth-family.service';
import { AuthService } from '../../core/services/auth.service';
import {
  FamilyOverviewDto,
  MemberSummaryDto,
  HoldingCardDto,
  QuickReturnDto
} from '../../core/models/family-portfolio.model';
import { HoldingDto } from '../../core/models/holding.model';
import { AuthFamilyMemberDto } from '../../core/models/auth-family.model';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

type PeriodKey = 'yesterday' | 'thisWeek' | 'oneMonth' | 'oneYear';

interface PeriodDef {
  key: PeriodKey;
  label: string;
  color: string;
}

const PERIODS: PeriodDef[] = [
  { key: 'yesterday', label: 'Yesterday', color: '#C08A2E' },
  { key: 'thisWeek', label: 'This Week', color: '#5C6EA8' },
  { key: 'oneMonth', label: '1 Month', color: '#7FD1B9' },
  { key: 'oneYear', label: '1 Year', color: '#E8A38E' }
];

interface DonutArc {
  dasharray: string;
  dashoffset: number;
  color: string;
  label?: string;
  value?: string;
}

interface DonutModel {
  size: number;
  thickness: number;
  cx: number;
  cy: number;
  r: number;
  arcs: DonutArc[];
}

interface LegendItem {
  color: string;
  label: string;
  text: string;
  positive: boolean;
}

interface PeriodDonut {
  donut: DonutModel;
  legend: LegendItem[];
}

/** A per-scheme aggregated card ready for display. */
interface SchemeCardVm {
  holdingId: number;
  schemeCode: string;
  schemeName: string;
  fundName: string;
  ownerName: string;
  investedAmount: number;
  currentValue: number;
  units: number;
  gain: number;
  gainPercent: number;
  isGain: boolean;
  returns: PeriodDonut;
  pnl: PeriodDonut;
}

/** A holding card tagged with its owning investor. */
type OwnedHoldingCard = HoldingCardDto & {
  investorUserId: string;
  investorName: string;
};

@Component({
  selector: 'app-investor',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinnerComponent],
  templateUrl: './investor.component.html',
  styleUrls: ['./investor.component.scss']
})
export class InvestorComponent implements OnInit {
  loading = true;
  overview: FamilyOverviewDto | null = null;
  relationshipMap = new Map<string, AuthFamilyMemberDto>();

  allHoldings: HoldingDto[] = [];
  allCards: OwnedHoldingCard[] = [];

  periods = PERIODS;
  selectedUserId = 'family'; // 'family' = aggregate view, else a userId

  // Cover donuts
  coverReturns: PeriodDonut | null = null;
  coverPnl: PeriodDonut | null = null;

  // Scheme cards for the current selection
  schemeCards: SchemeCardVm[] = [];

  // Detail-view (per-scheme ledger)
  selectedScheme: SchemeCardVm | null = null;
  ledgerRows: HoldingDto[] = [];

  // Snapshot job trigger
  isTriggeringSnapshot = false;

  // Filters
  schemeFilter = 'all';
  dateFrom: string | null = null;
  dateTo: string | null = null;

  // View state
  showDetailView = false;

  constructor(
    private familyService: FamilyPortfolioService,
    private holdingsService: HoldingsService,
    private authFamilyService: AuthFamilyService,
    private toastr: ToastrService,
    public auth: AuthService
  ) {}

  ngOnInit(): void {
    this.loadAll();
  }

  // ── Data loading ──────────────────────────────────────────────
  loadAll(): void {
    this.loading = true;

    forkJoin({
      overview: this.familyService.getOverview(),
      holdings: this.holdingsService.getAll()
    }).subscribe({
      next: ({ overview, holdings }) => {
        this.overview = overview;
        this.allHoldings = holdings;
        if (this.auth.currentUser()?.role !== 'User') {
          this.loadRelationships();
        }
        this.loadMemberCards(overview);
      },
      error: () => {
        this.loading = false;
        this.toastr.error('Failed to load family portfolio.');
      }
    });
  }

  /**
   * Fetch each member's holding cards (which carry the per-period
   * returns needed for the donuts) and flatten them into a single
   * owner-tagged list used to build the scheme cards.
   */
  private loadMemberCards(overview: FamilyOverviewDto): void {
    const members = overview.members ?? [];
    if (members.length === 0) {
      this.allCards = [];
      this.rebuildView();
      this.loading = false;
      return;
    }

    forkJoin(
      members.map(m =>
        this.familyService.getMemberHoldings(m.investorUserId).pipe(
          map(res =>
            (res.holdings ?? []).map(
              h =>
                ({
                  ...h,
                  investorUserId: m.investorUserId,
                  investorName: m.investorName
                }) as OwnedHoldingCard
            )
          ),
          catchError(() => of([] as OwnedHoldingCard[]))
        )
      )
    ).subscribe({
      next: (lists) => {
        this.allCards = lists.reduce((acc, list) => acc.concat(list), []);
        this.rebuildView();
        this.loading = false;
      },
      error: () => {
        this.allCards = [];
        this.rebuildView();
        this.loading = false;
      }
    });
  }

  private loadRelationships(): void {
    const anyUserId = this.overview?.members?.[0]?.investorUserId;
    if (!anyUserId) return;

    this.authFamilyService.getRelationshipMapForUser(anyUserId).subscribe({
      next: (map) => (this.relationshipMap = map),
      error: () => {
        /* non-fatal */
      }
    });
  }

  // ── Member selector ───────────────────────────────────────────
  selectMember(userId: string): void {
    this.selectedUserId = userId;
    this.selectedScheme = null;
    this.showDetailView = false;
    this.schemeFilter = 'all';
    this.rebuildView();
  }

  get currentMember(): MemberSummaryDto | null {
    if (this.selectedUserId === 'family' || !this.overview) return null;
    return (
      this.overview.members.find(m => m.investorUserId === this.selectedUserId) ??
      null
    );
  }

  // ── View builders ─────────────────────────────────────────────
  private rebuildView(): void {
    this.buildCoverDonuts();
    this.buildSchemeCards();
  }

  private buildCoverDonuts(): void {
    if (!this.overview) {
      this.coverReturns = null;
      this.coverPnl = null;
      return;
    }

    const pct: number[] = [];
    const amt: number[] = [];

    if (this.selectedUserId === 'family') {
      const members = this.overview.members ?? [];
      PERIODS.forEach(p => {
        let totalAmt = 0;
        let weightedPct = 0;
        let weight = 0;
        members.forEach(m => {
          const r = this.returnOf(m, p.key);
          if (r?.hasData) {
            totalAmt += r.periodGainAmount;
            weightedPct += r.returnPercent * m.totalInvested;
            weight += m.totalInvested;
          }
        });
        amt.push(totalAmt);
        pct.push(weight > 0 ? weightedPct / weight : 0);
      });
    } else {
      const member = this.currentMember;
      PERIODS.forEach(p => {
        const r = member ? this.returnOf(member, p.key) : null;
        pct.push(r?.hasData ? r.returnPercent : 0);
        amt.push(r?.hasData ? r.periodGainAmount : 0);
      });
    }

    this.coverReturns = this.buildPeriodDonut(pct, true, 100, 13);
    this.coverPnl = this.buildPeriodDonut(amt, false, 100, 13);
  }

  private buildSchemeCards(): void {
    const scoped =
      this.selectedUserId === 'family'
        ? this.allCards
        : this.allCards.filter(c => c.investorUserId === this.selectedUserId);

    // Group holdings (one per order) into a single card per scheme.
    const groups = new Map<string, OwnedHoldingCard[]>();
    scoped.forEach(c => {
      const list = groups.get(c.schemeCode);
      if (list) list.push(c);
      else groups.set(c.schemeCode, [c]);
    });

    const cards: SchemeCardVm[] = [];
    groups.forEach((list, schemeCode) => {
      const invested = list.reduce((s, c) => s + c.investedAmount, 0);
      const currentValue = list.reduce((s, c) => s + c.currentValue, 0);
      const units = list.reduce((s, c) => s + c.units, 0);
      const gain = list.reduce((s, c) => s + c.gain, 0);
      const gainPercent = invested > 0 ? (gain / invested) * 100 : 0;

      const owners = Array.from(new Set(list.map(c => c.investorName)));

      const pct: number[] = [];
      const amt: number[] = [];
      PERIODS.forEach(p => {
        let totalAmt = 0;
        let weightedPct = 0;
        let weight = 0;
        list.forEach(c => {
          const r = this.returnOf(c, p.key);
          if (r?.hasData) {
            totalAmt += r.periodGainAmount;
            weightedPct += r.returnPercent * c.investedAmount;
            weight += c.investedAmount;
          }
        });
        amt.push(totalAmt);
        pct.push(weight > 0 ? weightedPct / weight : 0);
      });

      cards.push({
        holdingId: list[0].holdingId,
        schemeCode,
        schemeName: list[0].schemeName,
        fundName: list[0].fundName,
        ownerName: owners.join(', '),
        investedAmount: invested,
        currentValue,
        units,
        gain,
        gainPercent,
        isGain: gain >= 0,
        returns: this.buildPeriodDonut(pct, true, 60, 8),
        pnl: this.buildPeriodDonut(amt, false, 60, 8)
      });
    });

    this.schemeCards = cards.sort((a, b) =>
      b.investedAmount - a.investedAmount
    );
  }

  private returnOf(
    source: MemberSummaryDto | HoldingCardDto,
    key: PeriodKey
  ): QuickReturnDto | null | undefined {
    return (source as unknown as Record<PeriodKey, QuickReturnDto | null | undefined>)[key];
  }

  // ── Donut / legend construction ───────────────────────────────
  private buildPeriodDonut(
    values: number[],
    isPercent: boolean,
    size: number,
    thickness: number
  ): PeriodDonut {
    const r = (size - thickness) / 2;
    const cx = size / 2;
    const cy = size / 2;
    const circumference = 2 * Math.PI * r;
    const totalAbs = values.reduce((s, v) => s + Math.abs(v), 0) || 1;

    let offset = 0;
    const arcs: DonutArc[] = values.map((v, i) => {
      const len = circumference * (Math.abs(v) / totalAbs);
      const arc: DonutArc = {
        dasharray: `${len} ${circumference - len}`,
        dashoffset: -offset,
        color: PERIODS[i].color,
        label: PERIODS[i].label,
        value: isPercent ? this.signedPct(v) : this.signedRupee(v)
      };
      offset += len;
      return arc;
    });

    const legend: LegendItem[] = values.map((v, i) => ({
      color: PERIODS[i].color,
      label: PERIODS[i].label,
      text: isPercent ? this.signedPct(v) : this.signedRupee(v),
      positive: v >= 0
    }));

    return { donut: { size, thickness, cx, cy, r, arcs }, legend };
  }

  // ── Formatting helpers (match reference) ──────────────────────
  fmt(n: number): string {
    return Math.round(Math.abs(n)).toLocaleString('en-IN');
  }
  signedPct(n: number): string {
    return (n >= 0 ? '+' : '-') + Math.abs(n).toFixed(2) + '%';
  }
  signedRupee(n: number): string {
    return (n >= 0 ? '+' : '-') + '₹' + this.fmt(n);
  }

  // ── Aggregate stat helpers (cover) ────────────────────────────
  get totalInvested(): number {
    if (this.selectedUserId === 'family') {
      return this.overview?.totalFamilyInvested ?? 0;
    }
    return this.currentMember?.totalInvested ?? 0;
  }
  get currentValue(): number {
    if (this.selectedUserId === 'family') {
      return this.overview?.totalFamilyCurrentValue ?? 0;
    }
    return this.currentMember?.totalCurrentValue ?? 0;
  }
  get totalGain(): number {
    if (this.selectedUserId === 'family') {
      return this.overview?.totalFamilyGain ?? 0;
    }
    return this.currentMember?.totalGain ?? 0;
  }
  get totalGainPercent(): number {
    if (this.selectedUserId === 'family') {
      return this.overview?.totalFamilyGainPercent ?? 0;
    }
    return this.currentMember?.totalGainPercent ?? 0;
  }

  // ── Investor details helpers ──────────────────────────────────
  relationOf(userId: string): string {
    return this.relationshipMap.get(userId)?.relationshipType ?? '';
  }
  panOf(userId: string): string {
    return this.relationshipMap.get(userId)?.panNumber ?? userId;
  }
  foliosLinkedFor(userId: string): number {
    return new Set(
      this.allHoldings
        .filter(h => h.investorUserId === userId)
        .map(h => h.folioNumber)
    ).size;
  }

  // ── Scheme filter ─────────────────────────────────────────────
  get uniqueSchemes(): string[] {
    return Array.from(new Set(this.schemeCards.map(c => c.schemeName))).sort();
  }

  get filteredSchemes(): SchemeCardVm[] {
    if (this.schemeFilter === 'all') return this.schemeCards;
    return this.schemeCards.filter(c => c.schemeName === this.schemeFilter);
  }

  // ── Scheme card → detail ledger drill-down ────────────────────
  openSchemeDetail(scheme: SchemeCardVm): void {
    this.selectedScheme = scheme;
    this.showDetailView = true;

    this.ledgerRows = this.allHoldings.filter(
      h =>
        h.schemeCode === scheme.schemeCode &&
        (this.selectedUserId === 'family' ||
          h.investorUserId === this.selectedUserId)
    );
    
    // Defer scroll slightly to guarantee Angular hides the list view, updates heights, and renders the detail view first.
    // Try multiple scroll targets to cover different browser and parent shell layouts.
    setTimeout(() => {
      const scrollConfig: ScrollToOptions = { top: 0, behavior: 'auto' };
      window.scrollTo(scrollConfig);
      document.documentElement.scrollTo(scrollConfig);
      document.body.scrollTo(scrollConfig);
      document.querySelector('.shell-content')?.scrollTo(scrollConfig);
    }, 50);
  }

  closeSchemeDetail(): void {
    this.selectedScheme = null;
    this.showDetailView = false;
    this.ledgerRows = [];
  }

  get filteredLedgerRows(): HoldingDto[] {
    let rows = this.ledgerRows;
    if (this.dateFrom)
      rows = rows.filter(r => r.purchaseDate && r.purchaseDate >= this.dateFrom!);
    if (this.dateTo)
      rows = rows.filter(r => r.purchaseDate && r.purchaseDate <= this.dateTo!);

    return rows.sort((a, b) => {
      const dateA = a.purchaseDate ? new Date(a.purchaseDate).getTime() : 0;
      const dateB = b.purchaseDate ? new Date(b.purchaseDate).getTime() : 0;
      return dateB - dateA;
    });
  }

  get ledgerTotals() {
    const rows = this.filteredLedgerRows;
    return {
      invested: rows.reduce((s, r) => s + r.investedAmount, 0),
      currentValue: rows.reduce((s, r) => s + r.currentValue, 0),
      units: rows.reduce((s, r) => s + r.units, 0),
      pnl: rows.reduce((s, r) => s + r.profitLoss, 0)
    };
  }

  clearDateRange(): void {
    this.dateFrom = null;
    this.dateTo = null;
  }

  // ── Manual snapshot job trigger ───────────────────────────────
  triggerSnapshot(): void {
    if (this.isTriggeringSnapshot) return;
    this.isTriggeringSnapshot = true;
    this.familyService.triggerSnapshot().subscribe({
      next: () => {
        this.isTriggeringSnapshot = false;
        this.toastr.success('Snapshot triggered successfully.');
        this.loadAll();
      },
      error: () => {
        this.isTriggeringSnapshot = false;
        this.toastr.error('Failed to trigger snapshot.');
      }
    });
  }
}
