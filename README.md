# Homevo API

RESTful backend service for the **Homevo** e-commerce platform, built with **ASP.NET Core** and **PostgreSQL** via **Entity Framework Core (`Npgsql`)**.

## Key Features
- Authentication & authorization (JWT & refresh token support, Identity roles)
- Product catalog, categories, search/querying, and low-stock visibility
- Shopping cart, favorites, and customer order management
- Admin dashboard summary endpoints
- Environment-based configuration, CORS policy setup, and automatic database migration on startup

## Tech Stack
- **Framework:** ASP.NET Core Web API
- **ORM & Database:** Entity Framework Core, Npgsql (PostgreSQL)
- **Validation & Mapping:** FluentValidation, AutoMapper

## Configuration & Running Locally
Set `ConnectionStrings__DefaultConnection`, `Jwt__Key`, and `Cors__AllowedOrigins` via environment variables or `appsettings.Development.json`, then run:

    dotnet restore
    dotnet run
