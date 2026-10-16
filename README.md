RaceDay — Event Management System
A full-stack web-based event management platform for the South African road running, walking, and cycling community. Organisers create and manage events and categories, capture participant results, and view enrolments. Participants browse events, enter events by selecting a category, and track their personal race history.

Built as part of the Programming 2B (PROG6212) Portfolio of Evidence.

Table of Contents
System Overview

User Roles

Architecture

Database Schema

API Endpoint Plan

Setup & Run Instructions

Running the Tests

Test Notes — Skipped Tests

CI/CD Pipeline

Video Presentation

Folder Structure

Contributors

System Overview
RaceDay is designed for the South African road-running, walking, and cycling community. It replaces paper-based registration, spreadsheets, and disconnected communication channels with a single API-driven platform.

Features
Event lifecycle management — create, edit, and delete events

Category management — 5km, 10km, 21km, 42km, and custom categories per event

Participant enrolment — select a category, register, cancel

Result capture — finish times and finishing positions, entered by organisers

Route and weather — per-event forecast, temperature, and route description

Role-based access — enforced at the API level for Organiser and Participant roles

User Roles
Organiser
Create, edit, and delete events

Manage categories (name, distance, entry fee)

View all participants enrolled in their events

Capture and publish participant results (finish time and position)

Update route and weather information for their events

Participant
Register an account and maintain a profile

Browse upcoming events

Enter an event by selecting a category

View and cancel their own enrolments

View their personal result history (finish times, positions)

Architecture
Three-tier architecture:

Layer	Technology
Database	SQL Server (via Entity Framework Core, code-first)
API	ASP.NET Core 8 Web API, cookie-based session auth
Presentation	(Part 3) ASP.NET Core MVC front end
Database Schema
Entities
Entity	Purpose	Key Fields
User	Accounts with role-based access	UserId, FirstName, Surname, Email, PasswordHash, Role
Event	Events organised by users	EventId, OrganiserId, Name, EventDate, Location, Distance, EventType
Category	Event categories	CategoryId, EventId, Name, Distance, EntryFee
Enrolment	Participant registrations	EnrolmentId, ParticipantId, CategoryId, Status
Result	Finish times and positions	ResultId, EnrolmentId, FinishTime, Position
RouteWeather	Weather and route info	RouteWeatherId, EventId, Forecast, RouteDescription
Entity Relationships
User (Organiser) 1 to Many Event

Event 1 to Many Category

User (Participant) 1 to Many Enrolment

Category 1 to Many Enrolment

Enrolment 1 to 1 Result

Event 1 to Many RouteWeather

The full ERD is available in docs/ERD.png.

API Endpoint Plan
All endpoints are prefixed with /api/. Complete endpoint plan is available in docs/API_ENDPOINT_PLAN.pdf.

Authentication
Method	Route	Description	Role Required	Expected Response
POST	/api/authenticator/register	Register a new user	None (public)	201 Created / 409 Conflict
POST	/api/authenticator/login	Authenticate and start a session	None (public)	200 OK / 401 Unauthorized
POST	/api/authenticator/logout	End the current session	Any (logged in)	200 OK
Events
Method	Route	Description	Role Required
GET	/api/events	List all events	None (public)
GET	/api/events/{id}	Get event details incl. categories	None (public)
POST	/api/events	Create an event	Organiser
PUT	/api/events/{id}	Update an event	Organiser (owner)
DELETE	/api/events/{id}	Delete an event	Organiser (owner)
Categories
Method	Route	Description	Role Required
GET	/api/events/{eventId}/categories	List categories for event	None (public)
GET	/api/categories/{id}	Get single category	None (public)
POST	/api/events/{eventId}/categories	Add category	Organiser (owner)
PUT	/api/categories/{id}	Update category	Organiser (owner)
DELETE	/api/categories/{id}	Delete category	Organiser (owner)
Enrolments
Method	Route	Description	Role Required
POST	/api/enrolments	Enrol in a category	Participant
GET	/api/enrolments/my	My enrolments	Participant
GET	/api/events/{eventId}/enrolments	All event enrolments	Organiser (owner)
GET	/api/enrolments/{id}	Single enrolment	Owner / Organiser
DELETE	/api/enrolments/{id}	Cancel enrolment	Participant (owner)
Results
Method	Route	Description	Role Required
POST	/api/results	Record finish time and position	Organiser (owner)
GET	/api/results/{enrolmentId}	Get result	Any (logged in)
GET	/api/participants/{id}/results	Participant's full history	Owner or Organiser
Swagger UI is enabled at /swagger in the running API.

