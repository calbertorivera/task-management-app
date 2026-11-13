import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import {
  ActivatedRoute,
  Router,
  RouterLink,
} from '@angular/router';
import { finalize } from 'rxjs';

import { NotificationService } from '../../../core/notifications/notification';
import {
  CreateTaskRequest,
  TaskStatus,
} from '../../../core/tasks/task.model';
import { TaskService } from '../../../core/tasks/task.service';

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './task-form.html',
  styleUrl: './task-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskForm {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly taskService = inject(TaskService);
  private readonly notificationService = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly TaskStatus = TaskStatus;
  readonly taskId = this.route.snapshot.paramMap.get('id');
  readonly isEditMode = signal(this.taskId !== null);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);

  readonly taskForm = this.formBuilder.group({
    title: ['', [Validators.required, Validators.maxLength(120)]],
    description: ['', [Validators.required, Validators.maxLength(500)]],
    dueDate: ['', Validators.required],
    status: [TaskStatus.Pending],
  });

  constructor() {
    if (this.taskId) {
      this.loadTask(this.taskId);
    }
  }

  loadTask(taskId: string): void {
    this.isLoading.set(true);

    this.taskService
      .getById(taskId)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (task) => {
          this.taskForm.patchValue({
            title: task.title,
            description: task.description,
            dueDate: this.toDateTimeLocalValue(task.dueDate),
            status: task.status,
          });
        },
        error: () => {
          this.notificationService.showError(
            'We could not load the task.',
            'Task unavailable',
          );

          void this.router.navigateByUrl('/tasks');
        },
      });
  }

  private toDateTimeLocalValue(value: string): string {
  return value ? value.substring(0, 16) : '';
  }

  submit(): void {
    if (this.taskForm.invalid) {
      this.taskForm.markAllAsTouched();
      return;
    }

    const values = this.taskForm.getRawValue();

    this.isSaving.set(true);

    const operation = this.taskId
      ? this.taskService.update(this.taskId, {
          title: values.title,
          description: values.description,
          dueDate: values.dueDate,
          status: Number(values.status) as TaskStatus,
        })
      : this.taskService.create({
          title: values.title,
          description: values.description,
          dueDate: values.dueDate,
        } satisfies CreateTaskRequest);

    operation
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: () => {
          this.notificationService.showSuccess(
            this.isEditMode()
              ? 'Task updated successfully.'
              : 'Task created successfully.',
            this.isEditMode() ? 'Task updated' : 'Task created',
          );

          void this.router.navigateByUrl('/tasks');
        },
        error: () => {
          this.notificationService.showError(
            'We could not save the task.',
            'Save failed',
          );
        },
      });
  }
}