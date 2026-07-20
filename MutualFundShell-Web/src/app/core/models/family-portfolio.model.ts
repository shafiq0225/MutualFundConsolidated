export interface QuickReturnDto {
  label: string;
  returnPercent: number;
  periodGainAmount: number;
  cagrPercent?: number;
  isPositive: boolean;
  hasData: boolean;
  isPartialPeriod?: boolean;
  actualFromDate?: string;
}

export interface MemberSummaryDto {
  investorUserId: string;
  investorName: string;
  totalInvested: number;
  totalCurrentValue: number;
  totalGain: number;
  totalGainPercent: number;
  isGain: boolean;
  schemeCount: number;
  holdingCount: number;
  dayBefore?: QuickReturnDto | null;
  yesterday?: QuickReturnDto | null;
  thisWeek?: QuickReturnDto | null;
  oneMonth?: QuickReturnDto | null;
  oneYear?: QuickReturnDto | null;
  threeYear?: QuickReturnDto | null;
  fiveYear?: QuickReturnDto | null;
}

export interface FamilyOverviewDto {
  totalFamilyInvested: number;
  totalFamilyCurrentValue: number;
  totalFamilyGain: number;
  totalFamilyGainPercent: number;
  isFamilyGain: boolean;
  totalMembers: number;
  totalSchemes: number;
  equitySchemeCount: number;
  debtSchemeCount: number;
  hybridSchemeCount: number;
  familyYesterdayReturn?: QuickReturnDto | null;
  reportDate: Date;
  members: MemberSummaryDto[];
}

export interface HoldingCardDto {
  holdingId: number;
  schemeCode: string;
  schemeName: string;
  fundName: string;
  folioNumber: string;
  orderNumber: string;
  investedAmount: number;
  units: number;
  purchaseNAV: number;
  currentNAV: number;
  currentValue: number;
  gain: number;
  gainPercent: number;
  isGain: boolean;
  dayBefore?: QuickReturnDto | null;
  yesterday?: QuickReturnDto | null;
  thisWeek?: QuickReturnDto | null;
  oneMonth?: QuickReturnDto | null;
  sixMonth?: QuickReturnDto | null;
  oneYear?: QuickReturnDto | null;
  threeYear?: QuickReturnDto | null;
  fiveYear?: QuickReturnDto | null;
}

export interface MemberHoldingsDto {
  investorUserId: string;
  investorName: string;
  initials: string;
  totalInvested: number;
  totalCurrentValue: number;
  totalGain: number;
  totalGainPercent: number;
  isGain: boolean;
  holdings: HoldingCardDto[];
}
