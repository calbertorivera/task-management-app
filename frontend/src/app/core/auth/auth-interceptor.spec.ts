import {
  HttpClient,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest';

import { apiConfig } from '../config/api.config';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth-interceptor';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;

  const authServiceMock = {
    getToken: vi.fn(),
    clearSession: vi.fn(),
  };

  const routerMock = {
    navigateByUrl: vi.fn(),
  };

  beforeEach(() => {
    authServiceMock.getToken.mockReset();
    authServiceMock.clearSession.mockReset();
    routerMock.navigateByUrl.mockReset();

    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: authServiceMock,
        },
        {
          provide: Router,
          useValue: routerMock,
        },
        provideHttpClient(
          withInterceptors([authInterceptor]),
        ),
        provideHttpClientTesting(),
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should add the bearer token to protected API requests', () => {
    authServiceMock.getToken.mockReturnValue(
      'jwt-token',
    );

    const url = `${apiConfig.baseUrl}/api/tasks`;

    httpClient.get(url).subscribe();

    const request = httpMock.expectOne(url);

    expect(
      request.request.headers.get('Authorization'),
    ).toBe('Bearer jwt-token');

    request.flush([]);
  });

  it('should not add a bearer token when no token exists', () => {
    authServiceMock.getToken.mockReturnValue(null);

    const url = `${apiConfig.baseUrl}/api/tasks`;

    httpClient.get(url).subscribe();

    const request = httpMock.expectOne(url);

    expect(
      request.request.headers.has('Authorization'),
    ).toBe(false);

    request.flush([]);
  });

  it('should not add a bearer token to authentication requests', () => {
    authServiceMock.getToken.mockReturnValue(
      'jwt-token',
    );

    const url = `${apiConfig.baseUrl}/api/auth/login`;

    httpClient
      .post(url, {
        email: 'user@example.com',
        password: 'Password123!',
      })
      .subscribe();

    const request = httpMock.expectOne(url);

    expect(
      request.request.headers.has('Authorization'),
    ).toBe(false);

    request.flush({
      userId: 'user-id',
      email: 'user@example.com',
      token: 'new-token',
    });
  });

  it('should not add a bearer token to external requests', () => {
    authServiceMock.getToken.mockReturnValue(
      'jwt-token',
    );

    const url = 'https://external.example.com/data';

    httpClient.get(url).subscribe();

    const request = httpMock.expectOne(url);

    expect(
      request.request.headers.has('Authorization'),
    ).toBe(false);

    request.flush({});
  });

  it('should clear the session and redirect to login on a protected 401 response', () => {
    authServiceMock.getToken.mockReturnValue(
      'jwt-token',
    );

    const url = `${apiConfig.baseUrl}/api/tasks`;

    httpClient.get(url).subscribe({
      error: () => undefined,
    });

    const request = httpMock.expectOne(url);

    request.flush(
      {
        message: 'Unauthorized',
      },
      {
        status: 401,
        statusText: 'Unauthorized',
      },
    );

    expect(
      authServiceMock.clearSession,
    ).toHaveBeenCalledOnce();

    expect(
      routerMock.navigateByUrl,
    ).toHaveBeenCalledWith('/login');
  });

  it('should not clear the session on a 401 authentication response', () => {
    const url = `${apiConfig.baseUrl}/api/auth/login`;

    httpClient
      .post(url, {
        email: 'user@example.com',
        password: 'WrongPassword',
      })
      .subscribe({
        error: () => undefined,
      });

    const request = httpMock.expectOne(url);

    request.flush(
      {
        message: 'Invalid email or password.',
      },
      {
        status: 401,
        statusText: 'Unauthorized',
      },
    );

    expect(
      authServiceMock.clearSession,
    ).not.toHaveBeenCalled();

    expect(
      routerMock.navigateByUrl,
    ).not.toHaveBeenCalled();
  });
});