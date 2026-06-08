import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { ChangePasswordRequest, ConfirmEmailChangeRequest, ProfileResponse, RequestEmailChangeRequest, UpdateProfileRequest } from '../models/user-profile.model';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/user`;

  getProfile(): Observable<ProfileResponse> {
    return this.http.get<ProfileResponse>(`${this.apiUrl}/get-profile`);
  }

  updateProfile(request: UpdateProfileRequest): Observable<ApiResponse> {
    return this.http.put<ApiResponse>(`${this.apiUrl}/update-profile`, request);
  }

  changePassword(request: ChangePasswordRequest): Observable<ApiResponse> {
    return this.http.put<ApiResponse>(`${this.apiUrl}/change-password`, request);
  }

  requestEmailChange(request: RequestEmailChangeRequest): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/request-email-change`, request);
  }

  confirmEmailChange(request: ConfirmEmailChangeRequest): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/confirm-email-change`, request);
  }

  uploadProfileImage(file: File): Observable<ApiResponse> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ApiResponse>(`${this.apiUrl}/profile-image`, formData);
  }

  deleteAccount(): Observable<ApiResponse> {
    return this.http.delete<ApiResponse>(`${this.apiUrl}/delete-account`);
  }
}