import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  provideRouter,
  Router,
} from '@angular/router';
import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest';

import { AuthService } from '../../../core/auth/auth.service';
import { AppShell } from './app-shell';

describe('AppShell', () => {
  let component: AppShell;
  let fixture: ComponentFixture<AppShell>;
  let router: Router;

  const authServiceMock = {
    getUserEmail: vi.fn(),
    clearSession: vi.fn(),
  };

  beforeEach(async () => {
    authServiceMock.getUserEmail.mockReset();
    authServiceMock.clearSession.mockReset();

    await TestBed.configureTestingModule({
      imports: [AppShell],
      providers: [
        {
          provide: AuthService,
          useValue: authServiceMock,
        },
        provideRouter([]),
      ],
    }).compileComponents();

    router = TestBed.inject(Router);

    vi.spyOn(router, 'navigateByUrl')
      .mockResolvedValue(true);
  });

  afterEach(() => {
    fixture?.destroy();
  });

  function createComponent(): void {
    fixture = TestBed.createComponent(AppShell);
    component = fixture.componentInstance;
  }

  it('should create the component', () => {
    authServiceMock.getUserEmail.mockReturnValue(
      'user@example.com',
    );

    createComponent();

    expect(component).toBeTruthy();
  });

  it('should expose the authenticated user email', () => {
    authServiceMock.getUserEmail.mockReturnValue(
      'user@example.com',
    );

    createComponent();

    expect(component.userEmail).toBe(
      'user@example.com',
    );
  });

  it('should use a fallback when the user email is not available', () => {
    authServiceMock.getUserEmail.mockReturnValue(null);

    createComponent();

    expect(component.userEmail).toBe('Signed-in user');
  });

  it('should return the user initial in uppercase', () => {
    authServiceMock.getUserEmail.mockReturnValue(
      'carlos@example.com',
    );

    createComponent();

    expect(component.userInitial).toBe('C');
  });

  it('should toggle the menu state', () => {
    authServiceMock.getUserEmail.mockReturnValue(
      'user@example.com',
    );

    createComponent();

    expect(component.isMenuOpen()).toBe(false);

    component.toggleMenu();

    expect(component.isMenuOpen()).toBe(true);

    component.toggleMenu();

    expect(component.isMenuOpen()).toBe(false);
  });

  it('should close the menu', () => {
    authServiceMock.getUserEmail.mockReturnValue(
      'user@example.com',
    );

    createComponent();

    component.toggleMenu();
    component.closeMenu();

    expect(component.isMenuOpen()).toBe(false);
  });

  it('should clear the session and navigate to login on logout', () => {
    authServiceMock.getUserEmail.mockReturnValue(
      'user@example.com',
    );

    createComponent();

    component.toggleMenu();
    component.logout();

    expect(
      authServiceMock.clearSession,
    ).toHaveBeenCalledOnce();

    expect(component.isMenuOpen()).toBe(false);

    expect(
      router.navigateByUrl,
    ).toHaveBeenCalledWith('/login');
  });
});