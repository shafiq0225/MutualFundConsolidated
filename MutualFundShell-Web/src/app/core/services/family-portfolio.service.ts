import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FamilyOverviewDto, MemberHoldingsDto } from '../models/family-portfolio.model';

@Injectable({ providedIn: 'root' })
export class FamilyPortfolioService {
  private readonly api = `${environment.investmentApiUrl}/api/familyportfolio`;
  private readonly jobsApi = `${environment.investmentApiUrl}/api/jobs`;

  constructor(private http: HttpClient) {}

  getOverview(): Observable<FamilyOverviewDto> {
    return this.http.get<FamilyOverviewDto>(this.api);
  }

  getMemberHoldings(userId: string): Observable<MemberHoldingsDto> {
    return this.http.get<MemberHoldingsDto>(`${this.api}/${userId}`);
  }

  triggerSnapshot(date?: Date): Observable<any> {
    const params = date ? `?date=${date.toISOString()}` : '';
    return this.http.post(`${this.jobsApi}/snapshot${params}`, {});
  }
}
