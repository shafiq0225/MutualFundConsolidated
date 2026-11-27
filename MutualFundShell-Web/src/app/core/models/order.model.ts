export type OrderStatus =
  | 'Requested' | 'Assigned' | 'Submitted' | 'Verified' | 'Active' | 'Cancelled';

export interface InvestmentOrderDto {
  id: number;
  orderNumber: string;
  investorUserId: string;
  investorName: string;
  schemeCode: string;
  schemeName: string;
  fundName: string;
  investedAmount: number;
  paymentMode: string;
  chequeNumber?: string | null;
  transactionRef?: string | null;
  bankName?: string | null;
  orderDate: string;
  assignedDate?: string | null;
  assignedStaffName?: string | null;
  submittedDate?: string | null;
  verifiedDate?: string | null;
  activatedDate?: string | null;
  status: OrderStatus;
  purchaseNAV?: number | null;
  unitsAllotted?: number | null;
  folioNumber?: string | null;
  notes?: string | null;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateOrderDto {
  investorUserId: string;
  investorName: string;
  schemeCode: string;
  schemeName: string;
  fundName: string;
  investedAmount: number;
  paymentMode: string;
  chequeNumber?: string | null;
  transactionRef?: string | null;
  bankName?: string | null;
  orderDate: string;
  purchaseNAV: number;
  folioNumber: string;
  notes?: string | null;
}

export interface UpdateOrderStatusDto {
  newStatus: 'Assigned' | 'Submitted' | 'Verified' | 'Cancelled';
  assignedDate?: string | null;
  assignedStaffName?: string | null;
  submittedDate?: string | null;
  submittedByUserId?: string | null;
  reference?: string | null;
  verifiedDate?: string | null;
  verifiedByUserId?: string | null;
  notes?: string | null;
}

export interface AdminEditOrderDto {
  investorUserId: string;
  investorName: string;
  schemeCode: string;
  schemeName: string;
  fundName: string;
  folioNumber: string;
  orderDate: string;
  investedAmount: number;
  purchaseNAV: number;
  unitsAllotted?: number | null;
  reason?: string | null;
}

export interface AdminReverseOrdersDto {
  orderIds: number[];
  reason: string;
}

export interface OrderImpactItemDto {
  orderId: number;
  orderNumber: string;
  schemeName: string;
  investedAmount: number;
  holdingId?: number | null;
  snapshotCount: number;
  statementCount: number;
}

export interface OrderCorrectionImpactDto {
  orderCount: number;
  holdingCount: number;
  snapshotCount: number;
  statementCount: number;
  items: OrderImpactItemDto[];
}

export interface AdminEditOrderResultDto {
  orderId: number;
  orderNumber: string;
  holdingId?: number | null;
  snapshotsRecalculated: number;
  changeSummary: string;
}

export interface AdminReverseOrdersResultDto {
  ordersRemoved: number;
  holdingsRemoved: number;
  snapshotsRemoved: number;
  statementsRemoved: number;
}

export interface OrderCorrectionLogDto {
  id: number;
  orderId?: number | null;
  orderNumber: string;
  actionType: 'EDITED' | 'REVERSED' | string;
  adminUserId: string;
  adminName: string;
  changeSummary: string;
  reason: string;
  createdAt: string;
}

export interface PreOrderSanityCheckRequestDto {
  schemeCode: string;
  orderDate: string;
  investedAmount: number;
  purchaseNAV: number;
  unitsAllotted?: number | null;
}

export interface PreOrderSanityCheckResultDto {
  severity: 'Critical' | 'Warning' | 'OK' | 'NoAmfiData' | string;
  amfiNav?: number | null;
  amfiNavDate?: string | null;
  variancePercent?: number | null;
  expectedUnits?: number | null;
  calculatedUnits: number;
  unitMathOk: boolean;
  looksSwapped: boolean;
  message: string;
}

export interface OrderSanityAuditItemDto {
  orderId: number;
  orderNumber: string;
  orderDate: string;
  investorName: string;
  schemeCode: string;
  schemeName: string;
  folioNumber?: string | null;
  investedAmount: number;
  enteredNav: number;
  amfiNav?: number | null;
  amfiNavDate?: string | null;
  variancePercent?: number | null;
  enteredUnits: number;
  expectedUnits?: number | null;
  severity: 'Critical' | 'Warning' | 'OK' | 'NoAmfiData' | string;
  finding: string;
  looksSwapped: boolean;
  duplicateOrderNumber?: string | null;
}

export interface OrderSanityAuditSummaryDto {
  totalScanned: number;
  criticalCount: number;
  warningCount: number;
  healthyCount: number;
  noAmfiDataCount: number;
  items: OrderSanityAuditItemDto[];
}

export interface BulkImportPreviewRowDto {
  rowNumber: number;
  orderDate: string;
  investorUserId: string;
  investorName: string;
  schemeCode: string;
  schemeName: string;
  fundName: string;
  rawSchemeText: string;
  folioNumber: string;
  investedAmount: number;
  purchaseNAV: number;
  unitsAllotted: number;
  paymentMode: string;
  amfiNav?: number | null;
  amfiNavDate?: string | null;
  variancePercent?: number | null;
  status: 'Ready' | 'Warning' | 'Issue' | 'Duplicate' | 'Unmatched' | string;
  statusNote: string;
  duplicateOrderNumber?: string | null;
  includeByDefault: boolean;
  selected?: boolean;
}

export interface BulkImportPreviewResponseDto {
  sourceType: 'Excel' | 'CAS' | string;
  fileName: string;
  totalParsed: number;
  readyCount: number;
  issueCount: number;
  duplicateCount: number;
  unmatchedCount: number;
  rows: BulkImportPreviewRowDto[];
}

export interface BulkImportConfirmRowDto {
  orderDate: string;
  investorUserId: string;
  investorName: string;
  schemeCode: string;
  schemeName: string;
  fundName: string;
  folioNumber: string;
  investedAmount: number;
  purchaseNAV: number;
  unitsAllotted: number;
  paymentMode: string;
}

export interface BulkImportConfirmResultDto {
  ordersCreated: number;
  holdingsCreated: number;
  snapshotsRecalculated: number;
  createdOrderNumbers: string[];
}
