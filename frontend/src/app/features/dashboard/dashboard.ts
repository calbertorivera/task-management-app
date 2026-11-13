import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { finalize } from 'rxjs';

import { NotificationService } from '../../core/notifications/notification';
import { Task, TaskStatus } from '../../core/tasks/task.model';
import { TaskService } from '../../core/tasks/task.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Dashboard {
  private readonly taskService = inject(TaskService);
  private readonly notificationService = inject(NotificationService);

  readonly tasks = signal<ReadonlyArray<Task>>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal('');

  constructor() {
    this.loadTasks();
  }

  loadTasks(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.taskService
      .getAll()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (tasks) => this.tasks.set(tasks),
        error: () => {
          this.errorMessage.set(
            'We could not load your tasks. Please try again.',
          );

          this.notificationService.showError(
            'Unable to load tasks.',
            'Dashboard error',
          );
        },
      });
  }

  get completedCount(): number {
    return this.tasks().filter(
      (task) => task.status === TaskStatus.Completed,
    ).length;
  }

  get pendingCount(): number {
    return this.tasks().filter(
      (task) => task.status !== TaskStatus.Completed,
    ).length;
  }

  isCompleted(task: Task): boolean {
    return task.status === TaskStatus.Completed;
  }

  statusLabel(status: TaskStatus): string {
    if (status === TaskStatus.Completed) {
      return 'Completed';
    }

    if (status === TaskStatus.InProgress) {
      return 'In progress';
    }

    return 'Pending';
  }
}