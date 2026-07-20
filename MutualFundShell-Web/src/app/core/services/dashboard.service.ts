import { Injectable } from '@angular/core';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { FamilyOverviewDto } from '../models/family-portfolio.model';
import { HoldingDto } from '../models/holding.model';
import { InvestmentOrderDto } from '../models/order.model';
import { UserDto } from '../models/user.model';
import { FamilyPortfolioService } from './family-portfolio.service';
import { HoldingsService } from './holdings.service';
import { OrderService } from './order.service';
import { UserService } from './user.service';

export interface CategoryMetrics {
  category: 'Equity' | 'Debt' | 'Gold';
  totalInvested: number;
  currentValue: number;
  totalReturn: number;
  totalReturnPercent: number;
  isReturnPositive: boolean;
  portfolioPercentage: number;
  schemeCount: number;
  color: string;
}

export interface DashboardStats {
  // Portfolio Overview
  totalInvested: number;
  totalCurrentValue: number;
  totalGain: number;
  totalGainPercent: number;
  isGain: boolean;
  totalMembers: number;
  totalSchemes: number;
  equitySchemeCount: number;
  debtSchemeCount: number;
  goldSchemeCount: number;

  // Category Metrics (Equity, Debt, Gold)
  categories: {
    equity: CategoryMetrics;
    debt: CategoryMetrics;
    gold: CategoryMetrics;
  };

  // Orders
  totalOrders: number;
  pendingOrders: number;
  activeOrders: number;
  completedOrders: number;

  // Users
  totalUsers: number;
  pendingApprovals: number;
  activeUsers: number;

  // Holdings
  totalHoldings: number;
  profitableHoldings: number;
  lossHoldings: number;
}

export interface DashboardData {
  stats: DashboardStats;
  familyOverview: FamilyOverviewDto | null;
  recentOrders: InvestmentOrderDto[];
  recentHoldings: HoldingDto[];
  recentUsers: UserDto[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(
    private familyPortfolioService: FamilyPortfolioService,
    private holdingsService: HoldingsService,
    private orderService: OrderService,
    private userService: UserService
  ) { }

  getDashboardData(): Observable<DashboardData> {
    return forkJoin({
      familyOverview: this.familyPortfolioService.getOverview().pipe(catchError(() => of(null))),
      holdings: this.holdingsService.getAll().pipe(catchError(() => of([]))),
      orders: this.orderService.getAll().pipe(catchError(() => of([]))),
      users: this.userService.getAll().pipe(catchError(() => of([])))
    }).pipe(
      map(data => this.transformToDashboardData(data))
    );
  }

  private transformToDashboardData(data: any): DashboardData {
    const familyOverview: FamilyOverviewDto | null = data.familyOverview;
    const holdings: HoldingDto[] = data.holdings || [];
    const orders: InvestmentOrderDto[] = data.orders || [];
    const users: UserDto[] = data.users || [];

    // Calculate Category Metrics for Equity, Debt, and Gold
    const categoryCalc = this.calculateCategoryMetrics(holdings);

    // Calculate stats
    const stats: DashboardStats = {
      // Portfolio Overview
      totalInvested: familyOverview?.totalFamilyInvested || holdings.reduce((sum, h) => sum + h.investedAmount, 0),
      totalCurrentValue: familyOverview?.totalFamilyCurrentValue || holdings.reduce((sum, h) => sum + (h.currentValue || h.investedAmount), 0),
      totalGain: familyOverview?.totalFamilyGain || (holdings.reduce((sum, h) => sum + (h.currentValue || h.investedAmount), 0) - holdings.reduce((sum, h) => sum + h.investedAmount, 0)),
      totalGainPercent: familyOverview?.totalFamilyGainPercent || 0,
      isGain: familyOverview ? familyOverview.isFamilyGain : (holdings.reduce((sum, h) => sum + (h.currentValue || h.investedAmount), 0) >= holdings.reduce((sum, h) => sum + h.investedAmount, 0)),
      totalMembers: familyOverview?.totalMembers || (holdings.length > 0 ? new Set(holdings.map(h => h.investorUserId)).size : 0),
      totalSchemes: familyOverview?.totalSchemes || new Set(holdings.map(h => h.schemeCode)).size,
      equitySchemeCount: categoryCalc.equity.schemeCount,
      debtSchemeCount: categoryCalc.debt.schemeCount,
      goldSchemeCount: categoryCalc.gold.schemeCount,

      categories: categoryCalc,

      // Orders
      totalOrders: orders.length,
      pendingOrders: orders.filter(o => o.status === 'Requested' || o.status === 'Assigned' || o.status === 'Submitted').length,
      activeOrders: orders.filter(o => o.status === 'Active').length,
      completedOrders: orders.filter(o => o.status === 'Verified').length,

      // Users
      totalUsers: users.length,
      pendingApprovals: users.filter(u => u.statusName === 'Pending' || u.approvalStatus === 0).length,
      activeUsers: users.filter(u => u.isActive).length,

      // Holdings
      totalHoldings: holdings.length,
      profitableHoldings: holdings.filter(h => h.isProfit).length,
      lossHoldings: holdings.filter(h => !h.isProfit).length
    };

    // Recalculate total gain percentage if totalInvested > 0
    if (stats.totalInvested > 0) {
      stats.totalGain = stats.totalCurrentValue - stats.totalInvested;
      stats.totalGainPercent = Math.round((stats.totalGain / stats.totalInvested) * 10000) / 100;
      stats.isGain = stats.totalGain >= 0;
    }

    // Get recent items (last 5)
    const recentOrders = orders
      .slice()
      .sort((a, b) => new Date(b.orderDate || b.createdAt).getTime() - new Date(a.orderDate || a.createdAt).getTime())
      .slice(0, 5);

    const recentHoldings = holdings
      .slice()
      .sort((a, b) => new Date(b.lastUpdatedDate || b.purchaseDate).getTime() - new Date(a.lastUpdatedDate || a.purchaseDate).getTime())
      .slice(0, 5);

    const recentUsers = users
      .slice()
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5);

    return {
      stats,
      familyOverview,
      recentOrders,
      recentHoldings,
      recentUsers
    };
  }

