import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { ContactRequest } from '../models/contact.model';

@Injectable({
  providedIn: 'root'
})
export class ContactService {

  private http = inject(HttpClient);

  send(request: ContactRequest) {
    return this.http.post<ApiResponse>(
      `${environment.apiUrl}/contact/contact`,
      request
    );
  }

}