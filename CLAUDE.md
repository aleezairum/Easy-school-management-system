# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Easy School Management System - A two-project .NET 8.0 solution: ASP.NET Core API backend + MVC web frontend.

## Build & Run Commands

```bash
# Build entire solution
dotnet build easy-school-management-system.sln

# Run the API (from solution root) - serves on http://localhost:5266
dotnet run --project School.API

# Run the Web frontend (from solution root) - path base is /EasySchool
dotnet run --project School.Web

# Add NuGet packages
dotnet add School.API package <package-name>
dotnet add School.Web package <package-name>

# EF Core migrations (from School.API directory)
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## Architecture

**Two-Project Solution:**
- `School.API/` - RESTful API backend (JWT auth, Swagger at `/swagger`)
- `School.Web/` - ASP.NET Core MVC frontend that calls the API via `HttpClient`

**API Layered Architecture:**
```
Controllers → Services → Repositories → SchoolDbContext (EF Core → SQL Server)
```

Each layer has paired `Interfaces/` and `Implementations/` folders. All are registered as `AddScoped` in `Program.cs`.

**Domain Organization (within each layer):**
- `Academic/` - Classes, Sections, Subjects, Sessions, Students, Exams, Timetables, Attendance
- `HR/` - Employees, Designations, Grades, Salaries
- `Accounts/` (models) / `FeeManagement/` (services/repos/controllers) - Fee Types, Fee Structures, Fee Vouchers, Apply Fee, Payment Methods
- `UserManagement/` - Users, Roles, Permissions, Menus
- `Student/` - Student-specific attendance
- `SMS/` (models) - SMS/WhatsApp messaging

Note: Domain folder naming is inconsistent between layers (e.g., `Accounts/` in models but `FeeManagement/` in controllers/services/repos). Follow existing patterns per layer.

**Web Frontend:**
- Views use jQuery + Bootstrap for UI, with AJAX calls to the API
- `wwwroot/js/common.js` defines `API_BASE` URL constant (toggle between local and live)
- Path base is `/EasySchool` (configured in `School.Web/Program.cs`)
- Each feature has its own controller + view folder (e.g., `Controllers/FeeVoucherController.cs` + `Views/FeeVoucher/`)

## Key Patterns

- **Repository Pattern**: All data access through repository interfaces/implementations; many repos use raw SQL via `Database.SqlQueryRaw<T>()` or stored procedures alongside EF Core
- **DTOs**: API contracts in `DTOs/` folder (organized by domain subfolder), separate from `Data/DBModels/`
- **DI Registration**: Every new service/repository pair must be registered in `Program.cs` with `AddScoped`
- **RBAC**: `RolePermission` entity with granular controls (View, Insert, Update, Delete, BackDate, Print)
- **SMS/WhatsApp**: Swappable sender implementations (`WahaSmsSender` for WhatsApp via WAHA API, `LogOnlySmsSender` for dev). Background jobs (`AbsentNotificationJob`, `FeeReminderJob`) extend `SmsJobBase` and are configured in `appsettings.json` under `SmsJobs`

## Database

- SQL Server with EF Core 8.0 (Code-First)
- Connection string: `School.API/appsettings.json` → `ConnectionStrings:DefaultConnection`
- Database: `EasySchoolDB`
- Seed/init scripts in `School.API/Scripts/` (CreateTables.sql, Campus.sql, FeeType.sql, etc.)

## Authentication

- JWT Bearer tokens (12-hour expiration, no issuer/audience validation)
- JWT key in `appsettings.json` under `Jwt:Key`
- Auth flow: `AuthController` → `AuthService`

## Adding New Features

When adding a new entity/feature:
1. Create model in `School.API/Data/DBModels/<Domain>/`
2. Add DbSet to `SchoolDbContext.cs`
3. Create DTO in `School.API/DTOs/<Domain>/`
4. Create repository interface + implementation in `Repositories/Interfaces/<Domain>/` and `Repositories/Implementations/<Domain>/`
5. Create service interface + implementation in `Services/Interfaces/<Domain>/` and `Services/Implementations/<Domain>/`
6. Register both service and repository in `Program.cs` with `AddScoped`
7. Create API controller in `Controllers/<Domain>/`
8. For web UI: add MVC controller in `School.Web/Controllers/`, views in `School.Web/Views/<Feature>/`

## Configuration

- API CORS: "AllowAll" policy (any origin/header/method)
- JSON serialization: `ReferenceHandler.IgnoreCycles` for circular relationships
- Swagger enabled unconditionally (not dev-only gated)
- WAHA WhatsApp integration configured in `appsettings.json` under `Waha`