  private calculateCategoryMetrics(holdings: HoldingDto[]) {
    let eqInv = 0, eqCur = 0, eqCount = 0;
    let dbtInv = 0, dbtCur = 0, dbtCount = 0;
    let gldInv = 0, gldCur = 0, gldCount = 0;

    for (const h of holdings) {
      const name = (h.schemeName || '').toLowerCase();
      const inv = h.investedAmount || 0;
      const cur = (h.currentValue && h.currentValue > 0) ? h.currentValue : inv;

      if (name.includes('gold') || name.includes('silver')) {
        gldInv += inv;
        gldCur += cur;
        gldCount++;
      } else if (name.includes('debt') || name.includes('bond') || name.includes('liquid') || name.includes('gilt') || name.includes('psu') || name.includes('income') || name.includes('money market') || name.includes('treasury')) {
        dbtInv += inv;
        dbtCur += cur;
        dbtCount++;
      } else {
        // Equity (includes Equity, Hybrid/Balance)
        eqInv += inv;
        eqCur += cur;
        eqCount++;
      }
    }

    const totalVal = eqCur + dbtCur + gldCur;
    const calcPct = (v: number) => totalVal > 0 ? Math.round((v / totalVal) * 1000) / 10 : 0;

    const buildMetric = (cat: 'Equity' | 'Debt' | 'Gold', inv: number, cur: number, count: number, color: string): CategoryMetrics => {
      const ret = cur - inv;
      const retPct = inv > 0 ? Math.round((ret / inv) * 10000) / 100 : 0;
      return {
        category: cat,
        totalInvested: Math.round(inv * 100) / 100,
        currentValue: Math.round(cur * 100) / 100,
        totalReturn: Math.round(ret * 100) / 100,
        totalReturnPercent: retPct,
        isReturnPositive: ret >= 0,
        portfolioPercentage: calcPct(cur),
        schemeCount: count,
        color
      };
    };

    return {
      equity: buildMetric('Equity', eqInv, eqCur, eqCount, '#3B82F6'),
      debt: buildMetric('Debt', dbtInv, dbtCur, dbtCount, '#10B981'),
      gold: buildMetric('Gold', gldInv, gldCur, gldCount, '#EAB308')
    };
  }
}
