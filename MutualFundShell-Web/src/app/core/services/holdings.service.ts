import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { HoldingDto } from '../models/holding.model';

@Injectable({ providedIn: 'root' })
export class HoldingsService {
  private readonly api = `${environment.investmentApiUrl}/api/portfolio/holdings`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<HoldingDto[]> {
    return this.http.get<HoldingDto[]>(this.api);
  }
}
