import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { forkJoin } from 'rxjs';

import { OrderService } from '../../core/services/order.service';
import { SchemeService } from '../../core/services/scheme.service';
import { AuthFamilyService } from '../../core/services/auth-family.service';
import { AuthUserService } from '../../core/services/auth-user.service';
import { AuthService } from '../../core/services/auth.service';
import { InvestmentOrderDto, OrderStatus } from '../../core/models/order.model';
import { SchemeEnrollmentDto } from '../../core/models/scheme.model';
import { AuthFamilyMemberDto } from '../../core/models/auth-family.model';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

interface InvestorOption {
  userId: string;
  fullName: string;
  relation: string;
}

interface StageInfo {
  key: OrderStatus;
  label: string;
}

const STAGES: StageInfo[] = [
  { key: 'Requested', label: 'Requested' },
  { key: 'Assigned',  label: 'Assigned' },
  { key: 'Submitted', label: 'Submitted' },
  { key: 'Verified',  label: 'Verified' }
];

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, LoadingSpinnerComponent],
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.scss']
})
export class OrdersComponent implements OnInit {
  loading = true;
  orders: InvestmentOrderDto[] = [];
  filtered: InvestmentOrderDto[] = [];
  schemes: SchemeEnrollmentDto[] = [];
  investors: InvestorOption[] = [];
  relationshipMap = new Map<string, AuthFamilyMemberDto>();

  stages = STAGES;

  // ── Filters ───────────────────────────────────────────────────
  investorFilter = 'all';
  schemeFilter = 'all';
  dateFrom = '';
  dateTo = '';

  // ── New Order Modal ──────────────────────────────────────────
  showNewOrderModal = false;
  newOrderForm!: FormGroup;
  submittingOrder = false;
  existingFoliosForSelection: string[] = [];
  useNewFolio = false;

  schemeFilterOptions: { code: string; name: string }[] = [];
  schemeSummaries: { schemeCode: string; schemeName: string; count: number; invested: number }[] = [];

  // ── Drawer (order detail + timeline) ─────────────────────────
  showDrawer = false;
  selectedOrder: InvestmentOrderDto | null = null;
  stageActionValue: Record<string, string> = {};

  constructor(
    private orderService: OrderService,
    private schemeService: SchemeService,
    private authFamilyService: AuthFamilyService,
    private authUserService: AuthUserService,
    private fb: FormBuilder,
    private toastr: ToastrService,
    public auth: AuthService
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadAll();
  }

  initForm(): void {
    this.newOrderForm = this.fb.group({
      investorUserId: ['', Validators.required],
      schemeCode: ['', Validators.required],
      investedAmount: [null, [Validators.required, Validators.min(1)]],
      orderDate: [new Date().toISOString().slice(0, 10), Validators.required],
      paymentMode: ['Online', Validators.required],
      bankName: [''],
      chequeNumber: [''],
      transactionRef: [''],
      folioNumber: ['', Validators.required],
      purchaseNAV: [null, [Validators.required, Validators.min(0.0001)]],
      notes: ['']
    });
  }

  loadAll(): void {
    this.loading = true;
    const canAddOrders = this.auth.canAddOrders();
    const isRegularUser = this.auth.currentUser()?.role === 'User';

    const requests: any = {
      orders: this.orderService.getAll()
    };

    if (canAddOrders) {
      requests['schemes'] = this.schemeService.getApproved();
    }
    if (!isRegularUser) {
      requests['investorUsers'] = this.authUserService.getInvestors();
    }

    forkJoin(requests).subscribe({
      next: (res: any) => {
        this.orders = res.orders;
        
        if (canAddOrders) {
          this.schemes = res.schemes;
        }

        if (isRegularUser) {
          const claims = this.auth.currentUser();
          const fullName = ((claims?.firstName || '') + ' ' + (claims?.lastName || '')).trim() || 'Me';
          this.investors = [{
            userId: claims?.sub || '',
            fullName: fullName,
            relation: 'Self'
          }];
          this.investorFilter = claims?.sub || '';
        } else {
          this.investors = res.investorUsers.map((u: any) => ({
            userId: u.id,
            fullName: u.fullName,
            relation: ''
          }));
          this.loadRelationships();
        }

        this.computeSchemeData();
        this.applyFilters();
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toastr.error('Failed to load orders.');
      }
    });
  }

  private loadRelationships(): void {
    const anyUserId = this.investors[0]?.userId;
    if (!anyUserId) return;

    this.authFamilyService.getRelationshipMapForUser(anyUserId).subscribe({
      next: (map) => {
        this.relationshipMap = map;
        this.investors = this.investors.map(inv => ({
          ...inv,
          relation: map.get(inv.userId)?.relationshipType ?? inv.relation
        }));
      },
      error: () => {
        // Non-fatal — relation labels just won't show
      }
    });
  }

