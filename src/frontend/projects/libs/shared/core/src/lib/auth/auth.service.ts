import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENVIRONMENT } from '@pdf-sign-flow/shared/environment';
import { map, Observable, tap } from 'rxjs';
import {
  ChangePasswordRequest,
  CurrentUser,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
} from './auth.models';
import { SessionStore } from './session.store';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(SessionStore);
  private readonly baseUrl = `${inject(APP_ENVIRONMENT).apiBaseUrl}/auth`;

  login(request: LoginRequest): Observable<CurrentUser> {
    return this.startSession(this.http.post<LoginResponse>(`${this.baseUrl}/login`, request));
  }

  register(request: RegisterRequest): Observable<CurrentUser> {
    return this.startSession(this.http.post<LoginResponse>(`${this.baseUrl}/register`, request));
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/change-password`, request);
  }

  logout(): void {
    this.session.clear();
  }

  private startSession(response: Observable<LoginResponse>): Observable<CurrentUser> {
    return response.pipe(
      tap((session) => this.session.start(session)),
      map((session) => session.user),
    );
  }
}
