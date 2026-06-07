import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { AlertService } from './alert.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { environment } from '../../../../environments/environment';
import { AlertFilterDto, CreateAlertDto, UpdateAlertDto } from '../models/alert.model';

describe('AlertService', () => {
  let service: AlertService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/alert`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(AlertService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('create() debe llamar a POST /alert enviando la alerta correctamente', () => {
    // Arrange
    const alert: CreateAlertDto = { symbol: 'AAPL', condition: '>', price: 100, isActive: true };
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.create(alert).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(alert);
    req.flush(mockResponse);
  });

  it('search() debe llamar a POST /alert/search con el filtro', () => {
    // Arrange
    const filter: AlertFilterDto = { page: 1, pageSize: 10 };
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: { data: [], total: 0 } };

    // Act
    service.search(filter).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/search`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(filter);
    req.flush(mockResponse);
  });

  it('delete() debe llamar a DELETE /alert/:id con el id correspondiente', () => {
    // Arrange
    const id = 7;
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: null };

    // Act
    service.delete(id).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/${id}`);
    expect(req.request.method).toBe('DELETE');
    req.flush(mockResponse);
  });

  it('toggle() debe llamar a PATCH /alert/:id/toggle', () => {
    // Arrange
    const id = 7;
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: null };

    // Act
    service.toggle(id).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/${id}/toggle`);
    expect(req.request.method).toBe('PATCH');
    req.flush(mockResponse);
  });

  it('getStats() debe llamar a GET /alert/stats', () => {
    // Arrange
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: { active: 1, paused: 0, triggeredToday: 0, totalUsed: 1, limitAlerts: 10 } };

    // Act
    service.getStats().subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/stats`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('update() debe llamar a PUT /alert con el dto correspondiente', () => {
    // Arrange
    const dto: UpdateAlertDto = { id: 7, symbol: 'AAPL', condition: '>', price: 100, isActive: true };
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.update(dto).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(dto);
    req.flush(mockResponse);
  });
});
