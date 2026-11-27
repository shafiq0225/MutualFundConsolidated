import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { forkJoin } from 'rxjs';

import { OrderService } from '../../core/services/order.service';
import { SchemeService } from '../../core/services/scheme.service';
import { AuthUserService } from '../../core/services/auth-user.service';
import {
  InvestmentOrderDto,
  OrderCorrectionImpactDto,
  OrderCorrectionLogDto,
  AdminEditOrderDto
} from '../../core/models/order.model';
import { SchemeEnrollmentDto } from '../../core/models/scheme.model';
import { AuthUserDto } from '../../core/models/auth-user.model';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-order-corrections',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './order-corrections.component.html',
  styleUrls: ['./order-corrections.component.scss']
})
export class OrderCorrectionsComponent implements OnInit {
  loading = true;
  activeTab: 'orders' | 'audit' = 'orders';

  orders: InvestmentOrderDto[] = [];
  filteredOrders: InvestmentOrderDto[] = [];
  schemes: SchemeEnrollmentDto[] = [];
  investors: AuthUserDto[] = [];
  auditLogs: OrderCorrectionLogDto[] = [];
  loadingLogs = false;

  // Filters
  searchQuery = '';
  investorFilter = 'all';
  schemeFilter = 'all';
  dateFilter = '';

  // Multi-select
  selectedIds = new Set<number>();

  // Edit Modal
  showEditModal = false;
  editingOrder: InvestmentOrderDto | null = null;
  editForm!: FormGroup;
  editImpact: OrderCorrectionImpactDto | null = null;
  savingEdit = false;

  // Reverse Modal
  showReverseModal = false;
  reversingIds: number[] = [];
  reverseImpact: OrderCorrectionImpactDto | null = null;
  loadingImpact = false;
  reverseReason = '';
  reversing = false;

  constructor(
    private orderService: OrderService,
    private schemeService: SchemeService,
    private authUserService: AuthUserService,
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    const initialSearch = this.route.snapshot.queryParamMap.get('search');
    if (initialSearch) {
      this.searchQuery = initialSearch;
    }
    this.initEditForm();
    this.loadAll();
  }

  private initEditForm(): void {
    this.editForm = this.fb.group({
      investorUserId: ['', Validators.required],
      schemeCode: ['', Validators.required],
      orderDate: ['', Validators.required],
      folioNumber: ['', Validators.required],
      investedAmount: [null, [Validators.required, Validators.min(0.01)]],
      purchaseNAV: [null, [Validators.required, Validators.min(0.0001)]],
      unitsAllotted: [null, [Validators.required, Validators.min(0.0001)]],
      reason: ['']
    });
  }

  loadAll(): void {
    this.loading = true;
    forkJoin({
      orders: this.orderService.getAll(),
      schemes: this.schemeService.getApproved(),
      investors: this.authUserService.getInvestors(),
      logs: this.orderService.getCorrectionLogs(100)
    }).subscribe({
      next: ({ orders, schemes, investors, logs }) => {
        this.orders = orders;
        this.schemes = schemes;
        this.investors = investors;
        this.auditLogs = logs;
        this.applyFilters();
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.toastr.error(err.error?.error || 'Failed to load orders for correction.');
      }
    });
  }

  switchTab(tab: 'orders' | 'audit'): void {
    this.activeTab = tab;
    if (tab === 'audit') {
      this.refreshLogs();
    }
  }

  refreshLogs(): void {
    this.loadingLogs = true;
    this.orderService.getCorrectionLogs(100).subscribe({
      next: (logs) => {
        this.auditLogs = logs;
        this.loadingLogs = false;
      },
      error: () => {
        this.loadingLogs = false;
      }
    });
  }

  get uniqueInvestors(): string[] {
    const map = new Map<string, string>();
    for (const o of this.orders) {
      const trimmed = (o.investorName || '').trim();
      if (!trimmed) continue;
      const key = trimmed.toLowerCase();
      if (!map.has(key)) map.set(key, trimmed);
    }
    return Array.from(map.values()).sort((a, b) => a.localeCompare(b));
  }

  get uniqueSchemes(): string[] {
    return Array.from(new Set(this.orders.map(o => o.schemeName))).sort();
  }

  applyFilters(): void {
    const q = this.searchQuery.trim().toLowerCase();
    const invFilter = this.investorFilter.toLowerCase();
    this.filteredOrders = this.orders.filter(o => {
      if (q) {
        const hay = `${o.orderNumber} ${o.schemeName} ${o.folioNumber || ''} ${o.investorName}`.toLowerCase();
        if (!hay.includes(q)) return false;
      }
      if (this.investorFilter !== 'all' && (o.investorName || '').trim().toLowerCase() !== invFilter) return false;
      if (this.schemeFilter !== 'all' && o.schemeName !== this.schemeFilter) return false;
      if (this.dateFilter && (!o.orderDate || !o.orderDate.startsWith(this.dateFilter))) return false;
      return true;
    });
  }

  resetFilters(): void {
    this.searchQuery = '';
    this.investorFilter = 'all';
    this.schemeFilter = 'all';
    this.dateFilter = '';
    this.applyFilters();
  }

  // ── Multi-select ──────────────────────────────────────────────
  isSelected(id: number): boolean {
    return this.selectedIds.has(id);
  }

  toggleRow(id: number, checked: boolean): void {
    if (checked) this.selectedIds.add(id);
    else this.selectedIds.delete(id);
  }

  get allFilteredSelected(): boolean {
    return this.filteredOrders.length > 0 && this.filteredOrders.every(o => this.selectedIds.has(o.id));
  }

