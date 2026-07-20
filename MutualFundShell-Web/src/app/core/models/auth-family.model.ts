export interface AuthFamilyMemberDto {
  userId: string;
  fullName: string;
  email: string;
  panNumber: string;
  relationshipType: string; // "Self" | "Spouse" | "Mother" | "Son" | etc.
  displayLabel?: string | null;
  addedAt: string;
}

export interface AuthFamilyGroupDto {
  id: number;
  groupName: string;
  headUserId: string;
  headUserName: string;
  headUserEmail: string;
  headPanNumber: string;
  createdAt: string;
  isActive: boolean;
  members: AuthFamilyMemberDto[];
  allMembers: AuthFamilyMemberDto[]; // Head (Self) + dependents, unified
  memberCount: number;
}
