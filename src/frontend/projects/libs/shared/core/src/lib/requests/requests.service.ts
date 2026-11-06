import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENVIRONMENT } from '@pdf-sign-flow/shared/environment';
import { Observable } from 'rxjs';
import {
  CreateRequestInput,
  RequestFile,
  SignatureRequestDetails,
  SignatureRequestSummary,
} from './requests.models';

@Injectable({ providedIn: 'root' })
export class RequestsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(APP_ENVIRONMENT).apiBaseUrl}/requests`;

  list(): Observable<SignatureRequestSummary[]> {
    return this.http.get<SignatureRequestSummary[]>(this.baseUrl);
  }

  create(input: CreateRequestInput): Observable<SignatureRequestDetails> {
    return this.http.post<SignatureRequestDetails>(this.baseUrl, input);
  }

  get(id: string): Observable<SignatureRequestDetails> {
    return this.http.get<SignatureRequestDetails>(`${this.baseUrl}/${id}`);
  }

  uploadDocument(id: string, file: File): Observable<RequestFile> {
    const body = new FormData();
    body.append('file', file, file.name);
    return this.http.post<RequestFile>(`${this.baseUrl}/${id}/documents`, body);
  }

  removeFile(id: string, fileId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/files/${fileId}`);
  }
}
