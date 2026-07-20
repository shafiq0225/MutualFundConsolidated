import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { forkJoin } from 'rxjs';

import { FamilyService } from '../../core/services/family.service';
import { UserService } from '../../core/services/user.service';
import { FamilyGroupDto, FamilyMemberDto, RELATIONSHIP_TYPES } from '../../core/models/family.model';
import { UserDto } from '../../core/models/user.model';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-family',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './family.component.html',
  styleUrl: './family.component.scss'
})
export class FamilyComponent implements OnInit {
  groups: FamilyGroupDto[] = [];
  eligibleUsers: UserDto[] = [];
  selectedGroup: FamilyGroupDto | null = null;
  relationshipTypes = RELATIONSHIP_TYPES;

  loading = true;
  showCreateModal = false;
  showAddMember = false;
  showDetailPanel = false;
  searchTerm = '';

  createForm!: FormGroup;
  addMemberForm!: FormGroup;

  constructor(
    private familyService: FamilyService,
    private userService: UserService,
    private fb: FormBuilder,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.initForms();
    this.loadData();
  }

  initForms(): void {
    this.createForm = this.fb.group({
      groupName: ['', [Validators.required, Validators.minLength(3)]],
      headUserId: ['', Validators.required]
    });

    this.addMemberForm = this.fb.group({
      userId: ['', Validators.required],
      relationshipType: ['', Validators.required],
      displayLabel: ['']
    });
  }

  loadData(): void {
    this.loading = true;
    forkJoin({
      groups: this.familyService.getAll(),
      users: this.userService.getAll()
    }).subscribe({
      next: ({ groups, users }) => {
        this.groups = groups;
        // Eligible: approved "User"-role accounts not already heading/in a group
        const assignedIds = new Set<string>();
        groups.forEach(g => {
          assignedIds.add(g.headUserId);
          g.members.forEach(m => assignedIds.add(m.userId));
        });
        this.eligibleUsers = users.filter(
          u => u.roleName === 'User' && u.statusName === 'Approved' && !assignedIds.has(u.id)
        );
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toastr.error('Failed to load family groups.');
      }
    });
  }

  get filteredGroups(): FamilyGroupDto[] {
    if (!this.searchTerm) return this.groups;
    const term = this.searchTerm.toLowerCase();
    return this.groups.filter(g =>
      g.groupName.toLowerCase().includes(term) ||
      g.headUserName.toLowerCase().includes(term) ||
      g.headPanNumber.toLowerCase().includes(term));
  }

  // ── Create Group ─────────────────────────────────────────────
  openCreateModal(): void {
    this.createForm.reset();
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
    this.familyService.create(this.createForm.value).subscribe({
      next: group => {
        this.groups = [...this.groups, group];
        this.eligibleUsers = this.eligibleUsers.filter(u => u.id !== group.headUserId);
        this.toastr.success(`Family group '${group.groupName}' created.`);
        this.closeCreateModal();
      },
      error: err => this.toastr.error(err?.error?.error ?? 'Failed to create family group.')
    });
  }

  // ── View Group Detail ────────────────────────────────────────
  openDetail(group: FamilyGroupDto): void {
    this.selectedGroup = group;
    this.showDetailPanel = true;
    this.addMemberForm.reset();
    this.showAddMember = false;
  }

  closeDetail(): void {
    this.showDetailPanel = false;
    this.selectedGroup = null;
  }

  // ── Add Member ────────────────────────────────────────────────
  toggleAddMember(): void {
    this.showAddMember = !this.showAddMember;
    if (!this.showAddMember) this.addMemberForm.reset();
  }

  submitAddMember(): void {
    if (!this.selectedGroup || this.addMemberForm.invalid) {
      this.addMemberForm.markAllAsTouched();
      return;
    }

    const groupId = this.selectedGroup.id;
    const dto = this.addMemberForm.value;

    this.familyService.addMember(groupId, dto).subscribe({
      next: updated => {
        this.selectedGroup = updated;
        const idx = this.groups.findIndex(g => g.id === groupId);
        if (idx !== -1) this.groups[idx] = updated;
        this.eligibleUsers = this.eligibleUsers.filter(u => u.id !== dto.userId);

        this.toastr.success('Member added successfully.');
        this.addMemberForm.reset();
        this.showAddMember = false;
      },
      error: err => this.toastr.error(err?.error?.error ?? 'Failed to add member.')
    });
  }

  // ── Remove Member ────────────────────────────────────────────
  removeMember(member: FamilyMemberDto): void {
    if (!this.selectedGroup) return;
    if (member.relationshipType === 'Self') {
      this.toastr.warning('The Head of Family cannot be removed — delete the group instead.');
      return;
    }
    if (!confirm(`Remove ${member.fullName} from this group?`)) return;

    const groupId = this.selectedGroup.id;
    this.familyService.removeMember(groupId, member.userId).subscribe({
      next: () => {
        if (this.selectedGroup) {
          this.selectedGroup = {
            ...this.selectedGroup,
            members: this.selectedGroup.members.filter(m => m.userId !== member.userId),
            allMembers: this.selectedGroup.allMembers.filter(m => m.userId !== member.userId)
          };
          const idx = this.groups.findIndex(g => g.id === groupId);
          if (idx !== -1) this.groups[idx] = this.selectedGroup;
        }
        this.toastr.warning(`${member.fullName} removed from group.`);
      },
      error: () => this.toastr.error('Failed to remove member.')
    });
  }

  // ── Helpers ───────────────────────────────────────────────────
  get availableUsersForGroup(): UserDto[] {
    return this.eligibleUsers;
  }

  initials(name: string): string {
    const parts = name.trim().split(' ');
    return parts.length >= 2
      ? `${parts[0][0]}${parts[1][0]}`.toUpperCase()
      : name.substring(0, 2).toUpperCase();
  }

  get f() { return this.createForm.controls; }
  get mf() { return this.addMemberForm.controls; }
}
