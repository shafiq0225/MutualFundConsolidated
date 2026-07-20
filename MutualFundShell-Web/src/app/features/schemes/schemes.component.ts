import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormControl, Validators, ReactiveFormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';

import { SchemeService } from '../../core/services/scheme.service';
import { SchemeEnrollmentDto } from '../../core/models/scheme.model';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { StatCardComponent } from '../../shared/components/stat-card/stat-card.component';

@Component({
  selector: 'app-schemes',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    LoadingSpinnerComponent,
    StatCardComponent
  ],
  templateUrl: './schemes.component.html',
  styleUrls: ['./schemes.component.scss']
})
export class SchemesComponent implements OnInit {
  // Data
  allSchemes: SchemeEnrollmentDto[] = [];
  filtered: SchemeEnrollmentDto[] = [];
  expandedCodes = new Set<string>();

  // UI state
  loading = true;
  showCreateModal = false;
  showEditModal = false;
  editingScheme: SchemeEnrollmentDto | null = null;
  submitting = false;

  // Filters
  searchCtrl = new FormControl('');
  statusFilter = new FormControl('all');

  // Forms
  createForm!: FormGroup;
  editForm!: FormGroup;

  constructor(
    private schemeService: SchemeService,
    private fb: FormBuilder,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.initForms();
    this.loadSchemes();
    this.setupFilters();
  }

  initForms(): void {
    this.createForm = this.fb.group({
      schemeCode: ['', [
        Validators.required,
        Validators.minLength(3),
        Validators.maxLength(20),
        Validators.pattern('^[0-9]+$')
      ]],
      schemeName: ['', [
        Validators.required,
        Validators.minLength(5)
      ]],
      isApproved: [false]
    });

    this.editForm = this.fb.group({
      schemeName: ['', [Validators.required, Validators.minLength(5)]],
      isApproved: [false]
    });
  }

  loadSchemes(): void {
    this.loading = true;
    this.schemeService.getAll().subscribe({
      next: (schemes) => {
        this.allSchemes = schemes;
        this.applyFilters();
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.toastr.error('Failed to load schemes.');
        this.cdr.detectChanges();
      }
    });
  }

  setupFilters(): void {
    this.searchCtrl.valueChanges.subscribe(() => this.applyFilters());
    this.statusFilter.valueChanges.subscribe(() => this.applyFilters());
  }

  applyFilters(): void {
    let result = [...this.allSchemes];
    const search = this.searchCtrl.value?.toLowerCase() || '';
    const status = this.statusFilter.value || 'all';

    if (search) {
      result = result.filter(s =>
        s.schemeCode.toLowerCase().includes(search) ||
        s.schemeName.toLowerCase().includes(search)
      );
    }

    if (status === 'active') {
      result = result.filter(s => s.isApproved);
    } else if (status === 'inactive') {
      result = result.filter(s => !s.isApproved);
    }

    this.filtered = result;
  }

  clearFilters(): void {
    this.searchCtrl.setValue('');
    this.statusFilter.setValue('all');
  }

  // ── Create ────────────────────────────────────────────────────
  openCreateModal(): void {
    this.createForm.reset({ isApproved: false });
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  submitCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }
    this.submitting = true;

    this.schemeService.create(this.createForm.value).subscribe({
      next: (scheme) => {
        this.allSchemes = [...this.allSchemes, scheme];
        this.applyFilters();
        this.toastr.success(
          `Scheme '${scheme.schemeCode}' enrolled successfully.`);
        this.closeCreateModal();
        this.submitting = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastr.error('Failed to enroll scheme.');
        this.submitting = false;
      }
    });
  }

  // ── Edit ──────────────────────────────────────────────────────
  openEditModal(scheme: SchemeEnrollmentDto): void {
    this.editingScheme = scheme;
    this.editForm.patchValue({
      schemeName: scheme.schemeName,
      isApproved: scheme.isApproved
    });
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.editingScheme = null;
  }

  submitEdit(): void {
    if (!this.editingScheme || this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }
    this.submitting = true;

    this.schemeService.update(
      this.editingScheme.schemeCode,
      this.editForm.value
    ).subscribe({
      next: (updated) => {
        const idx = this.allSchemes.findIndex(
          s => s.schemeCode === updated.schemeCode);
        if (idx !== -1) this.allSchemes[idx] = updated;
        this.applyFilters();
        this.toastr.success('Scheme updated successfully.');
        this.closeEditModal();
        this.submitting = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastr.error('Failed to update scheme.');
        this.submitting = false;
      }
    });
  }

  // ── Toggle Approval ───────────────────────────────────────────
  toggleApproval(scheme: SchemeEnrollmentDto): void {
    const newStatus = !scheme.isApproved;
    const action = newStatus ? 'approve' : 'deactivate';

    if (!confirm(
      `${newStatus ? 'Approve' : 'Deactivate'} scheme '${scheme.schemeName}'?`
    )) return;

    this.schemeService.update(scheme.schemeCode, {
      schemeName: scheme.schemeName,
      isApproved: newStatus
    }).subscribe({
      next: (updated) => {
        const idx = this.allSchemes.findIndex(
          s => s.schemeCode === updated.schemeCode);
        if (idx !== -1) this.allSchemes[idx] = updated;
        this.applyFilters();
        this.toastr.success(
          `Scheme ${newStatus ? 'approved' : 'deactivated'} successfully.`);
        this.cdr.detectChanges();
      },
      error: () => this.toastr.error(`Failed to ${action} scheme.`)
    });
  }

  // ── Helpers ───────────────────────────────────────────────────
  get cf() { return this.createForm.controls; }
  get ef() { return this.editForm.controls; }

  get totalCount(): number {
    return this.allSchemes.length;
  }
  get activeCount(): number {
    return this.allSchemes.filter(s => s.isApproved).length;
  }
  get inactiveCount(): number {
    return this.allSchemes.filter(s => !s.isApproved).length;
  }

  // Accordion (mobile)
  toggleExpand(code: string): void {
    this.expandedCodes.has(code)
      ? this.expandedCodes.delete(code)
      : this.expandedCodes.add(code);
  }
}