  // ── Filtering ─────────────────────────────────────────────────
  /** Investor + scheme scoped orders, ignoring the date range. */
  private investorSchemeScoped(): InvestmentOrderDto[] {
    return this.orders.filter(o =>
      (this.investorFilter === 'all' || o.investorUserId === this.investorFilter) &&
      (this.schemeFilter === 'all' || o.schemeCode === this.schemeFilter));
  }

  applyFilters(): void {
    let result = this.investorSchemeScoped();
    if (this.dateFrom) {
      result = result.filter(o => o.orderDate >= this.dateFrom);
    }
    if (this.dateTo) {
      result = result.filter(o => o.orderDate <= this.dateTo);
    }
    this.filtered = result;
  }

  onInvestorChange(): void {
    this.computeSchemeData();
    if (!this.schemeFilterOptions.some(s => s.code === this.schemeFilter)) {
      this.schemeFilter = 'all';
    }
    this.applyFilters();
  }

  clearDateRange(): void {
    this.dateFrom = '';
    this.dateTo = '';
    this.applyFilters();
  }

  // ── Investor / scheme option helpers ──────────────────────────
  initials(name: string): string {
    return (name || '?').trim().charAt(0).toUpperCase();
  }

  get allInvestorsLabel(): string {
    const head = this.investors[0]?.fullName;
    return head ? `${head} & Family Members` : 'All Investors';
  }

  // Removed schemeFilterOptions getter, computed in computeSchemeData()

  // ── Stats ─────────────────────────────────────────────────────
  get totalOrders(): number { return this.filtered.length; }
  get totalInvested(): number {
    return this.filtered.reduce((s, o) => s + o.investedAmount, 0);
  }
  get totalUnits(): number {
    return this.filtered.reduce((s, o) => s + (o.unitsAllotted ?? 0), 0);
  }
  get totalFolios(): number {
    return new Set(this.filtered.map(o => o.folioNumber).filter(Boolean)).size;
  }
  private get currentYearMonth(): string {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  }
  get thisMonthLabel(): string {
    return new Date().toLocaleString('en-US', { month: 'short', year: 'numeric' });
  }
  get thisMonthInvested(): number {
    const ym = this.currentYearMonth;
    return this.investorSchemeScoped()
      .filter(o => o.orderDate.startsWith(ym))
      .reduce((s, o) => s + o.investedAmount, 0);
  }

  // ── Scheme-wise summary (clickable filter cards) ───────────────
  computeSchemeData(): void {
    const scope = this.investorFilter === 'all'
      ? this.orders
      : this.orders.filter(o => o.investorUserId === this.investorFilter);

    // 1. Compute schemeFilterOptions
    const filterMap = new Map<string, string>();
    for (const o of scope) {
      filterMap.set(o.schemeCode, o.schemeName);
    }
    this.schemeFilterOptions = Array.from(filterMap, ([code, name]) => ({ code, name }));

    // 2. Compute schemeSummaries
    const summaryMap = new Map<string, { schemeName: string; count: number; invested: number }>();
    for (const o of scope) {
      const cur = summaryMap.get(o.schemeCode) ?? { schemeName: o.schemeName, count: 0, invested: 0 };
      cur.count++;
      cur.invested += o.investedAmount;
      summaryMap.set(o.schemeCode, cur);
    }
    this.schemeSummaries = Array.from(summaryMap.entries()).map(([schemeCode, v]) => ({ schemeCode, ...v }));
  }

  selectSchemeCard(schemeCode: string): void {
    this.schemeFilter = this.schemeFilter === schemeCode ? 'all' : schemeCode;
    this.applyFilters();
  }

  get ordersTitle(): string {
    let title = this.investorFilter === 'all'
      ? 'All orders'
      : `${this.investors.find(i => i.userId === this.investorFilter)?.fullName ?? 'Investor'}'s orders`;
    if (this.schemeFilter !== 'all') {
      const name = this.schemeFilterOptions.find(s => s.code === this.schemeFilter)?.name;
      if (name) title += ' · ' + name;
    }
    return title;
  }

  // ── Display helpers ───────────────────────────────────────────
  displayStatus(status: OrderStatus): { label: string; cls: string } {
    switch (status) {
      case 'Requested': return { label: 'Requested', cls: 'requested' };
      case 'Assigned':  return { label: 'Assigned',  cls: 'assigned' };
      case 'Submitted': return { label: 'Submitted', cls: 'submitted' };
      case 'Verified':
      case 'Active':    return { label: 'Active',    cls: 'active' };
      case 'Cancelled': return { label: 'Cancelled',  cls: 'cancelled' };
      default:          return { label: status, cls: '' };
    }
  }

