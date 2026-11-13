import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  input,
  output,
} from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDialog {
  readonly isOpen = input(false);
  readonly title = input('Confirm action');
  readonly message = input('');
  readonly confirmLabel = input('Confirm');
  readonly cancelLabel = input('Cancel');

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  @HostListener('document:keydown.escape')
  closeWithEscape(): void {
    if (this.isOpen()) {
      this.cancelled.emit();
    }
  }
}