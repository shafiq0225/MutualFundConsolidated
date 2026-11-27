import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  InvestmentOrderDto,
  CreateOrderDto,
  UpdateOrderStatusDto,
  AdminEditOrderDto,
  AdminReverseOrdersDto,
  OrderCorrectionImpactDto,
  AdminEditOrderResultDto,
  AdminReverseOrdersResultDto,
  OrderCorrectionLogDto,
  PreOrderSanityCheckRequestDto,
  PreOrderSanityCheckResultDto,
  OrderSanityAuditSummaryDto
} from '../models/order.model';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly api = `${environment.apiUrl}/api/orders`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<InvestmentOrderDto[]> {
    return this.http.get<InvestmentOrderDto[]>(this.api);
  }

  getById(id: number): Observable<InvestmentOrderDto> {
    return this.http.get<InvestmentOrderDto>(`${this.api}/${id}`);
  }

  getByInvestor(userId: string): Observable<InvestmentOrderDto[]> {
    return this.http.get<InvestmentOrderDto[]>(`${this.api}/investor/${userId}`);
  }

  create(dto: CreateOrderDto): Observable<InvestmentOrderDto> {
    return this.http.post<InvestmentOrderDto>(this.api, dto);
  }

  updateStatus(id: number, dto: UpdateOrderStatusDto): Observable<InvestmentOrderDto> {
    return this.http.put<InvestmentOrderDto>(`${this.api}/${id}/status`, dto);
  }

  getCorrectionImpact(orderIds: number[]): Observable<OrderCorrectionImpactDto> {
    return this.http.post<OrderCorrectionImpactDto>(`${this.api}/correction-impact`, { orderIds });
  }

  adminEditOrder(id: number, dto: AdminEditOrderDto): Observable<AdminEditOrderResultDto> {
    return this.http.put<AdminEditOrderResultDto>(`${this.api}/${id}/admin-edit`, dto);
  }

  adminReverseOrders(dto: AdminReverseOrdersDto): Observable<AdminReverseOrdersResultDto> {
    return this.http.post<AdminReverseOrdersResultDto>(`${this.api}/admin-reverse`, dto);
  }

  getCorrectionLogs(limit = 100): Observable<OrderCorrectionLogDto[]> {
    return this.http.get<OrderCorrectionLogDto[]>(`${this.api}/correction-logs`, {
      params: { limit: limit.toString() }
    });
  }

  scanOrdersSanity(): Observable<OrderSanityAuditSummaryDto> {
    return this.http.get<OrderSanityAuditSummaryDto>(`${this.api}/sanity-audit`);
  }

  checkPreOrderSanity(dto: PreOrderSanityCheckRequestDto): Observable<PreOrderSanityCheckResultDto> {
    return this.http.post<PreOrderSanityCheckResultDto>(`${this.api}/sanity-check`, dto);
  }
}
