// Mirrors MutualFund.Auth.Application.DTOs.Family exactly.
export interface FamilyMemberDto {
  userId: string;
  fullName: string;
  email: string;
  panNumber: string;
  relationshipType: string; // "Self" | "Spouse" | "Mother" | "Son" | etc.
  displayLabel?: string | null;
  addedAt: string;
}

export interface FamilyGroupDto {
  id: number;
  groupName: string;
  headUserId: string;
  headUserName: string;
  headUserEmail: string;
  headPanNumber: string;
  createdAt: string;
  isActive: boolean;
  members: FamilyMemberDto[]; // real dependents only
  allMembers: FamilyMemberDto[]; // Head (Self) + dependents, unified
  memberCount: number;
}

export interface CreateFamilyGroupDto {
  groupName: string;
  headUserId: string;
}

export interface AddFamilyMemberDto {
  userId: string;
  relationshipType: string; // must match FamilyRelationshipType enum, never "Self"
  displayLabel?: string | null;
}

// Valid relationship types for the Add Member dropdown — mirrors
// MutualFund.Auth.Domain.Enums.FamilyRelationshipType exactly.
// "Self" is excluded — reserved for the Head, never sent by the client.
export const RELATIONSHIP_TYPES = [
  'Spouse', 'Father', 'Mother', 'Son', 'Daughter', 'Brother', 'Sister', 'Other'
] as const;