  toggleSelectAll(checked: boolean): void {
    if (checked) {
      this.filteredOrders.forEach(o => this.selectedIds.add(o.id));
    } else {
      this.filteredOrders.forEach(o => this.selectedIds.delete(o.id));
    }
  }

  clearSelection(): void {
    this.selectedIds.clear();
  }

  // ── Edit Modal ────────────────────────────────────────────────
  openEditModal(order: InvestmentOrderDto): void {
    this.editingOrder = order;
    this.editImpact = null;
    this.showEditModal = true;

    const isoDate = order.orderDate ? order.orderDate.slice(0, 10) : '';
    const units = order.unitsAllotted ?? (order.purchaseNAV && order.purchaseNAV > 0
      ? Number((order.investedAmount / order.purchaseNAV).toFixed(4))
      : 0);

    this.editForm.patchValue({
      investorUserId: order.investorUserId,
      schemeCode: order.schemeCode,
      orderDate: isoDate,
      folioNumber: order.folioNumber || '',
      investedAmount: order.investedAmount,
      purchaseNAV: order.purchaseNAV ?? 0,
      unitsAllotted: units,
      reason: ''
    });

    this.orderService.getCorrectionImpact([order.id]).subscribe({
      next: (impact) => (this.editImpact = impact),
      error: () => { /* non-fatal preview */ }
    });
  }

  recalcUnitsFromNav(): void {
    const amt = Number(this.editForm.get('investedAmount')?.value || 0);
    const nav = Number(this.editForm.get('purchaseNAV')?.value || 0);
    if (amt > 0 && nav > 0) {
      this.editForm.patchValue({
        unitsAllotted: Number((amt / nav).toFixed(6))
      }, { emitEvent: false });
    }
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.editingOrder = null;
    this.editImpact = null;
  }

  saveEditOrder(): void {
    if (!this.editingOrder || this.editForm.invalid || this.savingEdit) {
      this.editForm.markAllAsTouched();
      return;
    }

    const v = this.editForm.value;
    const inv = this.investors.find(i => (i.panNumber || i.id) === v.investorUserId || i.id === v.investorUserId);
    const sch = this.schemes.find(s => s.schemeCode === v.schemeCode);

    const investorName = inv
      ? (inv.fullName || `${inv.firstName} ${inv.lastName}`.trim())
      : this.editingOrder.investorName;
    const schemeName = sch ? sch.schemeName : this.editingOrder.schemeName;
    const fundName = sch ? (sch.fundName || '') : this.editingOrder.fundName;

    const dto: AdminEditOrderDto = {
      investorUserId: v.investorUserId,
      investorName,
      schemeCode: v.schemeCode,
      schemeName,
      fundName,
      folioNumber: String(v.folioNumber).trim(),
      orderDate: v.orderDate,
      investedAmount: Number(v.investedAmount),
      purchaseNAV: Number(v.purchaseNAV),
      unitsAllotted: Number(v.unitsAllotted),
      reason: v.reason ? String(v.reason).trim() : null
    };

    this.savingEdit = true;
    this.orderService.adminEditOrder(this.editingOrder.id, dto).subscribe({
      next: (res) => {
        this.savingEdit = false;
        this.toastr.success(
          `Updated ${res.orderNumber}${res.snapshotsRecalculated > 0 ? ` & recalculated ${res.snapshotsRecalculated} snapshot(s)` : ''}.`
        );
        this.closeEditModal();
        this.loadAll();
      },
      error: (err) => {
        this.savingEdit = false;
        this.toastr.error(err.error?.error || 'Failed to update order.');
      }
    });
  }

  // ── Reverse Modal ─────────────────────────────────────────────
  openSingleReverseModal(order: InvestmentOrderDto): void {
    this.openReverseModal([order.id]);
  }

  openBulkReverseModal(): void {
    const ids = Array.from(this.selectedIds);
    if (ids.length === 0) return;
    this.openReverseModal(ids);
  }

  private openReverseModal(ids: number[]): void {
    this.reversingIds = ids;
    this.reverseImpact = null;
    this.reverseReason = '';
    this.loadingImpact = true;
    this.showReverseModal = true;

    this.orderService.getCorrectionImpact(ids).subscribe({
      next: (impact) => {
        this.reverseImpact = impact;
        this.loadingImpact = false;
      },
      error: () => {
        this.loadingImpact = false;
        this.toastr.error('Failed to preview dependent records.');
      }
    });
  }

  closeReverseModal(): void {
    this.showReverseModal = false;
    this.reversingIds = [];
    this.reverseImpact = null;
    this.reverseReason = '';
  }

  confirmReverseOrders(): void {
    const reason = this.reverseReason.trim();
    if (reason.length < 3) {
      this.toastr.warning('Please enter a reason (at least 3 characters) for the audit log.');
      return;
    }

    this.reversing = true;
    this.orderService.adminReverseOrders({ orderIds: this.reversingIds, reason }).subscribe({
      next: (res) => {
        this.reversing = false;
        this.toastr.success(
          `Reversed ${res.ordersRemoved} order(s), ${res.holdingsRemoved} holding(s), and ${res.snapshotsRemoved} snapshot(s).`
        );
        this.reversingIds.forEach(id => this.selectedIds.delete(id));
        this.closeReverseModal();
        this.loadAll();
      },
      error: (err) => {
        this.reversing = false;
        this.toastr.error(err.error?.error || 'Failed to reverse selected orders.');
      }
    });
  }

  investorOptionValue(u: AuthUserDto): string {
    return u.panNumber || u.id;
  }

  get editHoldingId(): number | null {
    return this.editImpact?.items?.[0]?.holdingId ?? null;
  }

  formatDate(dateStr: string | null | undefined): string {
    if (!dateStr) return '—';
    return dateStr.slice(0, 10);
  }
}