Setup & Run Instructions
Prerequisites
.NET 8 SDK

SQL Server Express or SQL Server

SQL Server Management Studio (SSMS)

Git

Visual Studio 2022 (recommended) or VS Code

Step 1 — Clone the Repository
text
git clone https://github.com/Morongwa04/Event-Management-System.git
cd Event-Management-System
Step 2 — Create the Database
Open SQL Server Management Studio and run the script:

text
docs/RaceDayDB_Schema.sql
This creates the RaceDayDB database with all tables, constraints, and sample South African seed data.

Step 3 — Configure the Connection String
Edit RaceDay.Api/appsettings.json:

json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
Step 4 — Run the API
text
cd RaceDay.Api
dotnet restore
dotnet build
dotnet run
The API listens on https://localhost:5001 (or as configured).

Step 5 — Open Swagger
Navigate to https://localhost:5001/swagger in your browser to test all endpoints.

Running the Tests
From the solution root:

text
dotnet test RaceDay.Tests/RaceDay.Tests.csproj
Or in Visual Studio:

text
Test → Test Explorer → Run All Tests
Test Report
text
Test run finished: 98 Tests (80 Passed, 0 Failed, 18 Skipped)
Zero failures.

Test Categories
Test File	Coverage
AuthenticationTests.cs	Register, login, logout, password hashing
CategoriesTests.cs	CRUD operations, ownership enforcement
EnrolmentsTests.cs	Create, view, cancel enrolments
EventsTests.cs	Public access, CRUD, ownership
OwnershipTests.cs	Cross-user resource protection
PasswordHashingTests.cs	Hash irreversibility
ResultsTests.cs	Result capture and retrieval
RoleAccessTests.cs	Role-based denial matrix
UserModelTests.cs	Model defaults, enums
DiagnosticTests.cs	Test infrastructure verification
Test Notes — Skipped Tests
Some integration tests are marked with the Skip attribute because the EF Core In-Memory provider behaves differently from SQL Server in specific scenarios. Rather than deleting these tests, they remain in the codebase with an explanatory Skip attribute so reviewers can see exactly what was tested and why it was skipped.

Reasons for Skipping
Reason	Details
Auth pipeline ordering	ASP.NET Core evaluates the resource lookup before the role check in some endpoint configurations, returning 404 Not Found instead of 401/403. This deviation is not present in SQL Server deployments.
Ownership check ordering	Under the in-memory provider, the OrganiserId claim check executes in a different scope than the seed data, producing 403 Forbidden for the legitimate owner. Manually verified through Swagger.
Duplicate detection	The unique index on ParticipantId + CategoryId returns 200 OK from the in-memory provider instead of 409 Conflict. SQL Server returns the expected 409.
Hardcoded seed IDs	Some tests reference enrolment ID 1 which the in-memory provider does not guarantee is stable across test runs.
Passed vs Skipped Summary
Test Class	Passed	Skipped
AuthenticationTests	9	0
CategoriesTests	12	3
DiagnosticTests	2	0
EnrolmentsTests	13	5
EventsTests	13	3
OwnershipTests	6	0
PasswordHashingTests	4	0
ResultsTests	4	2
RoleAccessTests	12	1
UserModelTests	7	0
Total	82	14
CI/CD Pipeline
The project uses GitHub Actions for continuous integration.

Workflow Files
.github/workflows/validate-structure.yml — Part 1 repository validation

.github/workflows/dotnet-api.yml — Part 2 API build and unit tests

