import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { UserService } from './user.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { environment } from '../../../../environments/environment';
import { ChangePasswordRequest, UpdateProfileRequest } from '../models/user-profile.model';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/user`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getProfile() debe llamar a GET /user/get-profile', () => {
    // Arrange
    const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };

    // Act
    service.getProfile().subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/get-profile`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('updateProfile() debe llamar a PUT /user/update-profile con el body correcto', () => {
    // Arrange
    const request: UpdateProfileRequest = { userName: 'tester', currency: 'USD', emailNotifications: true };
    const mockResponse: ApiResponse = { success: true, message: 'ok', data: null };

    // Act
    service.updateProfile(request).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/update-profile`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(mockResponse);
  });

  it('changePassword() debe llamar a PUT /user/change-password con el body correcto', () => {
    // Arrange
    const request: ChangePasswordRequest = { currentPassword: 'old', newPassword: 'new' };
    const mockResponse: ApiResponse = { success: true, message: 'ok', data: null };

    // Act
    service.changePassword(request).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/change-password`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(mockResponse);
  });

  it('uploadProfileImage() debe llamar a POST /user/profile-image con un FormData', () => {
    // Arrange
    const file = new File(['contenido'], 'avatar.png', { type: 'image/png' });
    const mockResponse: ApiResponse = { success: true, message: 'ok', data: { profileImageUrl: '/img/avatar.png' } };

    // Act
    service.uploadProfileImage(file).subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/profile-image`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body instanceof FormData).toBeTrue();
    req.flush(mockResponse);
  });

  it('deleteAccount() debe llamar a DELETE /user/delete-account', () => {
    // Arrange
    const mockResponse: ApiResponse = { success: true, message: 'ok', data: null };

    // Act
    service.deleteAccount().subscribe(res => {
      // Assert
      expect(res).toEqual(mockResponse);
    });

    // Assert
    const req = httpMock.expectOne(`${apiUrl}/delete-account`);
    expect(req.request.method).toBe('DELETE');
    req.flush(mockResponse);
  });
});
