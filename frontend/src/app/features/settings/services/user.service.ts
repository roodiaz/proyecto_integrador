import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { ChangePasswordRequest, ProfileResponse, UpdateProfileRequest } from '../models/user-profile.model';

@Injectable({
    providedIn: 'root'
})
export class UserService {

    private readonly http = inject(HttpClient);

    getProfile() {
        return this.http.get<ProfileResponse>(
            `${environment.apiUrl}/user/get-profile`
        );
    }

    updateProfile(request: UpdateProfileRequest) {

        return this.http.put<ApiResponse>(
            `${environment.apiUrl}/user/update-profile`,
            request
        );

    }

    changePassword(request: ChangePasswordRequest) {

        return this.http.put<ApiResponse>(
            `${environment.apiUrl}/user/change-password`,
            request
        );

    }

    uploadProfileImage(file: File) {

        const formData = new FormData();

        formData.append('file', file, file.name);

        return this.http.post(
            `${environment.apiUrl}/user/profile-image`,
            formData
        );

    }
}