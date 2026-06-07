import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { PortfolioService } from './portfolio.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { environment } from '../../../../environments/environment';
import { SellData } from '../models/portfolio.modal.model';
import { OpenPositionsFilter, TransactionFilter } from '../models/portfolio.model';

describe('PortfolioService', () => {
  let service: PortfolioService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/portfolio`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(PortfolioService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getBalanceCards() debe llamar a GET /portfolio/balance-cards', () => {
    // Arrange
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.getBalanceCards().subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/balance-cards`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('getPieChart() debe llamar a GET /portfolio/pie-chart', () => {
    // Arrange
    const mockResponse: ApiResponse<any[]> = { success: true, message: 'ok', data: [] };

    // Act
    service.getPieChart().subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/pie-chart`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('getLineChart() debe llamar a POST /portfolio/line-chart con el período', () => {
    // Arrange
    const period = '1M';
    const mockResponse: ApiResponse<any[]> = { success: true, message: 'ok', data: [] };

    // Act
    service.getLineChart(period).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/line-chart`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ period });
    req.flush(mockResponse);
  });

  it('getOpenPositions() debe llamar a POST /portfolio/open-positions con el filtro', () => {
    // Arrange
    const filter = { page: 1, pageSize: 10 } as OpenPositionsFilter;
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.getOpenPositions(filter).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/open-positions`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(filter);
    req.flush(mockResponse);
  });

  it('getTransactionHistory() debe llamar a POST /portfolio/history con el filtro', () => {
    // Arrange
    const filter = { page: 1, pageSize: 10 } as TransactionFilter;
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.getTransactionHistory(filter).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/history`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(filter);
    req.flush(mockResponse);
  });

  it('getAssetPrice() debe llamar a GET /portfolio/price/:symbol', () => {
    // Arrange
    const symbol = 'AAPL';
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: { symbol, currentPrice: 100 } };

    // Act
    service.getAssetPrice(symbol).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/price/${symbol}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('buyAsset() debe llamar a POST /portfolio/buy con el símbolo y la cantidad', () => {
    // Arrange
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.buyAsset('AAPL', 5).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/buy`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ symbol: 'AAPL', quantity: 5 });
    req.flush(mockResponse);
  });

  it('getPosition() debe llamar a GET /portfolio/:symbol', () => {
    // Arrange
    const symbol = 'AAPL';
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.getPosition(symbol).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/${symbol}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('sell() debe llamar a POST /portfolio/sell con el body correcto', () => {
    // Arrange
    const data = { symbol: 'AAPL', quantity: 2 } as SellData;
    const mockResponse: ApiResponse = { success: true, message: 'ok', data: null };

    // Act
    service.sell(data).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/sell`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(data);
    req.flush(mockResponse);
  });
});
