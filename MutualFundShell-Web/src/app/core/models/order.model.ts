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
