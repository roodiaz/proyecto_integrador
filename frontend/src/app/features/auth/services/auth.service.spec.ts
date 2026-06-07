import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { AuthService } from './auth.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { environment } from '../../../../environments/environment';
import { LoginRequest, LoginResponse } from '../models/login.model';
import { RegisterRequest, RegisterResponse, VerifyRequest } from '../models/register.model';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/auth`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('register() debe llamar a POST /auth/register con el body correcto', () => {
    // Arrange
    const request: RegisterRequest = { fullName: 'Test', email: 't@test.com', password: '123456', confirmPassword: '123456' };
    const mockResponse: ApiResponse<RegisterResponse> = { success: true, message: 'ok', data: { requiresVerification: true, email: request.email, emailSent: true } };

    // Act
    service.register(request).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/register`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(mockResponse);
  });

  it('resendCode() debe llamar a POST /auth/resend-code con el email', () => {
    // Arrange
    const email = 't@test.com';
    const mockResponse: ApiResponse<RegisterResponse> = { success: true, message: 'ok', data: { requiresVerification: true, email, emailSent: true } };

    // Act
    service.resendCode(email).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/resend-code`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email });
    req.flush(mockResponse);
  });

  it('verifyCode() debe llamar a POST /auth/verify-code con el body correcto', () => {
    // Arrange
    const request: VerifyRequest = { email: 't@test.com', code: '1234' };
    const mockResponse: ApiResponse<null> = { success: true, message: 'ok', data: null };

    // Act
    service.verifyCode(request).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/verify-code`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(mockResponse);
  });

  it('login() debe llamar a POST /auth/login con las credenciales', () => {
    // Arrange
    const request: LoginRequest = { email: 't@test.com', password: '123456' };
    const mockResponse: LoginResponse = { success: true, message: 'ok', data: { tokens: { accessToken: 'a', refreshToken: 'r' } } };

    // Act
    service.login(request).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(mockResponse);
  });

  it('logout() debe llamar a POST /auth/logout enviando el refreshToken', () => {
    // Arrange
    const refreshToken = 'r-token';
    const mockResponse: ApiResponse<null> = { success: true, message: 'ok', data: null };

    // Act
    service.logout(refreshToken).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/logout`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ refreshToken });
    req.flush(mockResponse);
  });

  it('refreshToken() debe llamar a POST /auth/refresh con el refreshToken almacenado', () => {
    // Arrange
    sessionStorage.setItem('refreshToken', 'stored-token');
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: { tokens: { accessToken: 'a', refreshToken: 'r' } } };

    // Act
    service.refreshToken().subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${environment.apiUrl}/auth/refresh`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ refreshToken: 'stored-token' });
    req.flush(mockResponse);

    sessionStorage.removeItem('refreshToken');
  });
});
