export interface AuthUserDto {
  id: string;           // PAN — same value used as InvestorUserId in Investment
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  panNumber: string;
  roleName: string;      // "Admin" | "Employee" | "User"
  userTypeName: string;  // "None" | "HeadOfFamily" | "FamilyMember"
  statusName: string;    // "Pending" | "Approved" | "Rejected"
  isActive: boolean;
}
