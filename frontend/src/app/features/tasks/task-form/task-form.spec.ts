import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  ActivatedRoute,
  Router,
  convertToParamMap,
} from '@angular/router';
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
import { TaskForm } from './task-form';

describe('TaskForm', () => {
  let component: TaskForm;
  let fixture: ComponentFixture<TaskForm>;

  const task: Task = {
    id: 'task-id',
    title: 'Prepare presentation',
    description: 'Complete the technical exercise presentation.',
    status: TaskStatus.Pending,
    dueDate: '2026-12-01T10:00:00Z',
    userId: 'user-id',
  };

  const taskServiceMock = {
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  };

  const notificationServiceMock = {
    showSuccess: vi.fn(),
    showError: vi.fn(),
  };

  const routerMock = {
    navigateByUrl: vi.fn(),
  };

  const activatedRouteMock = {
    snapshot: {
      paramMap: convertToParamMap({}),
    },
  };

  beforeEach(async () => {
    taskServiceMock.getById.mockReset();
    taskServiceMock.create.mockReset();
    taskServiceMock.update.mockReset();

    notificationServiceMock.showSuccess.mockReset();
    notificationServiceMock.showError.mockReset();

    routerMock.navigateByUrl.mockReset();

    activatedRouteMock.snapshot.paramMap =
      convertToParamMap({});

    await TestBed.configureTestingModule({
      imports: [TaskForm],
      providers: [
        {
          provide: TaskService,
          useValue: taskServiceMock,
        },
        {
          provide: NotificationService,
          useValue: notificationServiceMock,
        },
        {
          provide: ActivatedRoute,
          useValue: activatedRouteMock,
        },
        {
          provide: Router,
          useValue: routerMock,
        },
      ],
    }).compileComponents();
  });

  function createComponent(taskId?: string): void {
    activatedRouteMock.snapshot.paramMap =
      convertToParamMap(
        taskId
          ? { id: taskId }
          : {},
      );

    fixture = TestBed.createComponent(TaskForm);
    component = fixture.componentInstance;
  }

  it('should create the component in create mode', () => {
    createComponent();

    expect(component).toBeTruthy();
    expect(component.taskId).toBeNull();
    expect(component.isEditMode()).toBe(false);
    expect(component.taskForm.controls.status.value).toBe(
      TaskStatus.Pending,
    );
  });

  it('should not save when the form is invalid', () => {
    createComponent();

    component.taskForm.setValue({
      title: '',
      description: '',
      dueDate: '',
      status: TaskStatus.Pending,
    });

    component.submit();

    expect(taskServiceMock.create).not.toHaveBeenCalled();
    expect(taskServiceMock.update).not.toHaveBeenCalled();
    expect(
      component.taskForm.controls.title.touched,
    ).toBe(true);
    expect(
      component.taskForm.controls.description.touched,
    ).toBe(true);
    expect(
      component.taskForm.controls.dueDate.touched,
    ).toBe(true);
  });

  it('should create a task successfully', () => {
    taskServiceMock.create.mockReturnValue(of(task));

    createComponent();

    component.taskForm.setValue({
      title: 'New task',
      description: 'New task description.',
      dueDate: '2026-12-01T10:00',
      status: TaskStatus.Pending,
    });

    component.submit();

    expect(taskServiceMock.create).toHaveBeenCalledWith({
      title: 'New task',
      description: 'New task description.',
      dueDate: '2026-12-01T10:00',
    });

    expect(
      notificationServiceMock.showSuccess,
    ).toHaveBeenCalledWith(
      'Task created successfully.',
      'Task created',
    );

    expect(
      routerMock.navigateByUrl,
    ).toHaveBeenCalledWith('/tasks');

    expect(component.isSaving()).toBe(false);
  });

  it('should show an error when task creation fails', () => {
    taskServiceMock.create.mockReturnValue(
      throwError(() => new Error('Create failed')),
    );

    createComponent();

    component.taskForm.setValue({
      title: 'New task',
      description: 'New task description.',
      dueDate: '2026-12-01T10:00',
      status: TaskStatus.Pending,
    });

    component.submit();

    expect(
      notificationServiceMock.showError,
    ).toHaveBeenCalledWith(
      'We could not save the task.',
      'Save failed',
    );

    expect(routerMock.navigateByUrl).not.toHaveBeenCalled();
    expect(component.isSaving()).toBe(false);
  });

  it('should load an existing task in edit mode', () => {
    taskServiceMock.getById.mockReturnValue(of(task));

    createComponent(task.id);

    expect(component.taskId).toBe(task.id);
    expect(component.isEditMode()).toBe(true);

    expect(taskServiceMock.getById).toHaveBeenCalledWith(
      task.id,
    );

    expect(component.taskForm.getRawValue()).toEqual({
      title: task.title,
      description: task.description,
      dueDate: '2026-12-01T10:00',
      status: task.status,
    });

    expect(component.isLoading()).toBe(false);
  });

  it('should show an error and navigate to tasks when loading fails', () => {
    taskServiceMock.getById.mockReturnValue(
      throwError(() => new Error('Load failed')),
    );

    createComponent(task.id);

    expect(
      notificationServiceMock.showError,
    ).toHaveBeenCalledWith(
      'We could not load the task.',
      'Task unavailable',
    );

    expect(
      routerMock.navigateByUrl,
    ).toHaveBeenCalledWith('/tasks');

    expect(component.isLoading()).toBe(false);
  });

  it('should update an existing task successfully', () => {
    taskServiceMock.getById.mockReturnValue(of(task));
    taskServiceMock.update.mockReturnValue(of(task));

    createComponent(task.id);

    component.taskForm.setValue({
      title: 'Updated task',
      description: 'Updated task description.',
      dueDate: '2026-12-02T12:30',
      status: TaskStatus.Completed,
    });

    component.submit();

    expect(taskServiceMock.update).toHaveBeenCalledWith(
      task.id,
      {
        title: 'Updated task',
        description: 'Updated task description.',
        dueDate: '2026-12-02T12:30',
        status: TaskStatus.Completed,
      },
    );

    expect(
      notificationServiceMock.showSuccess,
    ).toHaveBeenCalledWith(
      'Task updated successfully.',
      'Task updated',
    );

    expect(
      routerMock.navigateByUrl,
    ).toHaveBeenCalledWith('/tasks');

    expect(component.isSaving()).toBe(false);
  });

  it('should show an error when task update fails', () => {
    taskServiceMock.getById.mockReturnValue(of(task));

    taskServiceMock.update.mockReturnValue(
      throwError(() => new Error('Update failed')),
    );

    createComponent(task.id);

    component.taskForm.setValue({
      title: 'Updated task',
      description: 'Updated task description.',
      dueDate: '2026-12-02T12:30',
      status: TaskStatus.InProgress,
    });

    component.submit();

    expect(
      notificationServiceMock.showError,
    ).toHaveBeenCalledWith(
      'We could not save the task.',
      'Save failed',
    );

    expect(routerMock.navigateByUrl).not.toHaveBeenCalled();
    expect(component.isSaving()).toBe(false);
  });
});