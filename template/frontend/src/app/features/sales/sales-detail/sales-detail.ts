import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SalesService } from '../../../services/sales.service';
import { Sale } from '../../../core/models/sales.model';

@Component({
  selector: 'app-sales-detail',
  templateUrl: './sales-detail.html',
  standalone: false,
  styleUrl: './sales-detail.css'
})
export class SalesDetailComponent implements OnInit {
  sale: Sale | null = null;
  isLoading = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private salesService: SalesService
  ) {}

  ngOnInit(): void {
    this.loadSale();
  }

  loadSale(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/sales']);
      return;
    }

    this.isLoading = true;
    this.salesService.getSale(id).subscribe({
      next: (res) => {
        this.sale = res.data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading sale details', err);
        alert('Venda não encontrada.');
        this.router.navigate(['/sales']);
      }
    });
  }

  cancelSale(): void {
    if (!this.sale) return;
    if (confirm('Deseja realmente cancelar esta venda?')) {
      this.salesService.cancelSale(this.sale.id).subscribe({
        next: () => {
          alert('Venda cancelada com sucesso!');
          this.loadSale();
        },
        error: (err) => {
          alert('Erro ao cancelar venda: ' + (err.error?.message || err.message));
        }
      });
    }
  }

  goBack(): void {
    this.router.navigate(['/sales']);
  }
}
