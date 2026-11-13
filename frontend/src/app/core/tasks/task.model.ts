export enum TaskStatus {
  Pending = 1,
  InProgress = 2,
  Completed = 3,
}

export interface Task {
  id: string;
  title: string;
  description: string;
  status: TaskStatus;
  dueDate: string;
  userId: string;
}

export interface CreateTaskRequest {
  title: string;
  description: string;
  dueDate: string;
}

export interface UpdateTaskRequest {
  title: string;
  description: string;
  dueDate: string;
  status: TaskStatus;
}