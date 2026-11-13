# Task Management App

Task management application built with .NET 10, ASP.NET Core Web API, Entity Framework Core, SQL Server and Angular.

## User story

As an authenticated user, I want to create, view, update and delete my own tasks so that I can organize and track my daily activities securely.

## Architecture

The backend follows Clean Architecture:

- `TaskManagement.Domain`: entities and domain rules.
- `TaskManagement.Application`: contracts, interfaces and business services.
- `TaskManagement.Infrastructure`: EF Core, database context, repositories and security implementations.
- `TaskManagement.Api`: controllers, authentication and HTTP configuration.

## Main features

- User registration.
- User login with JWT.
- Authenticated task CRUD.
- Tasks isolated by authenticated user.
- SQL Server persistence.
- Entity Framework Core migrations.
- Seed user and sample tasks.
- Unit tests for application, API and infrastructure layers.
- Swagger/OpenAPI documentation.

## Requirements

- .NET 10 SDK
- SQL Server or Azure SQL
- Visual Studio or VS Code
- Angular CLI, when running the frontend

## Backend setup

From the `backend` directory:

```bash
dotnet restore
dotnet build
dotnet ef database update \
  --project TaskManagement.Infrastructure \
  --startup-project TaskManagement.Api