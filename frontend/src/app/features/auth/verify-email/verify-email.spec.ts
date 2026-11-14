import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
    ActivatedRoute,
    Router,
    convertToParamMap,
} from '@angular/router';
import { of, throwError } from 'rxjs';
import {
    afterEach,
    beforeEach,
    describe,
    expect,
    it,
    vi,
} from 'vitest';

import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../core/notifications/notification';
import { VerifyEmailComponent } from './verify-email';

describe('VerifyEmailComponent', () => {
    let component: VerifyEmailComponent;
    let fixture: ComponentFixture<VerifyEmailComponent>;

    const authServiceMock = {
        verifyEmail: vi.fn(),
        resendVerification: vi.fn(),
    };

    const notificationServiceMock = {
        showSuccess: vi.fn(),
        showError: vi.fn(),
    };

    const routerMock = {
        navigateByUrl: vi.fn(),
    };

    const activatedRouteMock = {
        snapshot: {
            queryParamMap: convertToParamMap({
                email: 'user@example.com',
            }),
        },
    };

    afterEach(() => {
        fixture.destroy();
    });

    beforeEach(async () => {
        authServiceMock.verifyEmail.mockReset();
        authServiceMock.resendVerification.mockReset();

        notificationServiceMock.showSuccess.mockReset();
        notificationServiceMock.showError.mockReset();

        routerMock.navigateByUrl.mockReset();

        await TestBed.configureTestingModule({
            imports: [VerifyEmailComponent],
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
                    useValue: activatedRouteMock,
                },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(
            VerifyEmailComponent,
        );

        component = fixture.componentInstance;
    });

    it('should create the component', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize the email from the query parameter', () => {
        expect(
            component.verifyForm.controls.email.value,
        ).toBe('user@example.com');
    });

    it('should reject a non-numeric verification code', () => {
        component.verifyForm.setValue({
            email: 'user@example.com',
            code: '12A456',
        });

        expect(
            component.verifyForm.controls.code.invalid,
        ).toBe(true);

        component.submit();

        expect(
            authServiceMock.verifyEmail,
        ).not.toHaveBeenCalled();
    });

    it('should reject a code with an invalid length', () => {
        component.verifyForm.setValue({
            email: 'user@example.com',
            code: '12345',
        });

        expect(
            component.verifyForm.controls.code.invalid,
        ).toBe(true);

        component.submit();

        expect(
            authServiceMock.verifyEmail,
        ).not.toHaveBeenCalled();
    });

    it('should verify the email successfully', () => {
        authServiceMock.verifyEmail.mockReturnValue(
            of({
                userId: 'user-id',
                email: 'user@example.com',
                token: 'jwt-token',
            }),
        );

        component.verifyForm.setValue({
            email: 'user@example.com',
            code: '123456',
        });

        component.submit();

        expect(
            authServiceMock.verifyEmail,
        ).toHaveBeenCalledWith({
            email: 'user@example.com',
            code: '123456',
        });

        expect(
            notificationServiceMock.showSuccess,
        ).toHaveBeenCalledWith(
            'Your email has been verified.',
            'Verification successful',
        );

        expect(
            routerMock.navigateByUrl,
        ).toHaveBeenCalledWith('/dashboard');

        expect(component.isSubmitting()).toBe(false);
    });

    it('should show the API error when verification fails', () => {
        authServiceMock.verifyEmail.mockReturnValue(
            throwError(
                () =>
                    new HttpErrorResponse({
                        status: 400,
                        error: {
                            message:
                                'The verification code is invalid.',
                        },
                    }),
            ),
        );

        component.verifyForm.setValue({
            email: 'user@example.com',
            code: '123456',
        });

        component.submit();

        expect(
            notificationServiceMock.showError,
        ).toHaveBeenCalledWith(
            'The verification code is invalid.',
            'Verification failed',
        );

        expect(
            routerMock.navigateByUrl,
        ).not.toHaveBeenCalled();

        expect(component.isSubmitting()).toBe(false);
    });

    it('should resend the verification code successfully', () => {
        authServiceMock.resendVerification.mockReturnValue(
            of({
                message: 'Verification email sent.',
            }),
        );

        component.verifyForm.controls.email.setValue(
            'user@example.com',
        );

        component.verifyForm.controls.email.setValue(
            'user@example.com',
        );

        component.resendCooldown.set(0);
        component.resend();

        expect(
            authServiceMock.resendVerification,
        ).toHaveBeenCalledWith({
            email: 'user@example.com',
        });

        expect(
            notificationServiceMock.showSuccess,
        ).toHaveBeenCalledWith(
            'A new verification code was sent.',
            'Code sent',
        );

        expect(component.isResending()).toBe(false);
    });

    it('should not resend when the email is invalid', () => {
        component.verifyForm.controls.email.setValue(
            'invalid-email',
        );

        component.resend();

        expect(
            authServiceMock.resendVerification,
        ).not.toHaveBeenCalled();

        expect(
            component.verifyForm.controls.email.touched,
        ).toBe(true);
    });

    it('should show an error when resending fails', () => {
        authServiceMock.resendVerification.mockReturnValue(
            throwError(
                () =>
                    new HttpErrorResponse({
                        status: 429,
                        error: {
                            message:
                                'Please wait before requesting another code.',
                        },
                    }),
            ),
        );

        component.verifyForm.controls.email.setValue(
            'user@example.com',
        );

        component.verifyForm.controls.email.setValue(
            'user@example.com',
        );

        component.resendCooldown.set(0);

        component.resend();

        expect(
            notificationServiceMock.showError,
        ).toHaveBeenCalledWith(
            'Please wait before requesting another code.',
            'Resend failed',
        );

        expect(component.isResending()).toBe(false);
    });
});