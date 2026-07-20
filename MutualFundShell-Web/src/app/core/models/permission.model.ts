// Mirrors MutualFund.Auth.Application.DTOs.Permission exactly.
export interface PermissionDto {
  id: number;
  code: string;
  name: string;
  description: string;
}

export interface AssignPermissionDto {
  userId: string;
  permissionCode: string;
}

export interface UserPermissionDto {
  userId: string;
  userFullName: string;
  userEmail: string;
  permissions: PermissionDto[];
}
