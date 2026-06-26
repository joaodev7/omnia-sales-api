import { Component } from '@angular/core';
import { SalesService } from '../../../services/sales.service';
import { CreateSaleRequest, CreateSaleItemRequest } from '../../../core/models/sales.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-sales-create',
  templateUrl: './sales-create.html',
  standalone: false,
  styleUrl: './sales-create.css'
})
export class SalesCreateComponent {
  // Sales header fields
  customerId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';
  customerName = 'John Doe';
  branchId = '3fa85f64-5717-4562-b3fc-2c963f66afa7';
  branchName = 'Main Branch';

  // Draft items in the current sale
  items: CreateSaleItemRequest[] = [];

  // Current item sub-form input fields
  currentProductId = '3fa85f64-5717-4562-b3fc-2c963f66afa8';
  currentProductName = 'Sleek Phone';
  currentQuantity = 1;
  currentUnitPrice = 100.00;

  // Real-time calculation helpers
  get currentDiscountPercentage(): number {
    const qty = this.currentQuantity || 0;
    if (qty >= 10 && qty <= 20) return 20;
    if (qty >= 4 && qty <= 9) return 10;
    return 0;
  }

  get currentDiscountValue(): number {
    const qty = this.currentQuantity || 0;
    const price = this.currentUnitPrice || 0;
    const pct = this.currentDiscountPercentage;
    return Math.round((pct / 100) * price * qty * 100) / 100;
  }

  get currentSubtotal(): number {
    const qty = this.currentQuantity || 0;
    const price = this.currentUnitPrice || 0;
    return Math.round(((price * qty) - this.currentDiscountValue) * 100) / 100;
  }

  get isQuantityInvalid(): boolean {
    return (this.currentQuantity || 0) > 20;
  }

  get isAddDisabled(): boolean {
    return this.isQuantityInvalid || (this.currentQuantity || 0) <= 0 || (this.currentUnitPrice || 0) <= 0 || !this.currentProductName || !this.currentProductId;
  }

  get saleTotal(): number {
    return this.items.reduce((sum, item) => {
      const qty = item.quantity;
      const price = item.unitPrice;
      let pct = 0;
      if (qty >= 10 && qty <= 20) pct = 0.20;
      else if (qty >= 4 && qty <= 9) pct = 0.10;
      const discount = Math.round(pct * price * qty * 100) / 100;
      const subtotal = (price * qty) - discount;
      return sum + subtotal;
    }, 0);
  }

  constructor(
    private salesService: SalesService,
    private router: Router
  ) {}

  addItem(): void {
    if (this.isAddDisabled) return;

    this.items.push({
      productId: this.currentProductId,
      productName: this.currentProductName,
      quantity: this.currentQuantity,
      unitPrice: this.currentUnitPrice
    });

    // Reset current item inputs (generating new Guid to prevent duplicates)
    this.currentProductId = this.generateGuid();
    this.currentProductName = 'Outro Produto';
    this.currentQuantity = 1;
    this.currentUnitPrice = 50.00;
  }

  removeItem(index: number): void {
    this.items.splice(index, 1);
  }

  saveSale(): void {
    if (this.items.length === 0) {
      alert('Por favor, adicione pelo menos um item à venda.');
      return;
    }

    const payload: CreateSaleRequest = {
      customerId: this.customerId,
      customerName: this.customerName,
      branchId: this.branchId,
      branchName: this.branchName,
      items: this.items
    };

    this.salesService.createSale(payload).subscribe({
      next: () => {
        alert('Venda criada com sucesso!');
        this.router.navigate(['/sales']);
      },
      error: (err) => {
        alert('Erro ao criar venda: ' + (err.error?.message || err.message));
      }
    });
  }

  private generateGuid(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = (Math.random() * 16) | 0;
      const v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
  }
}
