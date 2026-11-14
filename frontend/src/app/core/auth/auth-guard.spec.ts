import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { TestBed } from '@angular/core/testing';
import {
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest';

import { authGuard } from './auth-guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  const authServiceMock = {
    isAuthenticated: vi.fn(),
  };

  beforeEach(() => {
    authServiceMock.isAuthenticated.mockReset();

    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: authServiceMock,
        },
        provideRouter([]),
      ],
    });
  });

  it('should allow navigation when the user is authenticated', () => {
    authServiceMock.isAuthenticated.mockReturnValue(true);

    const result = TestBed.runInInjectionContext(() =>
      authGuard(
        {} as ActivatedRouteSnapshot,
        {} as RouterStateSnapshot,
      ),
    );

    expect(result).toBe(true);
    expect(authServiceMock.isAuthenticated).toHaveBeenCalledOnce();
  });

  it('should redirect to login when the user is not authenticated', () => {
    authServiceMock.isAuthenticated.mockReturnValue(false);

    const result = TestBed.runInInjectionContext(() =>
      authGuard(
        {} as ActivatedRouteSnapshot,
        {} as RouterStateSnapshot,
      ),
    );

    expect(result).toBeInstanceOf(UrlTree);

    const router = TestBed.inject(Router);

    expect(
      router.serializeUrl(result as UrlTree),
    ).toBe('/login');

    expect(authServiceMock.isAuthenticated).toHaveBeenCalledOnce();
  });
});