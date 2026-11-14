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
import { RegisterComponent } from './register';

describe('RegisterComponent', () => {
    let component: RegisterComponent;
    let fixture: ComponentFixture<RegisterComponent>;

    const authServiceMock = {
        register: vi.fn(),
    };

    const notificationServiceMock = {
        showSuccess: vi.fn(),
        showError: vi.fn(),
    };

    const routerMock = {
        navigate: vi.fn(),
    };

    beforeEach(async () => {
        authServiceMock.register.mockReset();

        notificationServiceMock.showSuccess.mockReset();
        notificationServiceMock.showError.mockReset();

        routerMock.navigate.mockReset();

        await TestBed.configureTestingModule({
            imports: [RegisterComponent],
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

        fixture = TestBed.createComponent(RegisterComponent);
        component = fixture.componentInstance;
    });

    it('should create the component', () => {
        expect(component).toBeTruthy();
    });

    it('should not submit when the form is invalid', () => {
        component.registerForm.setValue({
            email: 'invalid-email',
            password: '123',
            confirmPassword: '456',
        });

        component.submit();

        expect(authServiceMock.register).not.toHaveBeenCalled();
        expect(
            component.registerForm.controls.email.touched,
        ).toBe(true);
        expect(
            component.registerForm.controls.password.touched,
        ).toBe(true);
        expect(
            component.registerForm.controls.confirmPassword.touched,
        ).toBe(true);
    });

    it('should invalidate the form when passwords do not match', () => {
        component.registerForm.setValue({
            email: 'user@example.com',
            password: 'Password123!',
            confirmPassword: 'Different123!',
        });

        expect(
            component.registerForm.hasError('passwordsMismatch'),
        ).toBe(true);

        component.submit();

        expect(authServiceMock.register).not.toHaveBeenCalled();
    });

    it('should register successfully and navigate to email verification', () => {
        const response = {
            email: 'user@example.com',
            message: 'Registration successful.',
        };

        authServiceMock.register.mockReturnValue(
            of(response),
        );

        component.registerForm.setValue({
  email: 'user@example.com',
  password: 'Password123!',
  confirmPassword: 'Password123!',
});

        component.submit();

        expect(authServiceMock.register).toHaveBeenCalledWith({
            email: 'user@example.com',
            password: 'Password123!',
        });

        expect(
            notificationServiceMock.showSuccess,
        ).toHaveBeenCalledWith(
            'Check your inbox to verify your account.',
            'Registration successful',
        );

        expect(routerMock.navigate).toHaveBeenCalledWith(
            ['/verify-email'],
            {
                queryParams: {
                    email: 'user@example.com',
                },
            },
        );

        expect(component.isSubmitting()).toBe(false);
    });

    it('should show the API error message when registration fails', () => {
        authServiceMock.register.mockReturnValue(
            throwError(
                () =>
                    new HttpErrorResponse({
                        status: 409,
                        error: {
                            message:
                                'A user with this email already exists.',
                        },
                    }),
            ),
        );

        component.registerForm.setValue({
            email: 'user@example.com',
            password: 'Password123!',
            confirmPassword: 'Password123!',
        });

        component.submit();

        expect(
            notificationServiceMock.showError,
        ).toHaveBeenCalledWith(
            'A user with this email already exists.',
            'Registration failed',
        );

        expect(routerMock.navigate).not.toHaveBeenCalled();
        expect(component.isSubmitting()).toBe(false);
    });

    it('should show a fallback error when the API does not return a message', () => {
        authServiceMock.register.mockReturnValue(
            throwError(
                () =>
                    new HttpErrorResponse({
                        status: 500,
                        error: {},
                    }),
            ),
        );

        component.registerForm.setValue({
            email: 'user@example.com',
            password: 'Password123!',
            confirmPassword: 'Password123!',
        });

        component.submit();

        expect(
            notificationServiceMock.showError,
        ).toHaveBeenCalledWith(
            'Unable to create your account. Please try again.',
            'Registration failed',
        );
    });
});