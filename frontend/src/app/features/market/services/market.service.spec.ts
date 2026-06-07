import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { MarketService } from './market.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { environment } from '../../../../environments/environment';

describe('MarketService', () => {
  let service: MarketService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/market`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(MarketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getMarketOverview() debe llamar a GET /market/overview', () => {
    // Arrange
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.getMarketOverview().subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/overview`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('getAssetDetail() debe llamar a GET /market/asset/:symbol', () => {
    // Arrange
    const symbol = 'AAPL';
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.getAssetDetail(symbol).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/asset/${symbol}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('getAssetHistory() debe llamar a GET /market/asset/:symbol/history?range=', () => {
    // Arrange
    const symbol = 'AAPL';
    const range = '1M';
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.getAssetHistory(symbol, range).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/asset/${symbol}/history?range=${range}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('getComparisonHistory() debe llamar a GET /market/comparison-history?range=', () => {
    // Arrange
    const range = '1M';
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.getComparisonHistory(range).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/comparison-history?range=${range}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('getTrending() debe llamar a GET /market/trending', () => {
    // Arrange
    const mockResponse: ApiResponse<any[]> = { success: true, message: 'ok', data: [] };

    // Act
    service.getTrending().subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/trending`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('getGainers() debe llamar a GET /market/gainers', () => {
    // Arrange
    const mockResponse: ApiResponse<any[]> = { success: true, message: 'ok', data: [] };

    // Act
    service.getGainers().subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/gainers`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('getLosers() debe llamar a GET /market/losers', () => {
    // Arrange
    const mockResponse: ApiResponse<any[]> = { success: true, message: 'ok', data: [] };

    // Act
    service.getLosers().subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/losers`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('getMarketNews() debe llamar a GET /market/news', () => {
    // Arrange
    const mockResponse: ApiResponse<any[]> = { success: true, message: 'ok', data: [] };

    // Act
    service.getMarketNews().subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/news`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });
});
