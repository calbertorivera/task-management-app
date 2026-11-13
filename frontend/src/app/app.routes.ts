import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login';
import { Dashboard } from './features/dashboard/dashboard';
import { authGuard } from './core/auth/auth-guard';
import { AppShell } from './shared/layout/app-shell/app-shell';
import { TaskList } from './features/tasks/task-list/task-list';
import { TaskForm } from './features/tasks/task-form/task-form';


export const routes: Routes = [
    {
        path: 'login',
        component: LoginComponent,
    },
    {
        path: '',
        component: AppShell,
        canActivate: [authGuard],
        children: [
            {
                path: '',
                pathMatch: 'full',
                redirectTo: 'dashboard',
            },
            {
                path: 'dashboard',
                component: Dashboard,
            },
            {
                path: 'tasks/new',
                component: TaskForm,
            },
            {
                path: 'tasks/:id/edit',
                component: TaskForm,
            },
            {
                path: 'tasks',
                pathMatch: 'full',
                component: TaskList,
            },
        ],
    },
    {
        path: '**',
        redirectTo: '',
    },
];