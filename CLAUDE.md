# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Easy School Management System - A two-project .NET 8.0 solution with ASP.NET Core API backend and MVC web frontend.

## Build & Run Commands

```bash
# Build entire solution
dotnet build easy-school-management-system.sln

# Run the API (from solution root)
dotnet run --project School.API

# Run the Web frontend (from solution root)
dotnet run --project School.Web

# Add NuGet packages
dotnet add School.API package <package-name>
dotnet add School.Web package <package-name>

# EF Core migrations (from School.API directory)
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## Architecture

**Solution Structure:**
- `School.API/` - RESTful API backend with JWT authentication
- `School.Web/` - ASP.NET Core MVC frontend that consumes the API

**Layered Architecture (School.API):**
```
Controllers → Services → Repositories → DbContext (EF Core)
```

**Domain Organization:**
Code is organized by business domains within `Data/DBModels/`:
- `Academic/` - Classes, Sections, Subjects, Sessions
- `HR/` - Employee management
- `Accounts/` - Financial entities
- `UserManagement/` - Users, Roles, Permissions, Menus

## Key Patterns

- **Repository Pattern**: All data access through `Repositories/Interfaces/` and `Repositories/Implementations/`
- **DTOs**: API contracts in `DTOs/` folder, separate from database models
- **Dependency Injection**: Services and repositories registered in `Program.cs` with `AddScoped`
- **RBAC**: Role-based access via `RolePermission` entity with granular controls (View, Insert, Update, Delete, BackDate, Print)

## Database

- SQL Server with EF Core 8.0 (Code-First)
- Connection string in `School.API/appsettings.json` (DefaultConnection)
- Database initialization script: `School.API/Scripts/CreateTables.sql`
- Database name: `EasySchoolDB`

## Authentication

- JWT Bearer tokens (12-hour expiration)
- JWT secret key in `appsettings.json` under `Jwt:Key`
- Auth endpoints: `AuthController` in API, login via `AuthService`
- Current mock credentials: `admin@school.com / admin123` (to be replaced with DB authentication)

## Adding New Features

When adding a new entity/feature:
1. Create model in `School.API/Data/DBModels/<Domain>/`
2. Add DbSet to `SchoolDbContext.cs`
3. Create DTO in `School.API/DTOs/`
4. Create repository interface and implementation in `Repositories/`
5. Create service interface and implementation in `Services/`
6. Register service and repository in `Program.cs`
7. Create API controller in `Controllers/`
8. For web UI: add controller, views, and models in `School.Web/`

## Configuration

- API runs with CORS "AllowAll" policy for development
- JSON serialization ignores reference cycles (handles circular relationships)
- HTTPS redirection disabled for local development
