# RaceDay – Sports Event Management System

## Project Overview

RaceDay is a sports event management application developed using ASP.NET Core 8.0. The system allows organisers to manage sporting events and participants to register for races, manage their profiles and view race results.

The project uses an ASP.NET Core Web API backend and an ASP.NET Core MVC frontend.

## Technologies Used

- ASP.NET Core 8.0 Web API
- ASP.NET Core MVC
- Entity Framework Core
- SQL database configured through Entity Framework Core
- JWT authentication
- Role-based authorisation
- Azure Blob Storage
- HTML, CSS, Bootstrap and Razor
- Docker and Docker Compose
- GitHub Actions

## User Roles

### Organiser

Organisers can create, edit and delete sporting events, manage event categories, view participant enrolments and capture race results.

### Participant

Participants can browse events, enrol in race categories, manage their profiles, upload profile pictures and view their race results.

## Main Features

1. User registration and login
2. Role-based access control
3. Event creation and management
4. Race category management
5. Participant enrolment
6. Race result capture
7. Participant result history
8. Profile management
9. Azure Blob Storage image uploads
10. Responsive web interface

## Solution Structure

- `RaceDay.Api` – Backend API, database access, authentication and business logic
- `RaceDay.Mvc` – MVC website and user interface
- `.github/workflows` – GitHub Actions automation
- `docker-compose.yml` – Container configuration

## Running Locally

1. Install the .NET 8 SDK and Visual Studio 2022.
2. Configure the database connection and JWT settings.
3. Configure Azure Blob Storage using User Secrets.
4. Apply the existing Entity Framework Core migrations.
5. Start both the API and MVC projects.
6. Open the MVC website in your browser.

## Docker

The application includes Dockerfiles for the API and MVC projects and a Docker Compose configuration.

To build and run the containers:

```bash
docker compose build
docker compose up -d
```

The MVC website is configured for `http://localhost:8081`.

Database and Azure configuration must be supplied securely through environment variables.

## CI/CD

GitHub Actions is configured to build both .NET projects and verify their Docker images.

Deployment automation is a separate configuration step.

**Known issue:** Profile-picture persistence after logout and login has not yet been confirmed. A local User Secrets configuration error requires further investigation.

## Security

RaceDay uses JWT authentication and role-based authorisation. Sensitive configuration values should not be committed to source control.
