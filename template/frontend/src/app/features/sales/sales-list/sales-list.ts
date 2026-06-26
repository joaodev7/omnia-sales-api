import { Component, OnInit } from '@angular/core';
import { PageEvent } from '@angular/material/paginator';
import { SalesService } from '../../../services/sales.service';
import { Sale } from '../../../core/models/sales.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-sales-list',
  templateUrl: './sales-list.html',
  standalone: false,
  styleUrl: './sales-list.css'
})
export class SalesListComponent implements OnInit {
  displayedColumns: string[] = ['saleNumber', 'saleDate', 'customerName', 'branchName', 'totalAmount', 'isCancelled', 'actions'];
  sales: Sale[] = [];
  isLoading = false;

  // Pagination state
  totalCount = 0;
  pageSize = 10;
  pageIndex = 0;

  // Filter state
  customerId = '';
  branchId = '';
  minDate = '';
  maxDate = '';
  order = 'SaleDate desc';

  constructor(
    private salesService: SalesService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadSales();
  }

  loadSales(): void {
    this.isLoading = true;
    const page = this.pageIndex + 1; // API is 1-based, Angular paginator is 0-based
    this.salesService.listSales(
      page,
      this.pageSize,
      this.order,
      this.customerId || undefined,
      this.branchId || undefined,
      this.minDate || undefined,
      this.maxDate || undefined
    ).subscribe({
      next: (res) => {
        this.sales = res.data;
        this.totalCount = res.totalCount;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading sales', err);
        this.isLoading = false;
      }
    });
  }

  applyFilters(): void {
    this.pageIndex = 0;
    this.loadSales();
  }

  clearFilters(): void {
    this.customerId = '';
    this.branchId = '';
    this.minDate = '';
    this.maxDate = '';
    this.order = 'SaleDate desc';
    this.applyFilters();
  }

  handlePageEvent(e: PageEvent): void {
    this.pageSize = e.pageSize;
    this.pageIndex = e.pageIndex;
    this.loadSales();
  }

  viewDetails(id: string): void {
    this.router.navigate(['/sales', id]);
  }

  cancelSale(id: string, event: Event): void {
    event.stopPropagation();
    if (confirm('Deseja realmente cancelar esta venda?')) {
      this.salesService.cancelSale(id).subscribe({
        next: () => {
          this.loadSales();
        },
        error: (err) => {
          alert('Erro ao cancelar venda: ' + (err.error?.message || err.message));
        }
      });
    }
  }
}
