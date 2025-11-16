## 10xCards

## Project description
10xCards helps learners create and manage study flashcards much faster. The app uses LLMs (via API) to suggest flashcards from a pasted text, allowing users to accept, edit, or reject proposals. It also supports manual flashcard creation and integrates with a spaced repetition algorithm to power study sessions. The product emphasizes privacy (per GDPR) and gives users control over their data.

### Table of contents
- [Project name](#project-name)
- [Project description](#project-description)
- [Tech stack](#tech-stack)
- [Getting started locally](#getting-started-locally)
- [Available scripts](#available-scripts)
- [Testing](#testing)
  - [Unit Tests](#unit-tests)
  - [E2E Tests](#e2e-tests)
- [Project scope](#project-scope)
- [Project status](#project-status)
- [License](#license)

## Tech stack
- **Frontend**: Blazor SSR (server‑side rendering) with interactive server components, Bootstrap 5
- **Backend**: ASP.NET Core (target framework: **net9.0**)
- **Auth**: ASP.NET Core Identity (planned)
- **Data**: Entity Framework Core with PostgreSQL (planned)
- **AI**: OpenAi API for access to multiple LLM providers (planned)
- **CI/CD**: GitHub Actions (planned)
- **Hosting**: Docker image deployment (e.g., DigitalOcean) (planned)

## Getting started locally

### Prerequisites
- .NET SDK 9.0+
- Docker Desktop (optional, for local PostgreSQL + pgAdmin via Docker Compose)
- OpenAi API key (optional for future AI integration): set as `OPENAI_API_KEY`

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

## Testing

The project includes comprehensive test coverage with both unit tests and end-to-end (E2E) tests.

### Unit Tests

Unit tests are located in the `10xCards.Tests` project and test individual components, services, and utilities in isolation.

**Running Unit Tests:**
```bash
# Run all unit tests
dotnet test 10xCards.Tests/10xCards.Tests.csproj

# Run with detailed output
dotnet test 10xCards.Tests/10xCards.Tests.csproj --verbosity normal

# Run with coverage
dotnet test 10xCards.Tests/10xCards.Tests.csproj --collect:"XPlat Code Coverage"
```

**Unit Test Structure:**
- `Services/` - Tests for business logic services
- `Validators/` - Tests for request validation
- `Utilities/` - Tests for helper utilities
- `Fixtures/` - Shared test fixtures (in-memory database)

### E2E Tests

End-to-end tests are located in the `10xCards.E2ETests` project and test complete user flows through the browser using Playwright.

**Architecture:**
- **Browser Automation**: Playwright for .NET (Chromium)
- **Database**: Testcontainers PostgreSQL (shared container across all tests)
- **Application Host**: WebApplicationFactory for in-process testing
- **Test Isolation**: Database transactions rolled back after each test

**Prerequisites:**
- Docker Desktop must be running (for PostgreSQL Testcontainers)
- Playwright browsers will be installed automatically on first build

**Running E2E Tests:**
```bash
# Build the test project (installs Playwright browsers)
dotnet build 10xCards.E2ETests/10xCards.E2ETests.csproj

# Run all E2E tests
dotnet test 10xCards.E2ETests/10xCards.E2ETests.csproj

# Run with detailed output
dotnet test 10xCards.E2ETests/10xCards.E2ETests.csproj --verbosity normal

# Run specific test class
dotnet test 10xCards.E2ETests/10xCards.E2ETests.csproj --filter "FullyQualifiedName~AuthenticationE2ETests"

# Run specific test
dotnet test 10xCards.E2ETests/10xCards.E2ETests.csproj --filter "Name=Login_WithValidCredentials_ShouldSucceed"
```

**Manual Playwright Browser Installation** (if needed):
```powershell
# PowerShell
pwsh 10xCards.E2ETests/bin/Debug/net9.0/playwright.ps1 install chromium
```

**E2E Test Features:**
- ✅ Real browser testing (not just HTTP calls)
- ✅ Single PostgreSQL container shared across all tests (fast startup)
- ✅ Test isolation via database transactions
- ✅ Authentication flow testing (register, login, logout)
- ✅ Flashcard CRUD operations testing
- ✅ API and UI interaction testing
- ✅ Screenshot capture on test failure

**E2E Test Structure:**
- `Tests/AuthenticationE2ETests.cs` - User registration, login, logout flows
- `Tests/FlashcardE2ETests.cs` - Flashcard CRUD operations
- `Fixtures/` - Shared test infrastructure (database, browser, app factory)
- `Helpers/` - Test utilities and page helpers
- `Base/` - Base test class with common functionality

**Debugging E2E Tests:**

To run tests in headed mode (see the browser):
1. Edit `10xCards.E2ETests/Fixtures/PlaywrightFixture.cs`
2. Change `Headless = true` to `Headless = false`
3. Run the tests

**Performance:**
- First test run: ~10-15 seconds (container startup + migrations)
- Subsequent tests: ~1-2 seconds per test (shared container)
- All tests reuse the same PostgreSQL container for optimal speed

**CI/CD Integration:**

E2E tests are CI/CD ready and work in GitHub Actions with Docker support:
```yaml
- name: Run E2E Tests
  run: dotnet test 10xCards.E2ETests/10xCards.E2ETests.csproj
```

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
- Connect OpenAi and build the AI generation flow and review UI
- Integrate spaced repetition session using a ready algorithm/library
- Add acceptance metrics and basic analytics
- CI/CD with GitHub Actions; containerized deploy

## License
TBD. If you intend to use this project publicly, please add a `LICENSE` file (e.g., MIT) and update this section accordingly.


