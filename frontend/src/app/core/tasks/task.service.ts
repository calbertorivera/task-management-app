import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { apiConfig } from '../config/api.config';
import {
  CreateTaskRequest,
  Task,
  UpdateTaskRequest,
} from './task.model';

@Injectable({
  providedIn: 'root',
})
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = `${apiConfig.baseUrl}/api/Tasks`;

  getAll(): Observable<ReadonlyArray<Task>> {
    return this.http.get<ReadonlyArray<Task>>(this.endpoint);
  }

  getById(taskId: string): Observable<Task> {
    return this.http.get<Task>(`${this.endpoint}/${taskId}`);
  }

  create(request: CreateTaskRequest): Observable<Task> {
    return this.http.post<Task>(this.endpoint, request);
  }

  update(
    taskId: string,
    request: UpdateTaskRequest,
  ): Observable<Task> {
    return this.http.put<Task>(
      `${this.endpoint}/${taskId}`,
      request,
    );
  }

  delete(taskId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.endpoint}/${taskId}`,
    );
  }
}