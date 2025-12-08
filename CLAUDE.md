# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run Commands

```bash
# Build entire solution
dotnet build easy-school-management-system.sln

# Run API project (http://localhost:5266, https://localhost:7191)
dotnet run --project School.API

# Run Web project (http://localhost:5267, https://localhost:7262)
dotnet run --project School.Web

# Run specific project with HTTPS profile
dotnet run --project School.API --launch-profile https
dotnet run --project School.Web --launch-profile https

# Restore dependencies
dotnet restore
```

## Architecture

This is a .NET 9.0 solution with two projects:

- **School.API**: REST API backend using ASP.NET Core Web API with OpenAPI/Swagger support
- **School.Web**: MVC frontend using ASP.NET Core MVC with Razor views

Both projects use:
- Nullable reference types enabled
- Implicit usings enabled
- Controller-based routing

### Project Structure

**School.API**: API controllers return JSON data, OpenAPI enabled in development mode
**School.Web**: MVC pattern with Controllers, Views (Razor), and Models. Uses conventional routing `{controller=Home}/{action=Index}/{id?}`
