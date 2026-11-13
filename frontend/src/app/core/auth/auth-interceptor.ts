import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { apiConfig } from '../config/api.config';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (
  request,
  next,
) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const isApiRequest = request.url.startsWith(
    apiConfig.baseUrl,
  );

  const isAuthenticationRequest =
    request.url.includes('/api/auth/');

  const token = authService.getToken();

  const requestWithBearer =
    isApiRequest &&
    !isAuthenticationRequest &&
    token
      ? request.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`,
          },
        })
      : request;

  return next(requestWithBearer).pipe(
    catchError((error) => {
      if (
        error.status === 401 &&
        !isAuthenticationRequest
      ) {
        authService.clearSession();

        void router.navigateByUrl('/login');
      }

      return throwError(() => error);
    }),
  );
};