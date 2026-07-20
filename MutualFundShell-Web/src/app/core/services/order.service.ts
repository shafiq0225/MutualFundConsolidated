import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { InvestmentOrderDto, CreateOrderDto, UpdateOrderStatusDto } from '../models/order.model';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly api = `${environment.investmentApiUrl}/api/orders`;

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
}
