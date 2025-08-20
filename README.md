# Online Shopping System

A comprehensive e-commerce backend solution built with .NET 8 and PostgreSQL following Clean Architecture principles.

## Project Structure

```
OnlineShoppingSystem/
├── src/
│   ├── OnlineShoppingSystem.API/          # Web API layer (Controllers, Middleware)
│   ├── OnlineShoppingSystem.Application/  # Business logic layer (Services, DTOs)
│   ├── OnlineShoppingSystem.Domain/       # Core domain layer (Entities, Value Objects)
│   └── OnlineShoppingSystem.Infrastructure/ # Data access layer (Repositories, EF Context)
├── tests/
│   ├── OnlineShoppingSystem.Tests.Unit/        # Unit tests
│   └── OnlineShoppingSystem.Tests.Integration/ # Integration tests
└── OnlineShoppingSystem.sln               # Solution file
```

## Technology Stack

- **.NET 8** - Web API framework
- **PostgreSQL** - Database
- **Entity Framework Core** - ORM
- **JWT Bearer Authentication** - Security
- **Serilog** - Structured logging
- **AutoMapper** - Object mapping
- **BCrypt** - Password hashing
- **MSTest** - Testing framework
- **Moq** - Mocking framework

## Getting Started

1. Ensure you have .NET 8 SDK installed
2. Set up PostgreSQL database
3. Update connection strings in `appsettings.json`
4. Run `dotnet restore` to restore packages
5. Run `dotnet build` to build the solution
6. Run `dotnet test` to execute tests
7. Run `dotnet run --project src/OnlineShoppingSystem.API` to start the API

## API Documentation

Once running, visit `https://localhost:7152/swagger` for API documentation.

## Health Check

Visit `https://localhost:7152/api/health` to check API health status.