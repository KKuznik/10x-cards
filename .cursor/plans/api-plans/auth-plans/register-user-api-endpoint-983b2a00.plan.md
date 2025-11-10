<!-- 983b2a00-20d1-455d-8da4-d8344296eb00 93568574-25cc-4cf6-9321-8906b4265ea9 -->
# API Endpoint Implementation Plan: User Registration

## 1. Przegląd punktu końcowego

Endpoint umożliwia rejestrację nowego użytkownika w systemie. Po pomyślnej rejestracji zwraca token JWT, który może być użyty do uwierzytelnienia w innych endpointach. Wykorzystuje ASP.NET Core Identity do zarządzania użytkownikami i hashowania haseł.

**Kluczowe funkcje:**

- Rejestracja nowego użytkownika z walidacją emaila i hasła
- Automatyczne hashowanie hasła przez ASP.NET Core Identity
- Generowanie tokena JWT po pomyślnej rejestracji
- Spójne zwracanie błędów zgodnie z RFC 7807 (ProblemDetails)

## 2. Szczegóły żądania

- **Metoda HTTP**: POST
- **Struktura URL**: `/api/auth/register`
- **Content-Type**: application/json
- **Autentykacja**: Brak (publiczny endpoint)

### Parametry:

**Request Body (wszystkie wymagane):**

```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!"
}
```

**Walidacja:**

- `email`: Format emaila, maksymalnie 255 znaków, unikalny w systemie
- `password`: Minimum 8 znaków, wymaga dużej litery, małej litery, cyfry i znaku specjalnego
- `confirmPassword`: Musi być identyczne z `password`

## 3. Wykorzystywane typy

### Istniejące typy (Models/Requests):

- **RegisterRequest**: `10xCards/Models/Requests/RegisterRequest.cs`
  - Email (string, required, email format, max 255 chars)
  - Password (string, required, min 8 chars, regex validation)
  - ConfirmPassword (string, required, must match Password)

### Istniejące typy (Models/Responses):

- **AuthResponse**: `10xCards/Models/Responses/AuthResponse.cs`
  - UserId (Guid)
  - Email (string)
  - Token (string - JWT)
  - ExpiresAt (DateTime)

- **ErrorResponse**: `10xCards/Models/Responses/ErrorResponse.cs`
  - Message (string)
  - ErrorCode (string, nullable)
  - Errors (Dictionary<string, List<string>>, nullable)

### Nowe typy do stworzenia:

**RegisterRequestValidator** (nowy plik: `10xCards/Validators/RegisterRequestValidator.cs`):

```csharp
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    // FluentValidation rules matching API specification
}
```

**IAuthService** (nowy plik: `10xCards/Services/IAuthService.cs`):

```csharp
public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterUserAsync(RegisterRequest request, CancellationToken cancellationToken);
}
```

**AuthService** (nowy plik: `10xCards/Services/AuthService.cs`):

```csharp
public class AuthService : IAuthService
{
    // Implementation with UserManager<User> and JWT generation
}
```

**Result<T>** (nowy plik: `10xCards/Models/Common/Result.cs`):

```csharp
public class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, List<string>>? Errors { get; init; }
}
```

## 4. Szczegóły odpowiedzi

### Sukces (201 Created):

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-11-04T12:00:00Z"
}
```

### Błąd walidacji (400 Bad Request):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "email": ["Email is already registered"],
    "password": ["Password must be at least 8 characters"]
  }
}
```

### Błąd serwera (500 Internal Server Error):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500
}
```

## 5. Przepływ danych

```
1. HTTP Request → Minimal API Endpoint
   ↓
2. Model Binding & Data Annotations Validation (RegisterRequest)
   ↓
3. FluentValidation (RegisterRequestValidator) - optional enhanced validation
   ↓
4. AuthService.RegisterUserAsync()
   ├→ 4a. Sprawdzenie unikalności emaila (UserManager.FindByEmailAsync)
   ├→ 4b. Utworzenie użytkownika (UserManager.CreateAsync)
   │   └→ Automatyczne hashowanie hasła przez Identity
   ├→ 4c. Zapis do bazy danych (EF Core + PostgreSQL)
   ├→ 4d. Generowanie JWT tokena (JwtSecurityTokenHandler)
   └→ 4e. Konstrukcja AuthResponse
   ↓
