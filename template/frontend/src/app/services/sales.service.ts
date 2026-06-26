import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Sale, CreateSaleRequest, PaginatedResponse, ApiResponseWithData } from '../core/models/sales.model';

@Injectable({
  providedIn: 'root'
})
export class SalesService {
  private apiUrl = '/api/sales';

  constructor(private http: HttpClient) {}

  listSales(
    page: number = 1,
    size: number = 10,
    order?: string,
    customerId?: string,
    branchId?: string,
    minDate?: string,
    maxDate?: string
  ): Observable<PaginatedResponse<Sale>> {
    let params = new HttpParams()
      .set('_page', page.toString())
      .set('_size', size.toString());

    if (order) params = params.set('_order', order);
    if (customerId) params = params.set('customerId', customerId);
    if (branchId) params = params.set('branchId', branchId);
    if (minDate) params = params.set('minDate', minDate);
    if (maxDate) params = params.set('maxDate', maxDate);

    return this.http.get<ApiResponseWithData<PaginatedResponse<Sale>>>(this.apiUrl, { params }).pipe(
      map(res => res.data)
    );
  }

  getSale(id: string): Observable<ApiResponseWithData<Sale>> {
    return this.http.get<ApiResponseWithData<Sale>>(`${this.apiUrl}/${id}`);
  }

  createSale(request: CreateSaleRequest): Observable<ApiResponseWithData<Sale>> {
    return this.http.post<ApiResponseWithData<Sale>>(this.apiUrl, request);
  }

  cancelSale(id: string): Observable<ApiResponseWithData<any>> {
    return this.http.put<ApiResponseWithData<any>>(`${this.apiUrl}/${id}/cancel`, {});
  }
}
