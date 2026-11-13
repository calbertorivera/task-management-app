import { Component, inject } from '@angular/core';
import { NotificationService } from '../../../core/notifications/notification';

@Component({
  selector: 'app-toast',
  standalone: true,
  templateUrl: './toast.html',
  styleUrl: './toast.scss',
})
export class Toast {
  private readonly notificationService = inject(
    NotificationService,
  );

  protected readonly notification =
    this.notificationService.notification;

  protected close(): void {
    this.notificationService.clear();
  }
}