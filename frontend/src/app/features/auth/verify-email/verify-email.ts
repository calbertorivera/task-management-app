import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  OnDestroy,
  inject,
  signal,
} from '@angular/core';
import {
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, Subscription, timer } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../core/notifications/notification';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './verify-email.html',
  styleUrl: './verify-email.scss',
})
export class VerifyEmailComponent implements OnDestroy {
  private readonly formBuilder = inject(
    NonNullableFormBuilder,
  );
  private readonly authService = inject(AuthService);
  private readonly notificationService = inject(
    NotificationService,
  );
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private resendCooldownSubscription?: Subscription;

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
  readonly resendCooldown = signal(60);

  constructor() {
    this.startResendCooldown(60);
  }

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

  onCodeInput(event: Event): void {
    const input = event.target as HTMLInputElement;

    const numericCode = input.value
      .replace(/\D/g, '')
      .slice(0, 6);

    input.value = numericCode;

    this.verifyForm.controls.code.setValue(
      numericCode,
      {
        emitEvent: false,
      },
    );
  }

  resend(): void {
    const emailControl = this.verifyForm.controls.email;

    if (
      emailControl.invalid ||
      this.resendCooldown() > 0 ||
      this.isResending()
    ) {
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
          this.startResendCooldown(60);

          this.notificationService.showSuccess(
            'A new verification code was sent.',
            'Code sent',
          );
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 429) {
            const retryAfter = Number(
              error.error?.retryAfterSeconds ?? 60,
            );

            this.startResendCooldown(retryAfter);
          }

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

  formatCooldown(): string {
    const totalSeconds = this.resendCooldown();
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;

    return `${minutes.toString().padStart(2, '0')}:${
      seconds.toString().padStart(2, '0')
    }`;
  }

  ngOnDestroy(): void {
    this.resendCooldownSubscription?.unsubscribe();
  }

  private startResendCooldown(seconds: number): void {
    this.resendCooldownSubscription?.unsubscribe();

    this.resendCooldown.set(Math.max(0, seconds));

    if (seconds <= 0) {
      return;
    }

    this.resendCooldownSubscription = timer(
      1000,
      1000,
    ).subscribe(() => {
      const remaining = this.resendCooldown() - 1;

      this.resendCooldown.set(Math.max(0, remaining));

      if (remaining <= 0) {
        this.resendCooldownSubscription?.unsubscribe();
        this.resendCooldownSubscription = undefined;
      }
    });
  }
}