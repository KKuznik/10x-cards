## 10xCards

## Project description
10xCards helps learners create and manage study flashcards much faster. The app uses LLMs (via API) to suggest flashcards from a pasted text, allowing users to accept, edit, or reject proposals. It also supports manual flashcard creation and integrates with a spaced repetition algorithm to power study sessions. The product emphasizes privacy (per GDPR) and gives users control over their data.

### Table of contents
- [Project name](#project-name)
- [Project description](#project-description)
- [Tech stack](#tech-stack)
- [Getting started locally](#getting-started-locally)
- [Available scripts](#available-scripts)
- [Project scope](#project-scope)
- [Project status](#project-status)
- [License](#license)

## Tech stack
- **Frontend**: Blazor SSR (server‑side rendering) with interactive server components, Bootstrap 5
- **Backend**: ASP.NET Core (target framework: **net9.0**)
- **Auth**: ASP.NET Core Identity (planned)
- **Data**: Entity Framework Core with PostgreSQL (planned)
- **AI**: OpenRouter.ai API for access to multiple LLM providers (planned)
- **CI/CD**: GitHub Actions (planned)
- **Hosting**: Docker image deployment (e.g., DigitalOcean) (planned)

## Getting started locally

### Prerequisites
- .NET SDK 9.0+
- Docker Desktop (optional, for local PostgreSQL + pgAdmin via Docker Compose)
- OpenRouter API key (optional for future AI integration): set as `OPENROUTER_API_KEY`

### Run the app (no Docker)
```bash
dotnet restore
dotnet run --project 10xCards/10xCards.csproj
```
The app will start using the launch profile URLs, typically:
- HTTP: `http://localhost:5150`
- HTTPS: `https://localhost:7084`

### Start local database services (Docker Compose)
This repository includes PostgreSQL and pgAdmin definitions.

Start only the database and pgAdmin services:
```bash
docker compose up -d 10xcards.database.postgres 10xcards.pgadmin
```

Services:
- PostgreSQL: `localhost:5432` (user: `postgres`, password: `postgres`, db: `10xCards`)
- pgAdmin: `http://localhost:8888` (email: `pgadmin@gmail.com`, password: `1234Qwer`)

Recommended connection strings (for later EF Core integration):
- Host app running on your machine:
  - `Host=localhost;Port=5432;Database=10xCards;Username=postgres;Password=postgres`
- Host app running inside Docker network (if you containerize the web app later):
  - `Host=10xcards.database.postgres;Port=5432;Database=10xCards;Username=postgres;Password=postgres`


### Configuration
Current configuration files:
- `10xCards/appsettings.json` and `10xCards/appsettings.Development.json` (logging defaults shown)
- `10xCards/Properties/launchSettings.json` (local URLs and Docker container ports)

## Available scripts
- **Restore**: `dotnet restore`
- **Build**: `dotnet build`
- **Run**: `dotnet run --project 10xCards/10xCards.csproj`
- **Watch (dev)**: `dotnet watch --project 10xCards/10xCards.csproj`
- **Docker Compose (DB + pgAdmin)**: `docker compose up -d 10xcards.database.postgres 10xcards.pgadmin`
- **Docker Compose stop**: `docker compose down`
- **Docker logs (service)**: `docker compose logs -f <service>`

After EF Core is added:
- **Migrations**: `dotnet ef migrations add <Name>`
- **Update DB**: `dotnet ef database update`

## Project scope

In scope (MVP):
- AI‑assisted flashcard generation from user‑pasted text (≈1,000–10,000 chars)
- Review flow to accept, edit, or reject proposed flashcards
- Manual flashcard CRUD and “My flashcards” list
- Basic auth (sign up, sign in) and account deletion
- Spaced repetition study session using a ready algorithm/library
- Storage for users and flashcards with scalability in mind
- Metrics on AI‑generated vs. accepted flashcards
- GDPR compliance: data access and deletion on request

Out of scope (MVP):
- Custom/advanced repetition algorithm (will use a proven OSS library)
- Gamification mechanics
- Native mobile apps (web only)
- Import of complex document formats (PDF, DOCX, etc.)
- Public API and sharing flashcards between users
- Advanced notifications
- Advanced keyword search

## Project status
Early MVP scaffolding. The Blazor SSR application template is in place. Database, Identity, AI integration, and spaced repetition workflow are planned and will be implemented iteratively.

Planned milestones (high level):
- Integrate ASP.NET Core Identity (registration, login, account deletion)
- Add EF Core + PostgreSQL (schema, migrations, repositories)
- Implement manual flashcard CRUD
- Connect OpenRouter.ai and build the AI generation flow and review UI
- Integrate spaced repetition session using a ready algorithm/library
- Add acceptance metrics and basic analytics
- CI/CD with GitHub Actions; containerized deploy

## License
TBD. If you intend to use this project publicly, please add a `LICENSE` file (e.g., MIT) and update this section accordingly.