5. Return 201 Created z AuthResponse
```

**Interakcje z zewnętrznymi systemami:**

- **PostgreSQL**: Zapis użytkownika przez Entity Framework Core
- **ASP.NET Core Identity**: Zarządzanie użytkownikami i hashowanie haseł
- **JWT**: Generowanie tokena uwierzytelniającego

## 6. Względy bezpieczeństwa

### Uwierzytelnienie:

- Endpoint jest publiczny - nie wymaga uwierzytelnienia
- Rate limiting powinien być rozważony (np. max 5 rejestracji na IP na godzinę)

### Autoryzacja:

- Brak - każdy może się zarejestrować

### Bezpieczeństwo haseł:

- Hashowanie przez ASP.NET Core Identity (PBKDF2 z iteracjami)
- Walidacja siły hasła (min 8 znaków, wielkie/małe litery, cyfry, znaki specjalne)
- ConfirmPassword zapobiega błędom przy wpisywaniu hasła

### JWT Token:

- Podpisany tokenem symetrycznym lub asymetrycznym
- Zawiera claims: userId, email
- Czas wygaśnięcia: zalecane 24 godziny dla web app, 7 dni dla mobile
- Przechowywany po stronie klienta (localStorage/sessionStorage)

### Walidacja danych:

- Email format validation (DataAnnotations + FluentValidation)
- SQL Injection prevention (EF Core parametryzowane zapytania)
- XSS prevention (nie zapisujemy HTML)

### HTTPS:

- Endpoint MUSI być dostępny tylko przez HTTPS (UseHttpsRedirection)

### Poufność danych:

- Hasła nigdy nie są logowane
- Hasła nigdy nie są zwracane w response
- Correlation ID dla śledzenia requestów w logach

## 7. Obsługa błędów

### Błędy walidacji (400 Bad Request):

| Scenariusz | Pole | Komunikat |

|------------|------|-----------|

| Email pusty | email | "Email is required" |

| Nieprawidłowy format email | email | "Invalid email format" |

| Email już istnieje | email | "Email is already registered" |

| Email za długi | email | "Email must not exceed 255 characters" |

| Hasło puste | password | "Password is required" |

| Hasło za krótkie | password | "Password must be at least 8 characters" |

| Hasło za słabe | password | "Password must contain uppercase, lowercase, number, and special character" |

| Hasła się nie zgadzają | confirmPassword | "Password and confirmation password must match" |

### Błędy serwera (500 Internal Server Error):

- Błąd połączenia z bazą danych
- Błąd podczas tworzenia użytkownika (Identity)
- Błąd podczas generowania tokena JWT
- Nieoczekiwane wyjątki

**Obsługa:**

- Global exception middleware loguje błąd z correlation ID
- Zwraca ProblemDetails bez szczegółów wewnętrznych
- Structured logging (Serilog lub ILogger)

### Błędy po stronie klienta (404, 405):

- 404 Not Found: endpoint nie istnieje
- 405 Method Not Allowed: użyto GET zamiast POST

## 8. Rozważania dotyczące wydajności

### Potencjalne wąskie gardła:

1. **Hashowanie hasła**: Operacja CPU-intensive (PBKDF2 z iteracjami)

   - Nie da się zoptymalizować - konieczne dla bezpieczeństwa
   - Czas: ~50-100ms

2. **Sprawdzenie unikalności emaila**: Zapytanie do bazy danych

   - Optymalizacja: indeks na kolumnie email (już istnieje w Identity)
   - Czas: ~5-20ms

3. **Zapis do bazy danych**: INSERT transaction

   - Optymalizacja: connection pooling (domyślne w EF Core)
   - Czas: ~10-30ms

4. **Generowanie JWT tokena**: Operacja szyfrowania

   - Optymalizacja: użycie symetrycznego klucza (szybsze niż asymetryczny)
   - Czas: ~1-5ms

**Całkowity czas odpowiedzi:** ~70-155ms (akceptowalne dla operacji rejestracji)

### Strategie optymalizacji:

- **Connection pooling**: Domyślnie włączone w Npgsql
- **Async/await**: Wszystkie operacje I/O jako asynchroniczne
- **Brak niepotrzebnych roundtripów**: Pojedyncza transakcja do bazy danych
- **Indeksy**: Email index w tabeli users (AspNetUsers)

### Skalowanie:

- Endpoint może obsłużyć ~100-200 rejestracji/sekundę na pojedynczym serwerze
- Rate limiting zalecany dla ochrony przed spam-registrations
- Rozważyć CAPTCHA dla publicznych rejestracji

## 9. Etapy wdrożenia

### Krok 1: Konfiguracja ASP.NET Core Identity i JWT

**Plik:** `10xCards/Program.cs`

- Dodać `AddIdentity<User, IdentityRole<Guid>>()` z konfiguracją
- Skonfigurować polityki haseł Identity (PasswordOptions)
- Dodać `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`
- Skonfigurować JwtBearerOptions (issuer, audience, signing key)
- Dodać middleware: `UseAuthentication()` i `UseAuthorization()`

**Plik:** `10xCards/appsettings.json`

- Dodać sekcję `JwtSettings`:
  - SecretKey (minimum 32 znaki)
  - Issuer
  - Audience
  - ExpirationInMinutes

### Krok 2: Utworzenie modelu Result<T>

**Nowy plik:** `10xCards/Models/Common/Result.cs`

- Klasa generyczna do zwracania wyników z serwisów
- Właściwości: IsSuccess, Value, ErrorMessage, Errors
- Metody helper: Success(), Failure()

### Krok 3: Utworzenie AuthService

**Nowy plik:** `10xCards/Services/IAuthService.cs`

- Interface z metodą `RegisterUserAsync(RegisterRequest, CancellationToken)`
- Zwraca `Task<Result<AuthResponse>>`

**Nowy plik:** `10xCards/Services/AuthService.cs`

- Implementacja IAuthService
- Dependencies: UserManager<User>, IConfiguration, ILogger<AuthService>
- Logika:

  1. Sprawdzenie czy email istnieje (FindByEmailAsync)
  2. Utworzenie użytkownika (CreateAsync)
  3. Obsługa błędów Identity (IdentityResult)
  4. Generowanie JWT tokena (metoda prywatna GenerateJwtToken)
  5. Zwrócenie Result<AuthResponse>

### Krok 4: Rejestracja serwisu w DI

**Plik:** `10xCards/Program.cs`

- Dodać `builder.Services.AddScoped<IAuthService, AuthService>()`

### Krok 5: Utworzenie FluentValidation validator (opcjonalne)

**Nowy plik:** `10xCards/Validators/RegisterRequestValidator.cs`

- Klasa dziedzicząca po `AbstractValidator<RegisterRequest>`
- Reguły walidacji duplikujące DataAnnotations + dodatkowe business rules
- Instalacja pakietu: `FluentValidation.AspNetCore`

**Plik:** `10xCards/Program.cs` (jeśli używamy FluentValidation)

- Dodać `builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>()`

### Krok 6: Utworzenie minimal API endpoint

**Nowy plik:** `10xCards/Endpoints/AuthEndpoints.cs`

- Statyczna klasa z metodą extension `MapAuthEndpoints(this WebApplication app)`
- Endpoint POST /api/auth/register:
  ```csharp
  app.MapPost("/api/auth/register", async (
      RegisterRequest request,
      IAuthService authService,
      CancellationToken cancellationToken) => 
  {
      var result = await authService.RegisterUserAsync(request, cancellationToken);
      
      if (!result.IsSuccess)
          return Results.BadRequest(new { errors = result.Errors });
      
      return Results.Created($"/api/users/{result.Value.UserId}", result.Value);
  });
  ```


**Plik:** `10xCards/Program.cs`

- Przed `app.Run()` dodać `app.MapAuthEndpoints()`

### Krok 7: Implementacja global exception handler

**Nowy plik:** `10xCards/Middleware/GlobalExceptionHandlerMiddleware.cs`

- Middleware przechwytujący wyjątki
- Logowanie z correlation ID
- Zwracanie ProblemDetails zgodnie z RFC 7807
- Mapowanie wyjątków na odpowiednie kody statusu

**Plik:** `10xCards/Program.cs`

- Dodać `app.UseMiddleware<GlobalExceptionHandlerMiddleware>()` przed innymi middleware

### Krok 8: Dodanie NuGet packages

**Plik:** `10xCards/10xCards.csproj`

Dodać pakiety:

- `Microsoft.AspNetCore.Authentication.JwtBearer` (9.0.x)
- `System.IdentityModel.Tokens.Jwt` (najnowsza wersja zgodna z .NET 9)
- `FluentValidation.AspNetCore` (opcjonalnie, jeśli używamy)

### Krok 9: Testy manualne

Użyć narzędzia (Postman, curl, lub HTTP file w Rider/VS):

1. Test sukcesu: poprawne dane → 201 Created z tokenem
2. Test duplikatu: ten sam email dwa razy → 400 Bad Request
3. Test walidacji: słabe hasło → 400 Bad Request
4. Test walidacji: niezgodne hasła → 400 Bad Request
5. Test walidacji: nieprawidłowy email → 400 Bad Request

### Krok 10: Testy jednostkowe (opcjonalnie, ale zalecane)

**Nowy projekt:** `10xCards.Tests`

- Testy dla AuthService (mock UserManager)
- Testy dla RegisterRequestValidator
- Testy integracyjne dla endpoint (WebApplicationFactory)

### Krok 11: Dokumentacja API (opcjonalnie)

**Plik:** `10xCards/Program.cs`

- Dodać Swagger/OpenAPI: `AddEndpointsApiExplorer()` i `AddSwaggerGen()`
- Skonfigurować JWT authentication w Swagger
- Dodać XML comments dla endpointu

### Krok 12: Logging i monitoring

**Plik:** `10xCards/Services/AuthService.cs`

- Dodać structured logging:
  - `LogInformation` dla sukcesu rejestracji
  - `LogWarning` dla duplikatów emaili
  - `LogError` dla błędów serwera
- Correlation ID w każdym logu

### Krok 13: Rate limiting (opcjonalnie, ale zalecane)

**Plik:** `10xCards/Program.cs`

- Dodać `AddRateLimiter()` z polityką dla /api/auth/register
- Przykład: max 5 requestów na IP na 10 minut
- Zwracać 429 Too Many Requests

---

## Podsumowanie zależności

**Nowe pliki do utworzenia:**

1. `10xCards/Models/Common/Result.cs`
2. `10xCards/Services/IAuthService.cs`
3. `10xCards/Services/AuthService.cs`
4. `10xCards/Endpoints/AuthEndpoints.cs`
5. `10xCards/Middleware/GlobalExceptionHandlerMiddleware.cs`
6. `10xCards/Validators/RegisterRequestValidator.cs` (opcjonalnie)

**Pliki do modyfikacji:**

1. `10xCards/Program.cs` - konfiguracja serwisów i middleware
2. `10xCards/appsettings.json` - JWT settings
3. `10xCards/10xCards.csproj` - dodanie pakietów NuGet

**Istniejące pliki używane bez zmian:**

1. `10xCards/Models/Requests/RegisterRequest.cs`
2. `10xCards/Models/Responses/AuthResponse.cs`
3. `10xCards/Models/Responses/ErrorResponse.cs`
4. `10xCards/Database/Entities/User.cs`
5. `10xCards/Database/Context/ApplicationDbContext.cs`

### To-dos

- [ ] Skonfigurować ASP.NET Core Identity i JWT authentication w Program.cs
- [ ] Utworzyć model Result<T> dla obsługi wyników operacji
- [ ] Zaimplementować IAuthService i AuthService z logiką rejestracji
- [ ] Utworzyć minimal API endpoint POST /api/auth/register
- [ ] Zaimplementować global exception handler middleware
- [ ] Przeprowadzić testy manualne wszystkich scenariuszy