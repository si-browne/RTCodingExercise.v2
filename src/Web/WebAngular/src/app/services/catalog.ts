import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Plate, PagedResult, RevenueStatistics, PlateStatus } from '../models/plate';

@Injectable({
  providedIn: 'root',
})
export class Catalog {
  private apiUrl = environment.catalogApiUrl;

  constructor(private http: HttpClient) { }

  getPlates(
    search?: string,
    letters?: string,
    numbers?: number,
    status?: PlateStatus,
    sortBy?: string,
    page: number = 1,
    pageSize: number = 20
  ): Observable<PagedResult<Plate>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) params = params.set('search', search);
    if (letters) params = params.set('letters', letters);
    if (numbers !== undefined && numbers !== null) params = params.set('numbers', numbers.toString());
    if (status !== undefined && status !== null) params = params.set('status', status.toString());
    else params = params.set('status', PlateStatus.ForSale.toString()); // Default to ForSale
    if (sortBy) params = params.set('sortBy', sortBy);

    return this.http.get<PagedResult<Plate>>(`${this.apiUrl}/api/plates`, { params });
  }

  getPlateById(id: string): Observable<Plate> {
    return this.http.get<Plate>(`${this.apiUrl}/api/plates/${id}`);
  }

  createPlate(plate: Plate): Observable<Plate> {
    return this.http.post<Plate>(`${this.apiUrl}/api/plates`, plate);
  }

  updatePlate(id: string, plate: Plate): Observable<Plate> {
    return this.http.put<Plate>(`${this.apiUrl}/api/plates/${id}`, plate);
  }

  deletePlate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/api/plates/${id}`);
  }

  reservePlate(id: string): Observable<Plate> {
    return this.http.post<Plate>(`${this.apiUrl}/api/plates/${id}/reserve`, null);
  }

  unreservePlate(id: string): Observable<Plate> {
    return this.http.post<Plate>(`${this.apiUrl}/api/plates/${id}/unreserve`, null);
  }

  sellPlate(id: string, promoCode?: string): Observable<Plate> {
    let params = new HttpParams();
    if (promoCode) params = params.set('promoCode', promoCode);
    
    return this.http.post<Plate>(`${this.apiUrl}/api/plates/${id}/sell`, null, { params });
  }

  calculatePrice(id: string, promoCode?: string): Observable<number> {
    let params = new HttpParams();
    if (promoCode) params = params.set('promoCode', promoCode);
    
    return this.http.get<number>(`${this.apiUrl}/api/plates/${id}/calculate-price`, { params });
  }

  getRevenueStatistics(): Observable<RevenueStatistics> {
    return this.http.get<RevenueStatistics>(`${this.apiUrl}/api/plates/statistics/revenue`);
  }
}
