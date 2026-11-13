import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { apiConfig } from '../config/api.config';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface VerifyEmailRequest {
  email: string;
  code: string;
}

export interface ResendVerificationRequest {
  email: string;
}

export interface AuthResponse {
  userId: string;
  email: string;
  token: string;
}

export interface RegistrationResponse {
  email: string;
  message: string;
}

export interface MessageResponse {
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly tokenKey = 'task-management-token';
  private readonly userEmailKey = 'task-management-user-email';

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(
        `${apiConfig.baseUrl}/api/auth/login`,
        request,
      )
      .pipe(
        tap((response) => {
          this.storeSession(response);
        }),
      );
  }

  register(
    request: RegisterRequest,
  ): Observable<RegistrationResponse> {
    return this.http.post<RegistrationResponse>(
      `${apiConfig.baseUrl}/api/auth/register`,
      request,
    );
  }

  verifyEmail(
    request: VerifyEmailRequest,
  ): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(
        `${apiConfig.baseUrl}/api/auth/verify-email`,
        request,
      )
      .pipe(
        tap((response) => {
          this.storeSession(response);
        }),
      );
  }

  resendVerification(
    request: ResendVerificationRequest,
  ): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(
      `${apiConfig.baseUrl}/api/auth/resend-verification`,
      request,
    );
  }

  getToken(): string | null {
    return sessionStorage.getItem(this.tokenKey);
  }

  getUserEmail(): string | null {
    return sessionStorage.getItem(this.userEmailKey);
  }

  isAuthenticated(): boolean {
    return Boolean(this.getToken());
  }

  clearSession(): void {
    sessionStorage.removeItem(this.tokenKey);
    sessionStorage.removeItem(this.userEmailKey);
  }

  logout(): void {
    this.clearSession();
  }

  private storeSession(response: AuthResponse): void {
    sessionStorage.setItem(this.tokenKey, response.token);
    sessionStorage.setItem(
      this.userEmailKey,
      response.email,
    );
  }
}