import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';

import { UserService } from '../../../core/services/user.service';
import { UserDto } from '../../../core/models/user.model';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-pending',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './pending.component.html',
  styleUrl: './pending.component.scss'
})
export class PendingComponent implements OnInit {
  pendingUsers: UserDto[] = [];
  loading = true;
  rejectingUser: UserDto | null = null;
  rejectReason = new FormControl('');
  showRejectModal = false;
  approvingAll = false;

  constructor(private userService: UserService, private toastr: ToastrService) {}

  ngOnInit(): void {
    this.loadPending();
  }

  loadPending(): void {
    this.loading = true;
    this.userService.getPending().subscribe({
      next: users => {
        this.pendingUsers = users;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toastr.error('Failed to load pending users.');
      }
    });
  }

  approveUser(user: UserDto): void {
    this.userService.approve(user.id).subscribe({
      next: () => {
        this.pendingUsers = this.pendingUsers.filter(u => u.id !== user.id);
        this.toastr.success(`${user.fullName} approved successfully.`);
      },
      error: () => this.toastr.error('Failed to approve user.')
    });
  }

  openRejectModal(user: UserDto): void {
    this.rejectingUser = user;
    this.showRejectModal = true;
    this.rejectReason.setValue('');
  }

  closeRejectModal(): void {
    this.showRejectModal = false;
    this.rejectingUser = null;
  }

  confirmReject(): void {
    if (!this.rejectingUser) return;
    const user = this.rejectingUser;
    const reason = this.rejectReason.value || 'Rejected by Admin';

    this.userService.reject(user.id, { reason }).subscribe({
      next: () => {
        this.pendingUsers = this.pendingUsers.filter(u => u.id !== user.id);
        this.showRejectModal = false;
        this.rejectingUser = null;
        this.toastr.warning(`${user.fullName} rejected.`);
      },
      error: () => this.toastr.error('Failed to reject user.')
    });
  }

  approveAll(): void {
    if (!this.pendingUsers.length) return;
    if (!confirm(`Approve all ${this.pendingUsers.length} pending users?`)) return;

    this.approvingAll = true;
    const calls = this.pendingUsers.map(u => this.userService.approve(u.id));
    let completed = 0;
    let failed = 0;

    calls.forEach(call => {
      call.subscribe({
        next: () => {
          completed++;
          if (completed + failed === calls.length) this.finishApproveAll(completed, failed);
        },
        error: () => {
          failed++;
          if (completed + failed === calls.length) this.finishApproveAll(completed, failed);
        }
      });
    });
  }

  private finishApproveAll(completed: number, failed: number): void {
    this.approvingAll = false;
    this.loadPending();
    if (failed === 0) {
      this.toastr.success(`All ${completed} users approved.`);
    } else {
      this.toastr.warning(`${completed} approved, ${failed} failed.`);
    }
  }
}
