export interface HoldingDto {
  id: number;
  orderId: number;
  orderNumber: string;
  investorUserId: string;
  investorName: string;
  schemeCode: string;
  schemeName: string;
  fundName: string;
  folioNumber: string;
  purchaseDate: string;
  purchaseYear: number;
  purchaseNAV: number;
  investedAmount: number;
  units: number;
  currentNAV: number;
  currentValue: number;
  profitLoss: number;
  profitLossPercent: number;
  isProfit: boolean;
  lastUpdatedDate?: string | null;
  isActive: boolean;
}
