import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { interval, Subscription } from 'rxjs';
import { DashboardService, DashboardData, DashboardStats, CategoryMetrics } from '../../core/services/dashboard.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit, OnDestroy {
  loading = true;
  error: string | null = null;
  dashboardData: DashboardData | null = null;
  stats: DashboardStats | null = null;
  lastRefreshedDate: Date = new Date();

  private refreshSubscription: Subscription | null = null;

  constructor(
    private dashboardService: DashboardService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
    // Refresh data every 60 seconds
    this.refreshSubscription = interval(60000).subscribe(() => {
      this.loadDashboardData();
    });
  }

  ngOnDestroy(): void {
    if (this.refreshSubscription) {
      this.refreshSubscription.unsubscribe();
    }
  }

  loadDashboardData(): void {
    this.loading = true;
    this.error = null;

    this.dashboardService.getDashboardData().subscribe({
      next: (data) => {
        this.dashboardData = data;
        this.stats = data.stats;
        this.loading = false;
        this.lastRefreshedDate = new Date();
      },
      error: (err) => {
        this.error = `Failed to load dashboard data: ${err.message || err}`;
        this.loading = false;
      }
    });
  }

  // User Info & Role Helpers
  getUserName(): string {
    const user = this.authService.getCurrentUser();
    if (user?.firstName || user?.lastName) {
      return `${user.firstName || ''} ${user.lastName || ''}`.trim();
    }
    if (user?.name) return user.name;
    if (user?.email) return user.email.split('@')[0];
    return 'User';
  }

  getUserRoleLabel(): string {
    if (this.isAdmin()) return 'System Administrator';
    if (this.isEmployee()) return 'Staff Employee';
    return 'Investor Account';
  }

  getCategoryList(): CategoryMetrics[] {
    if (!this.stats?.categories) return [];
    const cats = this.stats.categories;
    return [cats.equity, cats.debt, cats.gold];
  }

  hasPortfolioData(): boolean {
    return !!(this.stats && (this.stats.totalInvested > 0 || this.stats.totalHoldings > 0 || this.stats.totalCurrentValue > 0));
  }

  // Navigation methods
  navigateToPortfolio(): void {
    this.router.navigate(['/portfolio']);
  }

  navigateToOrders(): void {
    this.router.navigate(['/orders']);
  }

  navigateToUsers(): void {
    this.router.navigate(['/user']);
  }

  navigateToPendingApprovals(): void {
    this.router.navigate(['/pending-approvals']);
  }

  navigateToSchemes(): void {
    this.router.navigate(['/scheme']);
  }

  navigateToFamilyGroups(): void {
    this.router.navigate(['/family-groups']);
  }

  navigateToOrderDetails(orderId?: number): void {
    if (orderId) {
      this.router.navigate(['/orders'], { queryParams: { id: orderId } });
    } else {
      this.router.navigate(['/orders']);
    }
  }

  // Helper methods
  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0
    }).format(value || 0);
  }

  formatPercent(value: number): string {
    const val = value || 0;
    return `${val >= 0 ? '+' : ''}${val.toFixed(2)}%`;
  }

  formatDate(dateString: string | null | undefined): string {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return 'N/A';
    return date.toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  formatDateTime(dateString: string | null | undefined): string {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return 'N/A';
    return date.toLocaleString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  // Role-based access control
  isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  isEmployee(): boolean {
    return this.authService.isEmployee();
  }

  canViewOrders(): boolean {
    return this.isAdmin() || this.isEmployee() || this.authService.hasPermission('order.view');
  }

  canViewUsers(): boolean {
    return this.isAdmin() || this.authService.hasPermission('user.manage');
  }

  canViewPortfolio(): boolean {
    return true; // Available for all logged in accounts
  }

  canViewSchemes(): boolean {
    return true; // Available for all users
  }
}
