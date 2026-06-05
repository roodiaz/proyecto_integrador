import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { PortfolioService } from '../../services/portfolio.service';
import { BuyData, SellData, PortfolioModalData, PortfolioPosition, PortfolioModalResult } from '../../models/portfolio.modal.model';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { MaterialModule } from '../../../../shared/material.module';

@Component({
  selector: 'app-portfolio-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, MaterialModule],
  templateUrl: './portfolio-modal.html',
  styleUrl: './portfolio-modal.css'
})
export class PortfolioModal implements OnInit {
  mode: 'buy' | 'sell' = 'buy';
  symbol = '';

  buyTicker = '';
  buyQuantity = 0;

  sellPrice = 0;
  sellQuantity = 0;
  position?: PortfolioPosition;

  marketPrice = 0;
  isLoadingPrice = false;

  constructor(
    private dialogRef: MatDialogRef<PortfolioModal, PortfolioModalResult>,
    @Inject(MAT_DIALOG_DATA) public data: PortfolioModalData,
    private portfolioService: PortfolioService,
    private snackBarService: SnackBarService
  ) {
    this.mode = data.mode;
    this.symbol = data.symbol ?? '';

    if (this.mode === 'buy' && this.symbol)
      this.buyTicker = this.symbol;
  }

  ngOnInit(): void {
    if (this.mode === 'sell' && this.symbol)
      this.loadPosition();

    if (this.mode === 'buy' && this.buyTicker)
      this.loadMarketPrice();
  }

  loadPosition(): void {
    this.portfolioService.getPosition(this.symbol).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.snackBarService.error(response.message || 'No se pudo obtener la posición');
          this.onClose();
          return;
        }

        this.position = response.data;
        this.sellQuantity = 1;
      },
      error: error => {
        console.error('No se pudo obtener la posición', error);
        this.snackBarService.error('No se pudo obtener la posición');
        this.onClose();
      }
    });
  }

  loadMarketPrice(): void {
    if (!this.buyTicker.trim())
      return;

    this.isLoadingPrice = true;

    this.portfolioService.getAssetPrice(this.buyTicker.trim().toUpperCase()).subscribe({
      next: response => {
        this.isLoadingPrice = false;

        if (!response.success || !response.data) {
          this.marketPrice = 0;
          this.snackBarService.info(response.message || 'No se encontró el activo');
          return;
        }

        this.marketPrice = response.data.currentPrice;
      },
      error: error => {
        console.error('Error al obtener precio', error);
        this.isLoadingPrice = false;
        this.marketPrice = 0;
        this.snackBarService.error('No se pudo obtener el precio del activo');
      }
    });
  }

  get modalTitle(): string {
    return this.mode === 'buy' ? 'Nueva operación' : 'Vender activo';
  }

  get currentBalance(): number {
    return 10000;
  }

  get canBuy(): boolean {
    return !!(this.buyTicker.trim() && this.buyQuantity > 0 && this.marketPrice > 0 && this.totalCost <= this.currentBalance);
  }

  get totalCost(): number {
    return this.buyQuantity * this.marketPrice;
  }

  get canSell(): boolean {
    return !!(this.position && this.sellQuantity > 0 && this.sellQuantity <= this.position.quantity);
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

  onClose(): void {
    this.dialogRef.close();
  }

  onBuy(): void {
    if (!this.canBuy) return;

    const data: BuyData = {
      ticker: this.buyTicker.trim().toUpperCase(),
      quantity: this.buyQuantity
    };

    this.dialogRef.close({ mode: 'buy', data });
  }

  onSell(): void {
    if (!this.canSell || !this.position)
      return;

    const data: SellData = {
      symbol: this.symbol,
      quantity: this.sellQuantity
    };

    this.dialogRef.close({ mode: 'sell', data });
  }
}