  setPaymentMode(mode: 'Online' | 'Cheque'): void {
    this.newOrderForm.get('paymentMode')?.setValue(mode);
  }

  // ── New Order Modal ───────────────────────────────────────────
  openNewOrderModal(): void {
    this.newOrderForm.reset({
      orderDate: new Date().toISOString().slice(0, 10),
      paymentMode: 'Online'
    });
    this.useNewFolio = false;
    this.existingFoliosForSelection = [];
    this.showNewOrderModal = true;
  }

  closeNewOrderModal(): void {
    this.showNewOrderModal = false;
  }

  onInvestorOrSchemeChanged(): void {
    const investorUserId = this.newOrderForm.get('investorUserId')?.value;
    const schemeCode = this.newOrderForm.get('schemeCode')?.value;
    if (!investorUserId || !schemeCode) {
      this.existingFoliosForSelection = [];
      this.useNewFolio = false;
      return;
    }
    this.existingFoliosForSelection = Array.from(new Set(
      this.orders
        .filter(o => o.investorUserId === investorUserId && o.schemeCode === schemeCode && o.folioNumber)
        .map(o => o.folioNumber as string)
    ));
    // Commit a valid default so the folio <select> isn't left uncommitted
    // (a native select's initial [value] fires no change event).
    if (this.existingFoliosForSelection.length) {
      this.useNewFolio = false;
      const current = this.newOrderForm.get('folioNumber')?.value;
      if (!this.existingFoliosForSelection.includes(current)) {
        this.newOrderForm.get('folioNumber')?.setValue(this.existingFoliosForSelection[0]);
      }
    } else {
      this.useNewFolio = false;
    }
  }

  onFolioSelect(value: string): void {
    if (value === '__new__') {
      this.useNewFolio = true;
      this.newOrderForm.get('folioNumber')?.setValue('');
    } else {
      this.newOrderForm.get('folioNumber')?.setValue(value);
    }
  }

  get previewUnits(): number | null {
    const amount = this.newOrderForm.get('investedAmount')?.value;
    const nav = this.newOrderForm.get('purchaseNAV')?.value;
    if (!amount || !nav || nav <= 0) return null;
    return Math.round((amount / nav) * 1e6) / 1e6;
  }

  submitNewOrder(): void {
    if (this.newOrderForm.invalid) {
      this.newOrderForm.markAllAsTouched();
      const missing = this.missingRequiredLabels();
      this.toastr.warning(
        missing.length
          ? `Please complete: ${missing.join(', ')}.`
          : 'Please complete the required fields.');
      return;
    }

    const v = this.newOrderForm.value;
    const investor = this.investors.find(i => i.userId === v.investorUserId);
    const scheme = this.schemes.find(s => s.schemeCode === v.schemeCode);

    if (!scheme) {
      this.toastr.error('Please select a valid scheme.');
      return;
    }

    this.submittingOrder = true;

    this.orderService.create({
      investorUserId: v.investorUserId,
      investorName: investor?.fullName ?? v.investorUserId,
      schemeCode: scheme.schemeCode,
      schemeName: scheme.schemeName,
      fundName: scheme.fundName,
      investedAmount: v.investedAmount,
      paymentMode: v.paymentMode,
      chequeNumber: v.paymentMode === 'Cheque' ? v.chequeNumber : null,
      transactionRef: v.paymentMode !== 'Cheque' ? v.transactionRef : null,
      bankName: v.bankName || null,
      orderDate: v.orderDate,
      purchaseNAV: v.purchaseNAV,
      folioNumber: v.folioNumber,
      notes: v.notes || null
    }).subscribe({
      next: (order) => {
        this.orders = [order, ...this.orders];
        this.computeSchemeData();
        this.applyFilters();
        this.toastr.success(`Order ${order.orderNumber} logged successfully.`);
        this.submittingOrder = false;
        this.closeNewOrderModal();
      },
      error: (err) => {
        this.submittingOrder = false;
        this.toastr.error(err?.error?.error ?? 'Failed to create order.');
      }
    });
  }

  private missingRequiredLabels(): string[] {
    const labels: Record<string, string> = {
      investorUserId: 'Investor',
      schemeCode: 'Scheme',
      investedAmount: 'Amount',
      orderDate: 'Order date',
      paymentMode: 'Payment mode',
      folioNumber: 'Folio',
      purchaseNAV: 'Purchase NAV'
    };
    return Object.keys(labels)
      .filter(k => this.newOrderForm.get(k)?.invalid)
      .map(k => labels[k]);
  }

