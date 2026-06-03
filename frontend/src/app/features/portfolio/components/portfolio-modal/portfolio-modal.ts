import { Component, Input, Output, EventEmitter, OnChanges, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PortfolioService } from '../../services/portfolio.service';
import { BuyData } from '../../models/portfolio.modal.model';

@Component({
  selector: 'app-portfolio-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './portfolio-modal.html',
  styleUrl: './portfolio-modal.css'
})
export class PortfolioModal implements OnInit {
  @Input() isVisible: boolean = false;
  @Input() mode: 'buy' | 'sell' = 'buy';
  @Output() closeModal = new EventEmitter<void>();
  @Output() buyComplete = new EventEmitter<BuyData>();
  @Output() sellComplete = new EventEmitter<string>();

  // Buy form data
  buyTicker: string = '';
  buyQuantity: number = 0;

  // Sell form data
  sellPrice: number = 0;
  sellQuantity: number = 0;

  // Market price (simulated)
  marketPrice: number = 0;
  isLoadingPrice = false;

  constructor(private portfolioService: PortfolioService) { }

  ngOnInit() {
  }

  loadMarketPrice(): void {
    if (!this.buyTicker.trim())
      return;

    this.portfolioService.getAssetPrice(this.buyTicker.trim().toUpperCase()).subscribe({
      next: (response) => {
        if (response.success && response.data)
          this.marketPrice = response.data.currentPrice;
      },
      error: () => {
        this.marketPrice = 0;
      }
    });
  }

  get modalTitle(): string {
    return this.mode === 'buy' ? 'Nueva Operación' : 'Vender Activo';
  }

  get currentBalance(): number {
    return 10000; // Saldo actual fijo para ejemplo
  }

  get canBuy(): boolean {
    return !!(this.buyTicker.trim() &&
      this.buyQuantity > 0 &&
      this.marketPrice > 0 &&
      (this.buyQuantity * this.marketPrice) <= this.currentBalance);
  }

  get totalCost(): number {
    return this.buyQuantity * this.marketPrice;
  }

  switchMode(newMode: 'buy' | 'sell'): void {
    this.mode = newMode;
    this.resetForms();
  }

  onClose(): void {
    this.closeModal.emit();
    this.resetForms();
  }

  onBuy(): void {
    if (!this.canBuy) return;

    const buyData: BuyData = {
      ticker: this.buyTicker.trim().toUpperCase(),
      quantity: this.buyQuantity
    };

    this.buyComplete.emit(buyData);
    this.resetForms();
  }

  private resetForms(): void {
    this.buyTicker = '';
    this.buyQuantity = 0;
    this.sellPrice = 0;
    this.sellQuantity = 0;
    this.marketPrice = 0;
  }
}
