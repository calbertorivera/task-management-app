import { Injectable, signal } from '@angular/core';

export type NotificationType = 'success' | 'error' | 'info';

export interface NotificationMessage {
  id: number;
  type: NotificationType;
  title: string;
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private readonly notificationSignal =
    signal<NotificationMessage | null>(null);

  readonly notification = this.notificationSignal.asReadonly();

  private timeoutId?: ReturnType<typeof setTimeout>;

  show(
    type: NotificationType,
    message: string,
    title: string,
    duration = 5000,
  ): void {
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
    }

    this.notificationSignal.set({
      id: Date.now(),
      type,
      title,
      message,
    });

    this.timeoutId = setTimeout(() => {
      this.clear();
    }, duration);
  }

  showError(
    message: string,
    title = 'Something went wrong',
  ): void {
    this.show('error', message, title);
  }

  showSuccess(
    message: string,
    title = 'Success',
  ): void {
    this.show('success', message, title);
  }

  showInfo(
    message: string,
    title = 'Information',
  ): void {
    this.show('info', message, title);
  }

  clear(): void {
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
      this.timeoutId = undefined;
    }

    this.notificationSignal.set(null);
  }
}