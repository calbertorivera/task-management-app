import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import {
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest';

import { NotificationService } from '../../core/notifications/notification';
import {
  Task,
  TaskStatus,
} from '../../core/tasks/task.model';
import { TaskService } from '../../core/tasks/task.service';
import { Dashboard } from './dashboard';

describe('Dashboard', () => {
  let component: Dashboard;
  let fixture: ComponentFixture<Dashboard>;

  const tasks: ReadonlyArray<Task> = [
    {
      id: 'completed-earlier',
      title: 'Completed earlier',
      description: 'Completed task.',
      status: TaskStatus.Completed,
      dueDate: '2026-11-01T10:00:00Z',
      userId: 'user-id',
    },
    {
      id: 'pending-far',
      title: 'Pending far',
      description: 'Pending task with a later date.',
      status: TaskStatus.Pending,
      dueDate: '2026-12-10T10:00:00Z',
      userId: 'user-id',
    },
    {
      id: 'in-progress-near',
      title: 'In progress near',
      description: 'In-progress task.',
      status: TaskStatus.InProgress,
      dueDate: '2026-11-20T10:00:00Z',
      userId: 'user-id',
    },
    {
      id: 'pending-near',
      title: 'Pending near',
      description: 'Pending task with a nearby date.',
      status: TaskStatus.Pending,
      dueDate: '2026-11-15T10:00:00Z',
      userId: 'user-id',
    },
    {
      id: 'completed-later',
      title: 'Completed later',
      description: 'Completed task with a later date.',
      status: TaskStatus.Completed,
      dueDate: '2026-12-20T10:00:00Z',
      userId: 'user-id',
    },
  ];

  const taskServiceMock = {
    getAll: vi.fn(),
  };

  const notificationServiceMock = {
    showError: vi.fn(),
  };

  beforeEach(async () => {
    taskServiceMock.getAll.mockReset();
    notificationServiceMock.showError.mockReset();

    taskServiceMock.getAll.mockReturnValue(of(tasks));

    await TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [
        {
          provide: TaskService,
          useValue: taskServiceMock,
        },
        {
          provide: NotificationService,
          useValue: notificationServiceMock,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Dashboard);
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

  it('should order pending tasks before completed tasks', () => {
    const orderedIds = component
      .orderedTasks()
      .map((task) => task.id);

    expect(orderedIds).toEqual([
      'pending-near',
      'in-progress-near',
      'pending-far',
      'completed-earlier',
      'completed-later',
    ]);
  });

  it('should keep the original tasks unchanged', () => {
    expect(component.tasks()).toEqual(tasks);
  });

  it('should calculate the completed task count', () => {
    expect(component.completedCount).toBe(2);
  });

  it('should calculate the pending task count', () => {
    expect(component.pendingCount).toBe(3);
  });

  it('should identify completed tasks', () => {
    const completedTask = tasks[0];
    const pendingTask = tasks[1];

    expect(component.isCompleted(completedTask)).toBe(true);
    expect(component.isCompleted(pendingTask)).toBe(false);
  });

  it('should return the correct label for each status', () => {
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

  it('should show an error when tasks cannot be loaded', () => {
    taskServiceMock.getAll.mockReturnValue(
      throwError(() => new Error('Request failed')),
    );

    component.loadTasks();

    expect(component.errorMessage()).toBe(
      'We could not load your tasks. Please try again.',
    );

    expect(component.isLoading()).toBe(false);

    expect(
      notificationServiceMock.showError,
    ).toHaveBeenCalledWith(
      'Unable to load tasks.',
      'Dashboard error',
    );
  });
});