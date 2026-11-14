import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
    ActivatedRoute,
    Router,
} from '@angular/router';
import { of, throwError } from 'rxjs';
import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
} from 'vitest';

import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../core/notifications/notification';
import { LoginComponent } from './login';

describe('LoginComponent', () => {
    let component: LoginComponent;
    let fixture: ComponentFixture<LoginComponent>;

    const authServiceMock = {
        login: vi.fn(),
    };

    const notificationServiceMock = {
        showSuccess: vi.fn(),
        showError: vi.fn(),
    };

    const routerMock = {
        navigateByUrl: vi.fn(),
    };

    beforeEach(async () => {
        authServiceMock.login.mockReset();

        notificationServiceMock.showSuccess.mockReset();
        notificationServiceMock.showError.mockReset();

        routerMock.navigateByUrl.mockReset();

        await TestBed.configureTestingModule({
            imports: [LoginComponent],
            providers: [
                {
                    provide: AuthService,
                    useValue: authServiceMock,
                },
                {
                    provide: NotificationService,
                    useValue: notificationServiceMock,
                },
                {
                    provide: Router,
                    useValue: routerMock,
                },
                {
                    provide: ActivatedRoute,
                    useValue: {},
                },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(LoginComponent);
        component = fixture.componentInstance;
    });

    it('should create the component', () => {
        expect(component).toBeTruthy();
    });

    it('should not submit when the form is invalid', () => {
        component.loginForm.setValue({
            email: 'invalid-email',
            password: '123',
        });

        component.submit();

        expect(authServiceMock.login).not.toHaveBeenCalled();
        expect(
            component.loginForm.controls.email.touched,
        ).toBe(true);
        expect(
            component.loginForm.controls.password.touched,
        ).toBe(true);
    });

    it('should login successfully and navigate to the dashboard', () => {
        const credentials = {
            email: 'user@example.com',
            password: 'Password123!',
        };

        authServiceMock.login.mockReturnValue(
            of({
                userId: 'user-id',
                email: credentials.email,
                token: 'jwt-token',
            }),
        );

        component.loginForm.setValue(credentials);

        component.submit();

        expect(authServiceMock.login).toHaveBeenCalledWith(
            credentials,
        );

        expect(
            notificationServiceMock.showSuccess,
        ).toHaveBeenCalledWith(
            'Welcome back!',
            'Login successful',
        );

        expect(
            routerMock.navigateByUrl,
        ).toHaveBeenCalledWith('/dashboard');
    });

    it('should show the API error message when login fails', () => {
        const credentials = {
            email: 'user@example.com',
            password: 'WrongPassword',
        };

        authServiceMock.login.mockReturnValue(
            throwError(
                () =>
                    new HttpErrorResponse({
                        status: 401,
                        error: {
                            message: 'Invalid email or password.',
                        },
                    }),
            ),
        );

        component.loginForm.setValue(credentials);

        component.submit();

        expect(
            notificationServiceMock.showError,
        ).toHaveBeenCalledWith(
            'Invalid email or password.',
            'Sign in failed',
        );

        expect(
            routerMock.navigateByUrl,
        ).not.toHaveBeenCalled();
    });

    it('should show a fallback error when the API does not return a message', () => {
        authServiceMock.login.mockReturnValue(
            throwError(
                () =>
                    new HttpErrorResponse({
                        status: 500,
                        error: {},
                    }),
            ),
        );

        component.loginForm.setValue({
            email: 'user@example.com',
            password: 'Password123!',
        });

        component.submit();

        expect(
            notificationServiceMock.showError,
        ).toHaveBeenCalledWith(
            'Unable to sign in. Please try again.',
            'Sign in failed',
        );
    });
});