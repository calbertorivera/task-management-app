import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, timeout } from 'rxjs';
import { apiConfig } from '../config/api.config';

export interface DatabaseHealthResponse {
  status: string;
  durationMilliseconds: number;
}

@Injectable({
  providedIn: 'root',
})
export class DatabaseHealthService {
  private readonly http = inject(HttpClient);

  check(): Observable<DatabaseHealthResponse> {
    return this.http
      .get<DatabaseHealthResponse>(
        `${apiConfig.baseUrl}/api/health/database`,
      )
      .pipe(timeout({ each: 180_000 }));
  }
}