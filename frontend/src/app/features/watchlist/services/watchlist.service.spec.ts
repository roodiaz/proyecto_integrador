import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { WatchlistService } from './watchlist.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { environment } from '../../../../environments/environment';
import { FavoriteFilter } from '../models/watchlist-item';

describe('WatchlistService', () => {
  let service: WatchlistService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(WatchlistService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getFavorites() debe llamar a POST /favorite/list con el filtro', () => {
    // Arrange
    const filter: FavoriteFilter = { page: 1, pageSize: 7, search: '' };
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: { items: [], total: 0, page: 1, pageSize: 7, currentFavorites: 0, maxFavorites: 10 } };

    // Act
    service.getFavorites(filter).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${environment.apiUrl}/favorite/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(filter);
    req.flush(mockResponse);
  });

  it('addFavorite() debe llamar a POST /favorite/add enviando el símbolo', () => {
    // Arrange
    const symbol = 'AAPL';
    const mockResponse: ApiResponse = { success: true, message: 'ok', data: null };

    // Act
    service.addFavorite(symbol).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${environment.apiUrl}/favorite/add`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ symbol });
    req.flush(mockResponse);
  });

  it('removeFavorite() debe llamar a DELETE /favorite/:symbol', () => {
    // Arrange
    const symbol = 'AAPL';
    const mockResponse: ApiResponse = { success: true, message: 'ok', data: null };

    // Act
    service.removeFavorite(symbol).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${environment.apiUrl}/favorite/${symbol}`);
    expect(req.request.method).toBe('DELETE');
    req.flush(mockResponse);
  });

  it('existsFavorite() debe llamar a GET /favorite/exists/:symbol', () => {
    // Arrange
    const symbol = 'AAPL';
    const mockResponse: ApiResponse<boolean> = { success: true, message: 'ok', data: true };

    // Act
    service.existsFavorite(symbol).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${environment.apiUrl}/favorite/exists/${symbol}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });
});
