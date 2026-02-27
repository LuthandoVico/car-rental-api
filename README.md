# Car Rental API

ASP.NET Core 8 Web API for managing vehicles, bookings, maintenance, and authentication using JWT.

## Tech Stack
- ASP.NET Core 8
- Entity Framework Core
- PostgreSQL
- JWT Authentication
- Role-based Authorization

## Features
- User Registration & Login
- JWT Authentication
- Role-based access (Admin / Customer)
- Vehicle availability search
- Create / Cancel bookings
- Maintenance tracking
- Admin dashboard endpoints

## Run Locally

1. Update connection string in appsettings.Development.json
2. Run migrations:
   dotnet ef database update
3. Run API:
   dotnet run