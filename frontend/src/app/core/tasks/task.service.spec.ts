import {
  HttpClient,
  provideHttpClient,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
} from 'vitest';

import { apiConfig } from '../config/api.config';
import {
  CreateTaskRequest,
  Task,
  TaskStatus,
  UpdateTaskRequest,
} from './task.model';
import { TaskService } from './task.service';

describe('TaskService', () => {
  let service: TaskService;
  let httpMock: HttpTestingController;

  const endpoint = `${apiConfig.baseUrl}/api/Tasks`;

  const task: Task = {
    id: 'task-id',
    title: 'Prepare presentation',
    description: 'Complete the technical exercise presentation.',
    status: TaskStatus.Pending,
    dueDate: '2026-12-01T10:00:00Z',
    userId: 'user-id',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        TaskService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(TaskService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAll should request all tasks', () => {
    const tasks: ReadonlyArray<Task> = [task];

    service.getAll().subscribe((result) => {
      expect(result).toEqual(tasks);
    });

    const request = httpMock.expectOne(endpoint);

    expect(request.request.method).toBe('GET');

    request.flush(tasks);
  });

  it('getById should request a task by its id', () => {
    service.getById(task.id).subscribe((result) => {
      expect(result).toEqual(task);
    });

    const request = httpMock.expectOne(
      `${endpoint}/${task.id}`,
    );

    expect(request.request.method).toBe('GET');

    request.flush(task);
  });

  it('create should send a POST request with the task data', () => {
    const taskRequest: CreateTaskRequest = {
      title: task.title,
      description: task.description,
      dueDate: task.dueDate,
    };

    service.create(taskRequest).subscribe((result) => {
      expect(result).toEqual(task);
    });

    const request = httpMock.expectOne(endpoint);

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(taskRequest);

    request.flush(task);
  });

  it('update should send a PUT request with the task data', () => {
    const taskRequest: UpdateTaskRequest = {
      title: 'Updated presentation',
      description: 'Updated task description.',
      dueDate: '2026-12-02T10:00:00Z',
      status: TaskStatus.InProgress,
    };

    const updatedTask: Task = {
      ...task,
      ...taskRequest,
    };

    service
      .update(task.id, taskRequest)
      .subscribe((result) => {
        expect(result).toEqual(updatedTask);
      });

    const request = httpMock.expectOne(
      `${endpoint}/${task.id}`,
    );

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(taskRequest);

    request.flush(updatedTask);
  });

  it('delete should send a DELETE request with the task id', () => {
    let completed = false;

    service.delete(task.id).subscribe(() => {
      completed = true;
    });

    const request = httpMock.expectOne(
      `${endpoint}/${task.id}`,
    );

    expect(request.request.method).toBe('DELETE');

    request.flush(null);

    expect(completed).toBe(true);
  });
});