  // ── Drawer / Timeline ─────────────────────────────────────────
  openDrawer(order: InvestmentOrderDto): void {
    this.selectedOrder = order;
    this.stageActionValue = {};
    this.showDrawer = true;
  }

  closeDrawer(): void {
    this.showDrawer = false;
    this.selectedOrder = null;
  }

  stageIndex(status: OrderStatus): number {
    if (status === 'Active') return STAGES.length; // past all 4
    if (status === 'Cancelled') return -1;
    return STAGES.findIndex(s => s.key === status);
  }

  isStageDone(order: InvestmentOrderDto, stage: StageInfo): boolean {
    return this.stageIndex(order.status) > STAGES.findIndex(s => s.key === stage.key);
  }

  isStageCurrent(order: InvestmentOrderDto, stage: StageInfo): boolean {
    return order.status === stage.key;
  }

  stageDetail(order: InvestmentOrderDto, stage: StageInfo): string {
    let raw: string | null | undefined;
    let waiting = 'Pending';
    switch (stage.key) {
      case 'Requested': raw = order.orderDate;   waiting = 'Instruction logged, awaiting field visit'; break;
      case 'Assigned':  raw = order.assignedDate; waiting = 'Staff assigned, visit pending'; break;
      case 'Submitted': raw = order.submittedDate; waiting = 'Form & cheque submitted, pending verification'; break;
      case 'Verified':  raw = order.verifiedDate ?? order.activatedDate; waiting = 'Verified and reported to family'; break;
    }
    if (raw) {
      return new Date(raw).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
    }
    return this.isStageCurrent(order, stage) ? waiting : 'Pending';
  }

  get nextAction(): StageInfo | null {
    if (!this.selectedOrder) return null;
    const idx = this.stageIndex(this.selectedOrder.status);
    const nextIdx = idx + 1;
    if (idx < 0 || nextIdx >= STAGES.length) return null;
    return STAGES[nextIdx];
  }

  advanceOrder(): void {
    if (!this.selectedOrder || !this.nextAction) return;
    const order = this.selectedOrder;
    const stage = this.nextAction.key;

    if (stage === 'Assigned') {
      this.orderService.updateStatus(order.id, {
        newStatus: 'Assigned',
        assignedDate: new Date().toISOString().slice(0, 10),
        assignedStaffName: this.stageActionValue['staff'] || null
      }).subscribe(this.stageUpdateHandlers(order));
    } else if (stage === 'Submitted') {
      this.orderService.updateStatus(order.id, {
        newStatus: 'Submitted',
        submittedDate: new Date().toISOString().slice(0, 10),
        reference: this.stageActionValue['reference'] || null
      }).subscribe(this.stageUpdateHandlers(order));
    } else if (stage === 'Verified') {
      // Verified cascades to Active server-side in one call
      this.orderService.updateStatus(order.id, {
        newStatus: 'Verified',
        verifiedDate: new Date().toISOString().slice(0, 10)
      }).subscribe(this.stageUpdateHandlers(order, true));
    }
  }

  private stageUpdateHandlers(order: InvestmentOrderDto, isFinal = false) {
    return {
      next: (updated: InvestmentOrderDto) => {
        const idx = this.orders.findIndex(o => o.id === updated.id);
        if (idx !== -1) this.orders[idx] = updated;
        this.selectedOrder = updated;
        this.stageActionValue = {};
        this.computeSchemeData();
        this.applyFilters();
        this.toastr.success(
          isFinal
            ? `Order ${updated.orderNumber} verified and active in ${updated.investorName}'s portfolio.`
            : `Order ${updated.orderNumber} moved to ${updated.status}.`);
      },
      error: (err: any) => {
        this.toastr.error(err?.error?.error ?? 'Failed to update order status.');
      }
    };
  }

  cancelOrder(): void {
    if (!this.selectedOrder) return;
    if (!confirm(`Cancel order ${this.selectedOrder.orderNumber}?`)) return;

    this.orderService.updateStatus(this.selectedOrder.id, {
      newStatus: 'Cancelled',
      notes: this.stageActionValue['cancelReason'] || null
    }).subscribe(this.stageUpdateHandlers(this.selectedOrder));
  }

  @HostListener('document:keydown.escape')
  handleEscapeKey(): void {
    if (this.showDrawer) {
      this.closeDrawer();
    }
    if (this.showNewOrderModal) {
      this.closeNewOrderModal();
    }
  }
}
