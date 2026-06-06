import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { ChangePasswordRequest, ProfileResponse, UpdateProfileRequest } from '../models/user-profile.model';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class UserService {

    private readonly http = inject(HttpClient);
    private apiUrl = `${environment.apiUrl}/user`;


    getProfile() {
        return this.http.get<ProfileResponse>(
            `${this.apiUrl}/get-profile`
        );
    }

    updateProfile(request: UpdateProfileRequest) {

        return this.http.put<ApiResponse>(
            `${this.apiUrl}/update-profile`,
            request
        );

    }

    changePassword(request: ChangePasswordRequest) {

        return this.http.put<ApiResponse>(
            `${this.apiUrl}/change-password`,
            request
        );

    }

    uploadProfileImage(file: File) {

        const formData = new FormData();

        formData.append('file', file, file.name);

        return this.http.post(
            `${this.apiUrl}/profile-image`,
            formData
        );
    }

    deleteAccount():
        Observable<ApiResponse> {
        return this.http.delete<ApiResponse>(`
            ${this.apiUrl}/delete-account`
        );
    }
}