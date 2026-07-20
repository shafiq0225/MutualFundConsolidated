import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { forkJoin } from 'rxjs';

import { UserService } from '../../core/services/user.service';
import { PermissionService } from '../../core/services/permission.service';
import { UpdateRoleDto, USER_ROLE_VALUES, UserDto, UserRoleName } from '../../core/models/user.model';
import { PermissionDto } from '../../core/models/permission.model';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

const ROLE_OPTIONS: UserRoleName[] = ['Admin', 'Employee', 'User'];

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent implements OnInit {
  allUsers: UserDto[] = [];
  filtered: UserDto[] = [];
  permissions: PermissionDto[] = [];
  roleOptions = ROLE_OPTIONS;

  loading = true;
  selectedUser: UserDto | null = null;
  showRoleModal = false;
  showPermModal = false;
  userPermCodes: string[] = [];
  permBusy = false;

  searchCtrl = new FormControl('');
  roleFilter = new FormControl('all');
  statusFilter = new FormControl('all');

  selectedRoleName: UserRoleName = 'User';

  constructor(
    private userService: UserService,
    private permissionService: PermissionService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadData();
    this.setupFilters();
  }

  loadData(): void {
    this.loading = true;
    forkJoin({
      users: this.userService.getAll(),
      permissions: this.permissionService.getAll()
    }).subscribe({
      next: ({ users, permissions }) => {
        this.allUsers = users;
        this.permissions = permissions;
        this.applyFilters();
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toastr.error('Failed to load users.');
      }
    });
  }

  setupFilters(): void {
    this.searchCtrl.valueChanges.pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => this.applyFilters());
    this.roleFilter.valueChanges.subscribe(() => this.applyFilters());
    this.statusFilter.valueChanges.subscribe(() => this.applyFilters());
  }

  applyFilters(): void {
    let result = [...this.allUsers];
    const search = this.searchCtrl.value?.toLowerCase() || '';
    const role = this.roleFilter.value || 'all';
    const status = this.statusFilter.value || 'all';

    if (search) {
      result = result.filter(u =>
        u.fullName.toLowerCase().includes(search) ||
        u.email.toLowerCase().includes(search) ||
        u.panNumber.toLowerCase().includes(search));
    }
    if (role !== 'all') result = result.filter(u => u.roleName === role);
    if (status !== 'all') result = result.filter(u => u.statusName === status);

    this.filtered = result;
  }

  clearFilters(): void {
    this.searchCtrl.setValue('');
    this.roleFilter.setValue('all');
    this.statusFilter.setValue('all');
  }

  approveUser(user: UserDto): void {
    this.userService.approve(user.id).subscribe({
      next: updated => {
        this.updateUserInList(updated);
        this.toastr.success(`${user.fullName} approved.`);
      },
      error: () => this.toastr.error('Failed to approve user.')
    });
  }

  rejectUser(user: UserDto): void {
    this.userService.reject(user.id, { reason: 'Rejected by Admin' }).subscribe({
      next: updated => {
        this.updateUserInList(updated);
        this.toastr.warning(`${user.fullName} rejected.`);
      },
      error: () => this.toastr.error('Failed to reject user.')
    });
  }

  deactivateUser(user: UserDto): void {
    if (!confirm(`Deactivate ${user.fullName}?`)) return;
    this.userService.reject(user.id, { reason: 'Deactivated by Admin' }).subscribe({
      next: updated => {
        this.updateUserInList(updated);
        this.toastr.warning(`${user.fullName} deactivated.`);
      },
      error: () => this.toastr.error('Failed to deactivate user.')
    });
  }

  // ── Role modal ─────────────────────────────────────────────────
  openRoleModal(user: UserDto): void {
    this.selectedUser = user;
    this.selectedRoleName = user.roleName;
    this.showRoleModal = true;
  }

  closeRoleModal(): void {
    this.showRoleModal = false;
    this.selectedUser = null;
  }

  saveRole(): void {
    if (!this.selectedUser) return;
    const dto: UpdateRoleDto = { newRole: USER_ROLE_VALUES[this.selectedRoleName] };
    this.userService.updateRole(this.selectedUser.id, dto).subscribe({
      next: updated => {
        this.updateUserInList(updated);
        this.toastr.success('Role updated successfully.');
        this.closeRoleModal();
      },
      error: () => this.toastr.error('Failed to update role.')
    });
  }

  // ── Permission modal ───────────────────────────────────────────
  openPermModal(user: UserDto): void {
    this.selectedUser = user;
    this.showPermModal = true;
    this.userPermCodes = [];

    this.permissionService.getUserPermissions(user.id).subscribe({
      next: res => (this.userPermCodes = res.permissions.map(p => p.code)),
      error: () => this.toastr.error('Failed to load permissions.')
    });
  }

  closePermModal(): void {
    this.showPermModal = false;
    this.selectedUser = null;
    this.userPermCodes = [];
  }

  hasPermission(code: string): boolean {
    return this.userPermCodes.includes(code);
  }

  togglePermission(code: string): void {
    if (!this.selectedUser || this.permBusy) return;
    this.permBusy = true;
    const userId = this.selectedUser.id;

    if (this.hasPermission(code)) {
      const codesToRevoke: string[] = [code];

      if (code === 'order.view' && this.hasPermission('order.add')) {
        codesToRevoke.push('order.add');
      } else if (code === 'investor.view' && this.hasPermission('investor.snapshot')) {
        codesToRevoke.push('investor.snapshot');
      }

      const requests = codesToRevoke.map(c =>
        this.permissionService.revoke({ userId, permissionCode: c })
      );

      forkJoin(requests).subscribe({
        next: () => {
          this.userPermCodes = this.userPermCodes.filter(c => !codesToRevoke.includes(c));
          this.permBusy = false;
          codesToRevoke.forEach(c => this.toastr.info(`Permission '${c}' revoked.`));
        },
        error: () => {
          this.permBusy = false;
          this.toastr.error('Failed to revoke permission.');
        }
      });
    } else {
      const codesToAssign: string[] = [code];

      if (code === 'order.add' && !this.hasPermission('order.view')) {
        codesToAssign.push('order.view');
      } else if (code === 'investor.snapshot' && !this.hasPermission('investor.view')) {
        codesToAssign.push('investor.view');
      }

      const requests = codesToAssign.map(c =>
        this.permissionService.assign({ userId, permissionCode: c })
      );

      forkJoin(requests).subscribe({
        next: () => {
          this.userPermCodes = [...this.userPermCodes, ...codesToAssign];
          this.permBusy = false;
          codesToAssign.forEach(c => this.toastr.success(`Permission '${c}' assigned.`));
        },
        error: () => {
          this.permBusy = false;
          this.toastr.error('Failed to assign permission.');
        }
      });
    }
  }

  private updateUserInList(updated: UserDto): void {
    const idx = this.allUsers.findIndex(u => u.id === updated.id);
    if (idx !== -1) {
      this.allUsers[idx] = updated;
      this.applyFilters();
    }
    if (this.selectedUser?.id === updated.id) this.selectedUser = updated;
  }

  get totalCount(): number { return this.allUsers.length; }
  get approvedCount(): number { return this.allUsers.filter(u => u.statusName === 'Approved').length; }
  get pendingCount(): number { return this.allUsers.filter(u => u.statusName === 'Pending').length; }
  get rejectedCount(): number { return this.allUsers.filter(u => u.statusName === 'Rejected').length; }

  roleBadgeClass(role: string): string {
    switch (role) {
      case 'Admin': return 'badge-ink';
      case 'Employee': return 'badge-steel';
      default: return 'badge-muted';
    }
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'Approved': return 'badge-gain';
      case 'Pending': return 'badge-gold';
      default: return 'badge-loss';
    }
  }
}