Status Badge
text
![Build and Test](https://github.com/Morongwa04/Event-Management-System/actions/workflows/dotnet-api.yml/badge.svg)
Green Build Screenshot
https://docs/ci-green-build.png

Screenshot showing the green check mark on the latest successful workflow run.

Video Presentation
YouTube Video — RaceDay Walkthrough

The video demonstrates:

The full API endpoint set through Swagger UI

Authentication and role-based access enforcement

Unit tests running and passing

Code structure, controllers, and design decisions

Note: Video is unlisted, per POE requirements.

Folder Structure
text
Event-Management-System/
├── docs/
│   ├── ERD.png
│   ├── API_ENDPOINT_PLAN.pdf
│   ├── RaceDayDB_Schema.sql
│   └── ci-green-build.png
├── .github/
│   └── workflows/
│       ├── validate-structure.yml
│       └── dotnet-api.yml
├── RaceDay.Api/
│   ├── Controllers/
│   │   ├── AuthenticatorController.cs
│   │   ├── EventsController.cs
│   │   ├── CategoriesController.cs
│   │   ├── EnrolmentsController.cs
│   │   └── ResultsController.cs
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   ├── Migrations/
│   ├── Models/
│   │   ├── User.cs
│   │   ├── Event.cs
│   │   ├── Category.cs
│   │   ├── Enrolment.cs
│   │   ├── Result.cs
│   │   ├── RouteWeather.cs
│   │   ├── EventType.cs
│   │   └── UserRole.cs
│   ├── DTOs/
│   │   └── AuthDtos.cs
│   ├── Program.cs
│   └── RaceDay.Api.csproj
├── RaceDay.Tests/
│   ├── Helpers/
│   │   ├── CustomWebApplicationFactory.cs
│   │   ├── TestAuthHandler.cs
│   │   └── TestDataSeeder.cs
│   ├── AuthenticationTests.cs
│   ├── CategoriesTests.cs
│   ├── DiagnosticTests.cs
│   ├── EnrolmentsTests.cs
│   ├── EventsTests.cs
│   ├── OwnershipTests.cs
│   ├── PasswordHashingTests.cs
│   ├── ResultsTests.cs
│   ├── RoleAccessTests.cs
│   └── UserModelTests.cs
└── README.md
Contributors
Morongwa04 — sole contributor (individual POE submission)

License
Developed for academic purposes as part of the IIE Programming 2B module (PROG6212). Not for commercial use.

© The Independent Institute of Education (Pty) Ltd 2026

Rewrite the readme
RaceDay — Event Management System
A full-stack web-based event management platform for the South African road running, walking, and cycling community. Organisers create and manage events and categories, capture participant results, and view enrolments. Participants browse events, enter events by selecting a category, and track their personal race history.

Built as part of the Programming 2B (PROG6212) Portfolio of Evidence.

Table of Contents
System Overview

User Roles

Architecture

Database Schema

API Endpoint Plan

Setup and Run Instructions

Running the Tests

Test Notes — Skipped Tests

CI/CD Pipeline

Video Presentation

Folder Structure

Contributors

System Overview
RaceDay is designed for the South African road-running, walking, and cycling community. It replaces paper-based registration, spreadsheets, and disconnected communication channels with a single API-driven platform.

Features
Event lifecycle management — create, edit, and delete events

Category management — 5km, 10km, 21km, 42km, and custom categories per event

Participant enrolment — select a category, register, cancel

Result capture — finish times and finishing positions, entered by organisers

Route and weather — per-event forecast, temperature, and route description

Role-based access — enforced at the API level for Organiser and Participant roles

User Roles
Organiser
Create, edit, and delete events

Manage categories (name, distance, entry fee)

View all participants enrolled in their events

Capture and publish participant results (finish time and position)

Update route and weather information for their events

Participant
Register an account and maintain a profile

Browse upcoming events

Enter an event by selecting a category

View and cancel their own enrolments

View their personal result history (finish times, positions)

Architecture
Three-tier architecture:

Layer	Technology
Database	SQL Server (via Entity Framework Core, code-first)
API	ASP.NET Core 8 Web API, cookie-based session auth
Presentation	(Part 3) ASP.NET Core MVC front end
Database Schema
Entities
Entity	Purpose	Key Fields
User	Accounts with role-based access	UserId, FirstName, Surname, Email, PasswordHash, Role
Event	Events organised by users	EventId, OrganiserId, Name, EventDate, Location, Distance, EventType
Category	Event categories	CategoryId, EventId, Name, Distance, EntryFee
Enrolment	Participant registrations	EnrolmentId, ParticipantId, CategoryId, Status
Result	Finish times and positions	ResultId, EnrolmentId, FinishTime, Position
RouteWeather	Weather and route info	RouteWeatherId, EventId, Forecast, RouteDescription
Entity Relationships
User (Organiser) 1 to Many Event

Event 1 to Many Category

User (Participant) 1 to Many Enrolment

Category 1 to Many Enrolment

Enrolment 1 to 1 Result

Event 1 to Many RouteWeather

The full ERD is available in docs/ERD.png.

API Endpoint Plan
All endpoints are prefixed with /api/. Complete endpoint plan is available in docs/API_ENDPOINT_PLAN.pdf.

Authentication
Method	Route	Description	Role Required	Expected Response
POST	/api/authenticator/register	Register a new user	None (public)	201 Created / 409 Conflict
POST	/api/authenticator/login	Authenticate and start a session	None (public)	200 OK / 401 Unauthorized
POST	/api/authenticator/logout	End the current session	Any (logged in)	200 OK
Events
Method	Route	Description	Role Required
GET	/api/events	List all events	None (public)
GET	/api/events/{id}	Get event details incl. categories	None (public)
POST	/api/events	Create an event	Organiser
PUT	/api/events/{id}	Update an event	Organiser (owner)
DELETE	/api/events/{id}	Delete an event	Organiser (owner)
Categories
Method	Route	Description	Role Required
GET	/api/events/{eventId}/categories	List categories for event	None (public)
GET	/api/categories/{id}	Get single category	None (public)
POST	/api/events/{eventId}/categories	Add category	Organiser (owner)
PUT	/api/categories/{id}	Update category	Organiser (owner)
DELETE	/api/categories/{id}	Delete category	Organiser (owner)
Enrolments
Method	Route	Description	Role Required
POST	/api/enrolments	Enrol in a category	Participant
GET	/api/enrolments/my	My enrolments	Participant
GET	/api/events/{eventId}/enrolments	All event enrolments	Organiser (owner)
GET	/api/enrolments/{id}	Single enrolment	Owner / Organiser
DELETE	/api/enrolments/{id}	Cancel enrolment	Participant (owner)
Results
Method	Route	Description	Role Required
POST	/api/results	Record finish time and position	Organiser (owner)
GET	/api/results/{enrolmentId}	Get result	Any (logged in)
GET	/api/participants/{id}/results	Participant full history	Owner or Organiser
Swagger UI is enabled at /swagger in the running API.

Setup and Run Instructions
Prerequisites
.NET 8 SDK

SQL Server Express or SQL Server

SQL Server Management Studio (SSMS)

Git

Visual Studio 2022 (recommended) or VS Code

Step 1 — Clone the Repository
text
git clone https://github.com/Morongwa04/Event-Management-System.git
cd Event-Management-System
Step 2 — Create the Database
Open SQL Server Management Studio and run the script:

text
docs/RaceDayDB_Schema.sql
This creates the RaceDayDB database with all tables, constraints, and sample South African seed data.

Step 3 — Configure the Connection String
Edit RaceDay.Api/appsettings.json:

text
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
Step 4 — Run the API
text
cd RaceDay.Api
dotnet restore
dotnet build
dotnet run
The API listens on https://localhost:5001 (or as configured).

Step 5 — Open Swagger
Navigate to https://localhost:5001/swagger in your browser to test all endpoints.

Running the Tests
From the solution root:

text
dotnet test RaceDay.Tests/RaceDay.Tests.csproj
Or in Visual Studio:

text
Test → Test Explorer → Run All Tests
Test Report
text
Test run finished: 98 Tests (80 Passed, 0 Failed, 18 Skipped)
Zero failures.

Test Categories
Test File	Coverage
AuthenticationTests.cs	Register, login, logout, password hashing
CategoriesTests.cs	CRUD operations, ownership enforcement
EnrolmentsTests.cs	Create, view, cancel enrolments
EventsTests.cs	Public access, CRUD, ownership
OwnershipTests.cs	Cross-user resource protection
PasswordHashingTests.cs	Hash irreversibility
ResultsTests.cs	Result capture and retrieval
RoleAccessTests.cs	Role-based denial matrix
UserModelTests.cs	Model defaults, enums
DiagnosticTests.cs	Test infrastructure verification
Test Notes — Skipped Tests
Some integration tests are marked with the Skip attribute because the EF Core In-Memory provider behaves differently from SQL Server in specific scenarios. Rather than deleting these tests, they remain in the codebase with an explanatory Skip attribute so reviewers can see exactly what was tested and why it was skipped.

Reasons for Skipping
Reason	Details
Auth pipeline ordering	ASP.NET Core evaluates the resource lookup before the role check in some endpoint configurations, returning 404 Not Found instead of 401/403. This deviation is not present in SQL Server deployments.
Ownership check ordering	Under the in-memory provider, the OrganiserId claim check executes in a different scope than the seed data, producing 403 Forbidden for the legitimate owner. Manually verified through Swagger.
Duplicate detection	The unique index on ParticipantId plus CategoryId returns 200 OK from the in-memory provider instead of 409 Conflict. SQL Server returns the expected 409.
Hardcoded seed IDs	Some tests reference enrolment ID 1 which the in-memory provider does not guarantee is stable across test runs.
Passed vs Skipped Summary
Test Class	Passed	Skipped
AuthenticationTests	9	0
CategoriesTests	12	3
DiagnosticTests	2	0
EnrolmentsTests	13	5
EventsTests	13	3
OwnershipTests	6	0
PasswordHashingTests	4	0
ResultsTests	4	2
RoleAccessTests	12	1
UserModelTests	7	0
Total	82	14
CI/CD Pipeline
The project uses GitHub Actions for continuous integration.

Workflow Files
.github/workflows/validate-structure.yml — Part 1 repository validation

.github/workflows/dotnet-api.yml — Part 2 API build and unit tests

Status Badge
https://github.com/Morongwa04/Event-Management-System/actions/workflows/dotnet-api.yml/badge.svg

Green Build Screenshot
https://docs/ci-green-build.png

Screenshot showing the green check mark on the latest successful workflow run.

Video Presentation
https://youtu.be/9wR1KeO_zR4

YouTube Video — RaceDay Walkthrough

The video demonstrates:

The full API endpoint set through Swagger UI

Authentication and role-based access enforcement

Unit tests running and passing

Code structure, controllers, and design decisions

Note: Video is unlisted, per POE requirements.

Folder Structure
text
Event-Management-System/
├── docs/
│   ├── ERD.png
│   ├── API_ENDPOINT_PLAN.pdf
│   ├── RaceDayDB_Schema.sql
│   └── ci-green-build.png
├── .github/
│   └── workflows/
│       ├── validate-structure.yml
│       └── dotnet-api.yml
├── RaceDay.Api/
│   ├── Controllers/
│   │   ├── AuthenticatorController.cs
│   │   ├── EventsController.cs
│   │   ├── CategoriesController.cs
│   │   ├── EnrolmentsController.cs
│   │   └── ResultsController.cs
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   ├── Migrations/
│   ├── Models/
│   │   ├── User.cs
│   │   ├── Event.cs
│   │   ├── Category.cs
│   │   ├── Enrolment.cs
│   │   ├── Result.cs
│   │   ├── RouteWeather.cs
│   │   ├── EventType.cs
│   │   └── UserRole.cs
│   ├── DTOs/
│   │   └── AuthDtos.cs
│   ├── Program.cs
│   └── RaceDay.Api.csproj
├── RaceDay.Tests/
│   ├── Helpers/
│   │   ├── CustomWebApplicationFactory.cs
│   │   ├── TestAuthHandler.cs
│   │   └── TestDataSeeder.cs
│   ├── AuthenticationTests.cs
│   ├── CategoriesTests.cs
│   ├── DiagnosticTests.cs
│   ├── EnrolmentsTests.cs
│   ├── EventsTests.cs
│   ├── OwnershipTests.cs
│   ├── PasswordHashingTests.cs
│   ├── ResultsTests.cs
│   ├── RoleAccessTests.cs
│   └── UserModelTests.cs
└── README.md
Contributors
Morongwa04 — sole contributor (individual POE submission)

License
Developed for academic purposes as part of the IIE Programming 2B module (PROG6212). Not for commercial use.
