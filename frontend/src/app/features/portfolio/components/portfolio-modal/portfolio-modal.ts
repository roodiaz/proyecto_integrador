import { Component, Input, Output, EventEmitter, OnChanges, OnInit, SimpleChanges } from '@angular/core'; import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PortfolioService } from '../../services/portfolio.service';
import { BuyData, SellData, PortfolioPosition } from '../../models/portfolio.modal.model';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { MaterialModule } from '../../../../shared/material.module';

@Component({
  selector: 'app-portfolio-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule],
  templateUrl: './portfolio-modal.html',
  styleUrl: './portfolio-modal.css'
})
export class PortfolioModal implements OnInit, OnChanges {
  @Input() isVisible: boolean = false;
  @Input() mode: 'buy' | 'sell' = 'buy';
  @Input() symbol = '';
  @Output() closeModal = new EventEmitter<void>();
  @Output() buyComplete = new EventEmitter<BuyData>();
  @Output() sellComplete = new EventEmitter<SellData>();

  // Buy form data
  buyTicker: string = '';
  buyQuantity: number = 0;

  // Sell form data
  sellPrice: number = 0;
  sellQuantity: number = 0;
  position?: PortfolioPosition;

  // Market price (simulated)
  marketPrice: number = 0;
  isLoadingPrice = false;

  constructor(
    private portfolioService: PortfolioService,
    private snackBarService: SnackBarService
  ) { }

  ngOnInit() {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isVisible'] && this.isVisible && this.mode === 'sell' && this.symbol)
      this.loadPosition();
  }

  loadPosition(): void {
    this.portfolioService.getPosition(this.symbol).subscribe({
      next: (response) => {
        this.position = response.data!;
        this.sellQuantity = 1;
      },
      error: () => {
        console.error('No se pudo obtener la posición');
        this.onClose();
      }
    });
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

  onSell(): void {
    if (!this.position || this.sellQuantity <= 0)
      return;

    this.sellComplete.emit({
      symbol: this.symbol,
      quantity: this.sellQuantity
    });

    this.resetForms();
  }

  get sellTotal(): number {
    if (!this.position) return 0;

    return this.sellQuantity * this.position.currentPrice;
  }

  get profitAmount(): number {
    if (!this.position) return 0;

    return this.sellQuantity * (this.position.currentPrice - this.position.avgPrice);
  }

  get profitPercent(): number {
    if (!this.position || this.position.avgPrice === 0) return 0;

    return ((this.position.currentPrice - this.position.avgPrice) / this.position.avgPrice) * 100;
  }

  private resetForms(): void {
    this.buyTicker = '';
    this.buyQuantity = 0;
    this.sellPrice = 0;
    this.sellQuantity = 0;
    this.marketPrice = 0;
    this.position = undefined;
  }
}
