import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
} from 'vitest';

import {
  AuthResponse,
  AuthService,
  LoginRequest,
  RegisterRequest,
  ResendVerificationRequest,
  VerifyEmailRequest,
} from './auth.service';
import { apiConfig } from '../config/api.config';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('login should send credentials and store the session', () => {
    const request: LoginRequest = {
      email: 'user@example.com',
      password: 'Password123!',
    };

    const response: AuthResponse = {
      userId: 'user-id',
      email: 'user@example.com',
      token: 'jwt-token',
    };

    let actualResponse: AuthResponse | undefined;

    service.login(request).subscribe((result) => {
      actualResponse = result;
    });

    const httpRequest = httpMock.expectOne(
      `${apiConfig.baseUrl}/api/auth/login`,
    );

    expect(httpRequest.request.method).toBe('POST');
    expect(httpRequest.request.body).toEqual(request);

    httpRequest.flush(response);

    expect(actualResponse).toEqual(response);
    expect(service.getToken()).toBe('jwt-token');
    expect(service.getUserEmail()).toBe('user@example.com');
    expect(service.isAuthenticated()).toBe(true);
  });

  it('register should send the registration request', () => {
    const request: RegisterRequest = {
      email: 'new-user@example.com',
      password: 'Password123!',
    };

    const response = {
      email: 'new-user@example.com',
      message: 'Registration successful.',
    };

    let actualResponse:
      | typeof response
      | undefined;

    service.register(request).subscribe((result) => {
      actualResponse = result;
    });

    const httpRequest = httpMock.expectOne(
      `${apiConfig.baseUrl}/api/auth/register`,
    );

    expect(httpRequest.request.method).toBe('POST');
    expect(httpRequest.request.body).toEqual(request);

    httpRequest.flush(response);

    expect(actualResponse).toEqual(response);
    expect(service.isAuthenticated()).toBe(false);
  });

  it('verifyEmail should send the code and store the session', () => {
    const request: VerifyEmailRequest = {
      email: 'user@example.com',
      code: '123456',
    };

    const response: AuthResponse = {
      userId: 'user-id',
      email: 'user@example.com',
      token: 'jwt-token',
    };

    service.verifyEmail(request).subscribe();

    const httpRequest = httpMock.expectOne(
      `${apiConfig.baseUrl}/api/auth/verify-email`,
    );

    expect(httpRequest.request.method).toBe('POST');
    expect(httpRequest.request.body).toEqual(request);

    httpRequest.flush(response);

    expect(service.getToken()).toBe('jwt-token');
    expect(service.getUserEmail()).toBe('user@example.com');
    expect(service.isAuthenticated()).toBe(true);
  });

  it('resendVerification should send the email address', () => {
    const request: ResendVerificationRequest = {
      email: 'user@example.com',
    };

    const response = {
      message: 'Verification email sent.',
    };

    let actualResponse:
      | typeof response
      | undefined;

    service.resendVerification(request).subscribe((result) => {
      actualResponse = result;
    });

    const httpRequest = httpMock.expectOne(
      `${apiConfig.baseUrl}/api/auth/resend-verification`,
    );

    expect(httpRequest.request.method).toBe('POST');
    expect(httpRequest.request.body).toEqual(request);

    httpRequest.flush(response);

    expect(actualResponse).toEqual(response);
  });

  it('clearSession should remove authentication data', () => {
    sessionStorage.setItem(
      'task-management-token',
      'jwt-token',
    );

    sessionStorage.setItem(
      'task-management-user-email',
      'user@example.com',
    );

    service.clearSession();

    expect(service.getToken()).toBeNull();
    expect(service.getUserEmail()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
  });

  it('logout should clear the session', () => {
    sessionStorage.setItem(
      'task-management-token',
      'jwt-token',
    );

    sessionStorage.setItem(
      'task-management-user-email',
      'user@example.com',
    );

    service.logout();

    expect(service.getToken()).toBeNull();
    expect(service.getUserEmail()).toBeNull();
  });

  it('should not be authenticated without a token', () => {
    expect(service.isAuthenticated()).toBe(false);
  });
});