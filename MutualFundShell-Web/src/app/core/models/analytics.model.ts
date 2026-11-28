export interface AnalyticsHubResponseDto {
  years: number[];
  yearMonthsElapsed: Record<number, number>;
  totalInvested: number;
  totalMarketValue: number;
  totalProfitLoss: number;
  overallReturnPercent: number;
  sipStepUp: SipStepUpSummaryDto;
  yearWise: YearWiseAnalyticsDto;
  monthlyLedger: MonthlyLedgerYearDto[];
  headToHead: HeadToHeadComparisonDto;
}

export interface SipStepUpSummaryDto {
  years: number[];
  firstYear: number;
  prevYear: number;
  currentYear: number;
  rows: SipStepUpSchemeRowDto[];
  totals: SipStepUpSchemeRowDto;
}

export interface SipStepUpSchemeRowDto {
  schemeCode: string;
  schemeName: string;
  yearlyTotals: Record<number, number>;
  grandTotal: number;
  avgMonthlyByYear: Record<number, number>;
  prevToCurrStepUpRs: number;
  prevToCurrStepUpPercent: number;
  currVsPrevAnnualisedPercent: number;
  monthlyIncrementRs: number;
  monthlyIncrementPercent: number;
  annualisedRunRate: number;
}

export interface YearWiseAnalyticsDto {
  summary: YearWiseSummaryRowDto[];
  breakdowns: YearSchemeBreakdownDto[];
}

export interface YearWiseSummaryRowDto {
  year: number;
  investedAmount: number;
  shareOfTotalPercent: number;
  cumulativeInvested: number;
  status: string;
}

export interface YearSchemeBreakdownDto {
  year: number;
  monthsElapsed: number;
  totalInvested: number;
  avgMonthlySip: number;
  schemes: YearSchemeItemDto[];
}

export interface YearSchemeItemDto {
  schemeCode: string;
  schemeName: string;
  investedAmount: number;
  shareOfYearPercent: number;
  avgMonthlySip: number;
}

export interface MonthlyLedgerYearDto {
  year: number;
  yearTotal: number;
  monthlyTotals: number[];
  schemes: MonthlyLedgerSchemeRowDto[];
}

export interface MonthlyLedgerSchemeRowDto {
  schemeCode: string;
  schemeName: string;
  months: number[];
  orderCounts: number[];
  yearTotal: number;
}

export interface HeadToHeadComparisonDto {
  schemes: HeadToHeadSchemeRowDto[];
  categories: HeadToHeadCategoryRowDto[];
}

export interface HeadToHeadSchemeRowDto {
  schemeCode: string;
  schemeName: string;
  assetCategory: string;
  assetClassGroup: string;
  investedCapital: number;
  marketValue: number;
  profitLoss: number;
  returnPercent: number;
  portfolioWeightPercent: number;
  valuationSharePercent: number;
  profitSharePercent: number;
}

export interface HeadToHeadCategoryRowDto {
  assetCategory: string;
  schemeCount: number;
  investedCapital: number;
  marketValue: number;
  profitLoss: number;
  returnPercent: number;
  categorySharePercent: number;
}

export interface AnalyticsMonthOrderDto {
  id: number;
  orderNumber: string;
  orderDate: string;
  investorName: string;
  schemeName: string;
  folioNumber: string;
  investedAmount: number;
  purchaseNAV?: number | null;
  unitsAllotted?: number | null;
  paymentMode: string;
}
