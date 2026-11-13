import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { DatabaseHealthService } from './core/health/database-health';
import { Toast } from './shared/components/toast/toast';
import { finalize } from 'rxjs';

@Component({
  imports: [RouterOutlet, Toast],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  private readonly databaseHealthService = inject(
    DatabaseHealthService,
  );

  protected readonly isPreparingDatabase = signal(true);
  protected readonly databaseError = signal(false);
  protected readonly isRetrying = signal(false);

  constructor() {
    this.checkDatabase();
  }

  protected retryDatabase(): void {
  if (this.isRetrying()) {
    return;
  }

  this.checkDatabase();
}

  private checkDatabase(): void {
  this.isPreparingDatabase.set(true);
  this.isRetrying.set(true);
  this.databaseError.set(false);

  this.databaseHealthService
    .check()
    .pipe(
      finalize(() => {
        this.isRetrying.set(false);
      }),
    )
    .subscribe({
      next: () => {
        this.isPreparingDatabase.set(false);
      },
      error: () => {
        this.isPreparingDatabase.set(false);
        this.databaseError.set(true);
      },
    });
}
}