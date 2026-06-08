import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from '../../../core/models/api-response.model';
import { environment } from '../../../../environments/environment';
import {
  RegisterRequest,
  RegisterResponse,
  VerifyRequest,
} from '../models/register.model';

import {
  LoginRequest,
  LoginResponse,
  LoginData
} from '../models/login.model';

import {
  ForgotPasswordRequest,
  ForgotPasswordResponse,
  ResetPasswordRequest
} from '../models/forgot-password.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/auth`;

  register(
    request: RegisterRequest
  ): Observable<ApiResponse<RegisterResponse>> {

    return this.http.post<ApiResponse<RegisterResponse>>(
      `${this.apiUrl}/register`,
      request
    );
  }

  resendCode(email: string): Observable<ApiResponse<RegisterResponse>> {
    return this.http.post<ApiResponse<RegisterResponse>>(
      `${this.apiUrl}/resend-code`,
      { email }
    );
  }

  verifyCode(request: VerifyRequest): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(
      `${this.apiUrl}/verify-code`,
      request
    );
  }

  login(request: LoginRequest): Observable<LoginResponse> {

    return this.http.post<LoginResponse>(
      `${this.apiUrl}/login`,
      request
    );
  }

  logout(refreshToken: string): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(
      `${this.apiUrl}/logout`,
      { refreshToken }
    );
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<ApiResponse<ForgotPasswordResponse>> {
    return this.http.post<ApiResponse<ForgotPasswordResponse>>(
      `${this.apiUrl}/forgot-password`,
      request
    );
  }

  resetPassword(request: ResetPasswordRequest): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(
      `${this.apiUrl}/reset-password`,
      request
    );
  }

  refreshToken() {
    const refreshToken = sessionStorage.getItem('refreshToken');

    return this.http.post<ApiResponse<LoginData>>(
      `${environment.apiUrl}/auth/refresh`,
      { refreshToken }
    );
  }
}