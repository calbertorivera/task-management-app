import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import {
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../core/notifications/notification';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './verify-email.html',
  styleUrl: './verify-email.scss',
})
export class VerifyEmailComponent {
  private readonly formBuilder = inject(
    NonNullableFormBuilder,
  );
  private readonly authService = inject(AuthService);
  private readonly notificationService = inject(
    NotificationService,
  );
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly verifyForm = this.formBuilder.group({
    email: [
      this.route.snapshot.queryParamMap.get('email') ?? '',
      [Validators.required, Validators.email],
    ],
    code: [
      '',
      [
        Validators.required,
        Validators.pattern(/^\d{6}$/),
      ],
    ],
  });

  readonly isSubmitting = signal(false);
  readonly isResending = signal(false);

  submit(): void {
    if (this.verifyForm.invalid) {
      this.verifyForm.markAllAsTouched();
      return;
    }

    const formValue = this.verifyForm.getRawValue();

    this.isSubmitting.set(true);

    this.authService
      .verifyEmail({
        email: formValue.email.trim(),
        code: formValue.code.trim(),
      })
      .pipe(
        finalize(() => {
          this.isSubmitting.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.notificationService.showSuccess(
            'Your email has been verified.',
            'Verification successful',
          );

          void this.router.navigateByUrl('/dashboard');
        },
        error: (error: HttpErrorResponse) => {
          const message =
            error.error?.message ??
            'The verification code is invalid or expired.';

          this.notificationService.showError(
            message,
            'Verification failed',
          );
        },
      });
  }

  resend(): void {
    const emailControl = this.verifyForm.controls.email;

    if (emailControl.invalid) {
      emailControl.markAsTouched();
      return;
    }

    this.isResending.set(true);

    this.authService
      .resendVerification({
        email: emailControl.value.trim(),
      })
      .pipe(
        finalize(() => {
          this.isResending.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.notificationService.showSuccess(
            'A new verification code was sent.',
            'Code sent',
          );
        },
        error: (error: HttpErrorResponse) => {
          const message =
            error.error?.message ??
            'Unable to resend the verification code.';

          this.notificationService.showError(
            message,
            'Resend failed',
          );
        },
      });
  }
}