// Mirrors MutualFund.Auth.Application.DTOs.User exactly.
// Role/UserType/ApprovalStatus serialize as numbers (no JsonStringEnumConverter
// registered); the *Name fields are computed server-side strings.

export type UserRoleName = 'Admin' | 'Employee' | 'User';
export type UserTypeName = 'None' | 'HeadOfFamily' | 'FamilyMember';
export type ApprovalStatusName = 'Pending' | 'Approved' | 'Rejected';

export interface UserDto {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  panNumber: string;
  role: number;
  roleName: UserRoleName;
  userType: number;
  userTypeName: UserTypeName;
  approvalStatus: number;
  statusName: ApprovalStatusName;
  isActive: boolean;
  createdAt: string;
  approvedAt: string | null;
  lastLoginAt: string | null;
  rejectionReason: string | null;
}

export interface RejectUserDto {
  reason?: string;
}

// Enum values — MutualFund.Auth.Domain.Enums
export const USER_ROLE_VALUES: Record<UserRoleName, number> = {
  Admin: 1,
  Employee: 2,
  User: 3
};

export const USER_TYPE_VALUES: Record<UserTypeName, number> = {
  None: 0,
  HeadOfFamily: 1,
  FamilyMember: 2
};

export const APPROVAL_STATUS_VALUES: Record<ApprovalStatusName, number> = {
  Pending: 0,
  Approved: 1,
  Rejected: 2
};

export interface UpdateRoleDto {
  newRole: number;
}
