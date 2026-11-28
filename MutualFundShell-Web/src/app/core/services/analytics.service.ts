import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AnalyticsHubResponseDto,
  AnalyticsMonthOrderDto
} from '../models/analytics.model';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly api = `${environment.apiUrl}/api/analytics`;

  constructor(private http: HttpClient) {}

  getAnalytics(investorUserId?: string | null): Observable<AnalyticsHubResponseDto> {
    let params = new HttpParams();
    if (investorUserId && investorUserId !== 'all' && investorUserId !== 'family') {
      params = params.set('investorUserId', investorUserId);
    }
    return this.http.get<AnalyticsHubResponseDto>(this.api, { params });
  }

  getMonthOrders(
    year: number,
    month: number,
    schemeCode?: string,
    investorUserId?: string | null
  ): Observable<AnalyticsMonthOrderDto[]> {
    let params = new HttpParams()
      .set('year', year.toString())
      .set('month', month.toString());
    if (schemeCode) {
      params = params.set('schemeCode', schemeCode);
    }
    if (investorUserId && investorUserId !== 'all' && investorUserId !== 'family') {
      params = params.set('investorUserId', investorUserId);
    }
    return this.http.get<AnalyticsMonthOrderDto[]>(`${this.api}/month-orders`, { params });
  }
}
