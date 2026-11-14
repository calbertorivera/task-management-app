import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import {
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest';

import { NotificationService } from '../../../core/notifications/notification';
import {
  Task,
  TaskStatus,
} from '../../../core/tasks/task.model';
import { TaskService } from '../../../core/tasks/task.service';
import { TaskList } from './task-list';

describe('TaskList', () => {
  let component: TaskList;
  let fixture: ComponentFixture<TaskList>;

  const pendingTask: Task = {
    id: 'pending-task-id',
    title: 'Pending task',
    description: 'Pending task description.',
    status: TaskStatus.Pending,
    dueDate: '2026-12-01T10:00:00Z',
    userId: 'user-id',
  };

  const completedTask: Task = {
    id: 'completed-task-id',
    title: 'Completed task',
    description: 'Completed task description.',
    status: TaskStatus.Completed,
    dueDate: '2026-11-01T10:00:00Z',
    userId: 'user-id',
  };

  const tasks: ReadonlyArray<Task> = [
    pendingTask,
    completedTask,
  ];

  const taskServiceMock = {
    getAll: vi.fn(),
    delete: vi.fn(),
  };

  const notificationServiceMock = {
    showSuccess: vi.fn(),
    showError: vi.fn(),
  };

  beforeEach(async () => {
    taskServiceMock.getAll.mockReset();
    taskServiceMock.delete.mockReset();

    notificationServiceMock.showSuccess.mockReset();
    notificationServiceMock.showError.mockReset();

    taskServiceMock.getAll.mockReturnValue(of(tasks));

    await TestBed.configureTestingModule({
      imports: [TaskList],
      providers: [
        {
          provide: TaskService,
          useValue: taskServiceMock,
        },
        {
          provide: NotificationService,
          useValue: notificationServiceMock,
        },
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TaskList);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should load tasks when the component is created', () => {
    expect(taskServiceMock.getAll).toHaveBeenCalledOnce();
    expect(component.tasks()).toEqual(tasks);
    expect(component.isLoading()).toBe(false);
    expect(component.errorMessage()).toBe('');
  });

  it('should show an error when tasks cannot be loaded', () => {
    taskServiceMock.getAll.mockReturnValue(
      throwError(() => new Error('Request failed')),
    );

    component.loadTasks();

    expect(component.errorMessage()).toBe(
      'We could not load your tasks. Please try again.',
    );
    expect(component.isLoading()).toBe(false);
  });

  it('should set the task selected for deletion', () => {
    component.requestDelete(pendingTask);

    expect(component.taskToDelete()).toEqual(
      pendingTask,
    );

    expect(component.deleteMessage).toBe(
      'The task "Pending task" will be permanently deleted.',
    );
  });

  it('should cancel the delete operation', () => {
    component.requestDelete(pendingTask);

    component.cancelDelete();

    expect(component.taskToDelete()).toBeNull();
    expect(component.deleteMessage).toBe('');
  });

  it('should not delete a task when no task is selected', () => {
    component.confirmDelete();

    expect(taskServiceMock.delete).not.toHaveBeenCalled();
  });

  it('should delete the selected task successfully', () => {
    taskServiceMock.delete.mockReturnValue(of(void 0));

    component.requestDelete(pendingTask);
    component.confirmDelete();

    expect(taskServiceMock.delete).toHaveBeenCalledWith(
      pendingTask.id,
    );

    expect(component.tasks()).toEqual([
      completedTask,
    ]);

    expect(component.taskToDelete()).toBeNull();
    expect(component.deletingTaskId()).toBeNull();

    expect(
      notificationServiceMock.showSuccess,
    ).toHaveBeenCalledWith(
      'Task deleted successfully.',
      'Task deleted',
    );
  });

  it('should show an error when deleting a task fails', () => {
    taskServiceMock.delete.mockReturnValue(
      throwError(() => new Error('Delete failed')),
    );

    component.requestDelete(pendingTask);
    component.confirmDelete();

    expect(component.tasks()).toEqual(tasks);
    expect(component.deletingTaskId()).toBeNull();

    expect(
      notificationServiceMock.showError,
    ).toHaveBeenCalledWith(
      'We could not delete the task.',
      'Delete failed',
    );
  });

  it('should return the correct label for each task status', () => {
    expect(
      component.statusLabel(TaskStatus.Pending),
    ).toBe('Pending');

    expect(
      component.statusLabel(TaskStatus.InProgress),
    ).toBe('In progress');

    expect(
      component.statusLabel(TaskStatus.Completed),
    ).toBe('Completed');
  });

  it('should identify completed tasks', () => {
    expect(
      component.isCompleted(TaskStatus.Completed),
    ).toBe(true);

    expect(
      component.isCompleted(TaskStatus.Pending),
    ).toBe(false);
  });
});