import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { NotificationService } from '../../../core/notifications/notification';
import { Task, TaskStatus } from '../../../core/tasks/task.model';
import { TaskService } from '../../../core/tasks/task.service';
import { ConfirmDialog } from '../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [DatePipe, RouterLink, ConfirmDialog],
  templateUrl: './task-list.html',
  styleUrl: './task-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskList {
  private readonly taskService = inject(TaskService);
  private readonly notificationService = inject(NotificationService);

  readonly tasks = signal<ReadonlyArray<Task>>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal('');
  readonly deletingTaskId = signal<string | null>(null);
  readonly taskToDelete = signal<Task | null>(null);

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
        },
      });
  }

  requestDelete(task: Task): void {
  this.taskToDelete.set(task);
}

cancelDelete(): void {
  this.taskToDelete.set(null);
}

get deleteMessage(): string {
  const task = this.taskToDelete();

  return task
    ? `The task "${task.title}" will be permanently deleted.`
    : '';
}

confirmDelete(): void {
  const task = this.taskToDelete();

  if (!task) {
    return;
  }

  this.taskToDelete.set(null);
  this.deletingTaskId.set(task.id);

  this.taskService
    .delete(task.id)
    .pipe(finalize(() => this.deletingTaskId.set(null)))
    .subscribe({
      next: () => {
        this.tasks.update((tasks) =>
          tasks.filter((item) => item.id !== task.id),
        );

        this.notificationService.showSuccess(
          'Task deleted successfully.',
          'Task deleted',
        );
      },
      error: () => {
        this.notificationService.showError(
          'We could not delete the task.',
          'Delete failed',
        );
      },
    });
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

  isCompleted(status: TaskStatus): boolean {
    return status === TaskStatus.Completed;
  }
}