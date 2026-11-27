import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { forkJoin } from 'rxjs';

import { OrderService } from '../../core/services/order.service';
import { SchemeService } from '../../core/services/scheme.service';
import { AuthUserService } from '../../core/services/auth-user.service';
import {
  BulkImportPreviewResponseDto,
  BulkImportPreviewRowDto,
  BulkImportConfirmRowDto,
  BulkImportConfirmResultDto
} from '../../core/models/order.model';
import { SchemeEnrollmentDto } from '../../core/models/scheme.model';
import { AuthUserDto } from '../../core/models/auth-user.model';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-bulk-import',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinnerComponent],
  templateUrl: './bulk-import.component.html',
  styleUrls: ['./bulk-import.component.scss']
})
export class BulkImportComponent implements OnInit {
  step: 1 | 2 | 3 = 1;

  selectedFile: File | null = null;
  casPassword = '';
  showPassword = false;

  parsing = false;
  importing = false;

  schemes: SchemeEnrollmentDto[] = [];
  investors: AuthUserDto[] = [];

  preview: BulkImportPreviewResponseDto | null = null;
  rows: BulkImportPreviewRowDto[] = [];
  importResult: BulkImportConfirmResultDto | null = null;

  constructor(
    private orderService: OrderService,
    private schemeService: SchemeService,
    private authUserService: AuthUserService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    forkJoin({
      schemes: this.schemeService.getApproved(),
      investors: this.authUserService.getInvestors()
    }).subscribe({
      next: ({ schemes, investors }) => {
        this.schemes = schemes;
        this.investors = investors;
      },
      error: () => { /* non-fatal */ }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
    }
  }

  parseAndPreview(): void {
    if (!this.selectedFile) {
      this.toastr.warning('Please select a CAMS/KFintech CAS PDF file first.');
      return;
    }

    this.parsing = true;
    this.orderService.previewBulkCas(this.selectedFile, this.casPassword).subscribe({
      next: (res) => {
        this.parsing = false;
        this.preview = res;
        this.rows = (res.rows ?? []).map(r => ({ ...r, selected: r.includeByDefault }));
        this.step = 2;
        if (this.rows.length === 0) {
          this.toastr.info('No purchase transactions were found in this CAS PDF.');
        }
      },
      error: (err) => {
        this.parsing = false;
        this.toastr.error(err.error?.error || 'Failed to parse CAS PDF.');
      }
    });
  }

  get selectedCount(): number {
    return this.rows.filter(r => r.selected && !!r.schemeCode && !!r.investorUserId).length;
  }

  get selectedTotalAmount(): number {
    return this.rows
      .filter(r => r.selected && !!r.schemeCode && !!r.investorUserId)
      .reduce((sum, r) => sum + (Number(r.investedAmount) || 0), 0);
  }

  get readyCount(): number {
    return this.rows.filter(r => r.status === 'Ready' || r.status === 'Warning').length;
  }

  get issueCount(): number {
    return this.rows.filter(r => r.status === 'Issue' || r.status === 'Unmatched').length;
  }

  get duplicateCount(): number {
    return this.rows.filter(r => r.status === 'Duplicate').length;
  }

  autoFixAmfiNav(row: BulkImportPreviewRowDto): void {
    if (!row.amfiNav || row.amfiNav <= 0) return;
    row.purchaseNAV = row.amfiNav;
    row.unitsAllotted = row.investedAmount > 0 ? Number((row.investedAmount / row.amfiNav).toFixed(6)) : row.unitsAllotted;
    row.variancePercent = 0;
    row.status = 'Ready';
    row.statusNote = `Auto-fixed to AMFI NAV (${row.amfiNav.toFixed(4)})`;
    row.selected = true;
    this.toastr.success(`Row #${row.rowNumber} updated to AMFI NAV.`);
  }

  onSchemeChanged(row: BulkImportPreviewRowDto, schemeCode: string): void {
    const sch = this.schemes.find(s => s.schemeCode === schemeCode);
    if (!sch) return;
    row.schemeCode = sch.schemeCode;
    row.schemeName = sch.schemeName;
    row.fundName = sch.fundName || '';
    if (row.investorUserId) {
      row.status = 'Ready';
      row.statusNote = 'Manually matched scheme';
      row.selected = true;
    }
  }

  onInvestorChanged(row: BulkImportPreviewRowDto, userId: string): void {
    const inv = this.investors.find(i => (i.panNumber || i.id) === userId || i.id === userId);
    if (!inv) return;
    row.investorUserId = inv.panNumber || inv.id;
    row.investorName = inv.fullName || `${inv.firstName} ${inv.lastName}`.trim();
    if (row.schemeCode) {
      row.status = 'Ready';
      row.statusNote = 'Manually matched investor';
      row.selected = true;
    }
  }

  confirmImport(): void {
    const toImport: BulkImportConfirmRowDto[] = this.rows
      .filter(r => r.selected && !!r.schemeCode && !!r.investorUserId)
      .map(r => ({
        orderDate: r.orderDate,
        investorUserId: r.investorUserId,
        investorName: r.investorName,
        schemeCode: r.schemeCode,
        schemeName: r.schemeName,
        fundName: r.fundName,
        folioNumber: r.folioNumber || 'PENDING',
        investedAmount: Number(r.investedAmount),
        purchaseNAV: Number(r.purchaseNAV),
        unitsAllotted: Number(r.unitsAllotted),
        paymentMode: r.paymentMode || 'Online'
      }));

    if (toImport.length === 0) {
      this.toastr.warning('Please select at least one valid row to import.');
      return;
    }

    this.importing = true;
    const note = `Bulk Imported (CAS: ${this.preview?.fileName || ''})`;
    this.orderService.confirmBulkImport(toImport, note).subscribe({
      next: (res) => {
        this.importing = false;
        this.importResult = res;
        this.step = 3;
        this.toastr.success(`Imported ${res.ordersCreated} order(s) successfully!`);
      },
      error: (err) => {
        this.importing = false;
        this.toastr.error(err.error?.error || 'Bulk import failed.');
      }
    });
  }

  resetWizard(): void {
    this.step = 1;
    this.selectedFile = null;
    this.preview = null;
    this.rows = [];
    this.importResult = null;
  }

  goToOrders(): void {
    this.router.navigate(['/orders']);
  }

  formatDate(dateStr: string | null | undefined): string {
    if (!dateStr) return '—';
    return dateStr.slice(0, 10);
  }
}
