import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { forkJoin } from 'rxjs';

import { OrderService } from '../../core/services/order.service';
import { SchemeService } from '../../core/services/scheme.service';
import { AuthService } from '../../core/services/auth.service';
import {
  OrderSanityAuditItemDto,
  OrderSanityAuditSummaryDto,
  PreOrderSanityCheckResultDto
} from '../../core/models/order.model';
import { SchemeEnrollmentDto } from '../../core/models/scheme.model';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-order-sanity-check',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './order-sanity-check.component.html',
  styleUrls: ['./order-sanity-check.component.scss']
})
export class OrderSanityCheckComponent implements OnInit {
  activeTab: 'audit' | 'precheck' = 'audit';
  loading = true;
  scanning = false;

  summary: OrderSanityAuditSummaryDto | null = null;
  filteredItems: OrderSanityAuditItemDto[] = [];
  schemes: SchemeEnrollmentDto[] = [];

  // Filters
  severityFilter = 'all';
  searchQuery = '';

  // Pre-Order Sanity Calculator
  calcForm!: FormGroup;
  checkingPreOrder = false;
  preCheckResult: PreOrderSanityCheckResultDto | null = null;

  constructor(
    private orderService: OrderService,
    private schemeService: SchemeService,
    private fb: FormBuilder,
    private router: Router,
    private toastr: ToastrService,
    public auth: AuthService
  ) {}

  ngOnInit(): void {
    this.initCalcForm();
    this.loadInitial();
  }

  private initCalcForm(): void {
    this.calcForm = this.fb.group({
      schemeCode: ['', Validators.required],
      orderDate: [new Date().toISOString().slice(0, 10), Validators.required],
      investedAmount: [1000, [Validators.required, Validators.min(0.01)]],
      purchaseNAV: [null, [Validators.required, Validators.min(0.0001)]],
      unitsAllotted: [null]
    });
  }

  loadInitial(): void {
    this.loading = true;
    forkJoin({
      audit: this.orderService.scanOrdersSanity(),
      schemes: this.schemeService.getApproved()
    }).subscribe({
      next: ({ audit, schemes }) => {
        this.summary = audit;
        this.schemes = schemes;
        if (schemes.length > 0 && !this.calcForm.get('schemeCode')?.value) {
          this.calcForm.patchValue({ schemeCode: schemes[0].schemeCode });
        }
        this.applyFilters();
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.toastr.error(err.error?.error || 'Failed to load sanity check data.');
      }
    });
  }

  reScanOrders(): void {
    this.scanning = true;
    this.orderService.scanOrdersSanity().subscribe({
      next: (audit) => {
        this.summary = audit;
        this.applyFilters();
        this.scanning = false;
        this.toastr.success(`Scanned ${audit.totalScanned} orders against AMFI NAV history.`);
      },
      error: (err) => {
        this.scanning = false;
        this.toastr.error(err.error?.error || 'Failed to scan orders.');
      }
    });
  }

  switchTab(tab: 'audit' | 'precheck'): void {
    this.activeTab = tab;
  }

  setSeverityFilter(sev: string): void {
    this.severityFilter = sev;
    this.applyFilters();
  }

  applyFilters(): void {
    const items = this.summary?.items ?? [];
    const q = this.searchQuery.trim().toLowerCase();
    this.filteredItems = items.filter(item => {
      if (this.severityFilter !== 'all' && item.severity !== this.severityFilter) return false;
      if (q) {
        const hay = `${item.orderNumber} ${item.schemeName} ${item.investorName} ${item.finding}`.toLowerCase();
        if (!hay.includes(q)) return false;
      }
      return true;
    });
  }

  fixInOrderCorrections(orderNumber: string): void {
    this.router.navigate(['/order-corrections'], { queryParams: { search: orderNumber } });
  }

  // ── Pre-Order Sanity Calculator ───────────────────────────────
  runPreCheck(): void {
    if (this.calcForm.invalid) {
      this.calcForm.markAllAsTouched();
      return;
    }
    const v = this.calcForm.value;
    this.checkingPreOrder = true;
    this.orderService.checkPreOrderSanity({
      schemeCode: v.schemeCode,
      orderDate: v.orderDate,
      investedAmount: Number(v.investedAmount),
      purchaseNAV: Number(v.purchaseNAV),
      unitsAllotted: v.unitsAllotted ? Number(v.unitsAllotted) : null
    }).subscribe({
      next: (res) => {
        this.preCheckResult = res;
        this.checkingPreOrder = false;
      },
      error: (err) => {
        this.checkingPreOrder = false;
        this.toastr.error(err.error?.error || 'Sanity check failed.');
      }
    });
  }

  applyAmfiNav(): void {
    if (!this.preCheckResult?.amfiNav) return;
    const amt = Number(this.calcForm.get('investedAmount')?.value || 0);
    const nav = this.preCheckResult.amfiNav;
    this.calcForm.patchValue({
      purchaseNAV: nav,
      unitsAllotted: amt > 0 ? Number((amt / nav).toFixed(4)) : null
    });
    this.runPreCheck();
    this.toastr.success('Applied AMFI NAV and recalculated expected units.');
  }

  goToCreateOrder(): void {
    this.router.navigate(['/orders']);
  }

  formatDate(dateStr: string | null | undefined): string {
    if (!dateStr) return '—';
    return dateStr.slice(0, 10);
  }
}
