import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { PortfolioService } from './portfolio.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { environment } from '../../../../environments/environment';
import { SellData } from '../models/portfolio.modal.model';
import { OpenPositionsFilter, TransactionFilter, SetupPortfolioRequest } from '../models/portfolio.model';

describe('PortfolioService', () => {
  let service: PortfolioService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/portfolio`;
  const portfolioId = 1;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(PortfolioService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getUserPortfolios() debe llamar a GET /portfolio', () => {
    const mockResponse: ApiResponse<any[]> = { success: true, message: 'ok', data: [] };

    service.getUserPortfolios().subscribe(res => {
      expect(res).toEqual(mockResponse);
    });

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('createPortfolio() debe llamar a POST /portfolio con el dto', () => {
    const dto: SetupPortfolioRequest = { portfolioName: 'Mi Portfolio', initialBalance: 10000 };
    const mockResponse: ApiResponse = { success: true, message: 'ok', data: null };

    service.createPortfolio(dto).subscribe(res => {
      expect(res).toEqual(mockResponse);
    });

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush(mockResponse);
  });

  it('setActivePortfolio() debe llamar a POST /portfolio/:id/activate', () => {
    const mockResponse: ApiResponse = { success: true, message: 'ok', data: null };

    service.setActivePortfolio(portfolioId).subscribe(res => {
      expect(res).toEqual(mockResponse);
    });

    const req = httpMock.expectOne(`${apiUrl}/${portfolioId}/activate`);
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });

  it('deletePortfolio() debe llamar a DELETE /portfolio/:id', () => {
    const mockResponse: ApiResponse = { success: true, message: 'ok', data: null };

    service.deletePortfolio(portfolioId).subscribe(res => {
      expect(res).toEqual(mockResponse);
    });

    const req = httpMock.expectOne(`${apiUrl}/${portfolioId}`);
    expect(req.request.method).toBe('DELETE');
    req.flush(mockResponse);
  });

  it('resetPortfolio() debe llamar a POST /portfolio/:id/reset con el dto', () => {
    const dto: SetupPortfolioRequest = { portfolioName: 'Mi Portfolio', initialBalance: 10000 };
    const mockResponse: ApiResponse = { success: true, message: 'ok', data: null };

    service.resetPortfolio(portfolioId, dto).subscribe(res => {
      expect(res).toEqual(mockResponse);
    });

    const req = httpMock.expectOne(`${apiUrl}/${portfolioId}/reset`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush(mockResponse);
  });

  it('getBalanceCards() debe llamar a GET /portfolio/:id/balance-cards', () => {
    // Arrange
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.getBalanceCards(portfolioId).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/${portfolioId}/balance-cards`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('getPieChart() debe llamar a GET /portfolio/:id/pie-chart', () => {
    // Arrange
    const mockResponse: ApiResponse<any[]> = { success: true, message: 'ok', data: [] };

    // Act
    service.getPieChart(portfolioId).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/${portfolioId}/pie-chart`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('getLineChart() debe llamar a POST /portfolio/:id/line-chart con el período', () => {
    // Arrange
    const period = '1M';
    const mockResponse: ApiResponse<any[]> = { success: true, message: 'ok', data: [] };

    // Act
    service.getLineChart(portfolioId, period).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/${portfolioId}/line-chart`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ period });
    req.flush(mockResponse);
  });

  it('getOpenPositions() debe llamar a POST /portfolio/:id/open-positions con el filtro', () => {
    // Arrange
    const filter = { page: 1, pageSize: 10 } as OpenPositionsFilter;
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.getOpenPositions(portfolioId, filter).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/${portfolioId}/open-positions`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(filter);
    req.flush(mockResponse);
  });

  it('getTransactionHistory() debe llamar a POST /portfolio/:id/history con el filtro', () => {
    // Arrange
    const filter = { page: 1, pageSize: 10 } as TransactionFilter;
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.getTransactionHistory(portfolioId, filter).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/${portfolioId}/history`);
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

  it('buyAsset() debe llamar a POST /portfolio/:id/buy con el símbolo y la cantidad', () => {
    // Arrange
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.buyAsset(portfolioId, 'AAPL', 5).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/${portfolioId}/buy`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ symbol: 'AAPL', quantity: 5 });
    req.flush(mockResponse);
  });

  it('getPosition() debe llamar a GET /portfolio/:id/position/:symbol', () => {
    // Arrange
    const symbol = 'AAPL';
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.getPosition(portfolioId, symbol).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/${portfolioId}/position/${symbol}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('sell() debe llamar a POST /portfolio/:id/sell con el body correcto', () => {
    // Arrange
    const data = { symbol: 'AAPL', quantity: 2 } as SellData;
    const mockResponse: ApiResponse = { success: true, message: 'ok', data: null };

    // Act
    service.sell(portfolioId, data).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/${portfolioId}/sell`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(data);
    req.flush(mockResponse);
  });
});
