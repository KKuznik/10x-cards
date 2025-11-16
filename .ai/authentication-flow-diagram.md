# Diagram przepływu autentykacji - 10xCards

Ten dokument zawiera diagramy sekwencji Mermaid prezentujące przepływ autentykacji dla modułów logowania i rejestracji w aplikacji 10xCards.

## Przepływ rejestracji użytkownika (US-001)

```mermaid
sequenceDiagram
    actor Użytkownik
    participant Przeglądarka as Przeglądarka<br/>(Blazor SSR)
    participant Register as Register.razor<br/>(Komponent)
    participant EditForm as EditForm<br/>(Walidacja)
    participant RegisterCS as Register.razor.cs<br/>(Code-behind)
    participant AuthEndpoint as AuthEndpoints<br/>POST /api/auth/register
    participant AuthService as AuthService<br/>(IAuthService)
    participant UserManager as UserManager<br/>(ASP.NET Identity)
    participant Database as Baza danych<br/>(PostgreSQL)
    participant ClientAuth as ClientAuthenticationService
    participant LocalStorage as localStorage<br/>(JavaScript)
    participant AuthStateProvider as ClientAuthenticationStateProvider

    %% Wypełnianie formularza
    Użytkownik->>Przeglądarka: Otwiera /register
    Przeglądarka->>Register: Renderowanie strony rejestracji
    Register->>Użytkownik: Wyświetla formularz
    
    Użytkownik->>Register: Wypełnia dane (email, hasło, potwierdzenie)
    Użytkownik->>Register: Klika "Zarejestruj się"
    
    %% Walidacja po stronie klienta
    Register->>EditForm: Triggeruje OnValidSubmit
    EditForm->>EditForm: Walidacja DataAnnotations<br/>(Email, MinLength, Compare)
    
    alt Walidacja nie powiodła się
        EditForm->>Register: Błędy walidacji
        Register->>Użytkownik: Wyświetla komunikaty błędów
    else Walidacja powiodła się
        EditForm->>RegisterCS: HandleValidSubmit()
        
        %% Dodatkowa walidacja po stronie serwera
        RegisterCS->>RegisterCS: Sprawdza długość email (max 255)
        RegisterCS->>RegisterCS: Sprawdza długość hasła (8-100)
        RegisterCS->>RegisterCS: Sprawdza zgodność haseł
        
        alt Walidacja serwerowa nie powiodła się
            RegisterCS->>Register: Ustawia errorMessage
            Register->>Użytkownik: Wyświetla komunikat błędu
        else Walidacja serwerowa powiodła się
            %% Mapowanie do DTO
            RegisterCS->>RegisterCS: Tworzy RegisterRequest DTO
            
            %% Wywołanie API
            RegisterCS->>AuthEndpoint: POST /api/auth/register<br/>(RegisterRequest)
            AuthEndpoint->>AuthService: RegisterUserAsync(request)
            
            %% Sprawdzenie istnienia użytkownika
            AuthService->>UserManager: FindByEmailAsync(email)
            UserManager->>Database: SELECT * FROM Users WHERE Email = ?
            Database-->>UserManager: Wynik zapytania
            UserManager-->>AuthService: User lub null
            
            alt Email już istnieje
                AuthService->>AuthService: Loguje ostrzeżenie
                AuthService-->>AuthEndpoint: Result.Failure<br/>("Email is already registered")
                AuthEndpoint-->>RegisterCS: 400 Bad Request<br/>(ValidationProblem)
                RegisterCS->>RegisterCS: ExtractErrorMessage(errors)
                RegisterCS->>Register: Ustawia errorMessage
                Register->>Użytkownik: Wyświetla: "Email jest już zarejestrowany"
            else Email dostępny
                %% Tworzenie użytkownika
                AuthService->>AuthService: Tworzy obiekt User<br/>(Id, Email, UserName)
                AuthService->>UserManager: CreateAsync(user, password)
                UserManager->>UserManager: Hashuje hasło
                UserManager->>Database: INSERT INTO Users
                Database-->>UserManager: Potwierdzenie
                UserManager-->>AuthService: IdentityResult
                
                alt Tworzenie użytkownika nie powiodło się
                    AuthService->>AuthService: Loguje błąd i mapuje błędy Identity
                    AuthService-->>AuthEndpoint: Result.Failure<br/>(errors dictionary)
                    AuthEndpoint-->>RegisterCS: 400 Bad Request
                    RegisterCS->>Register: Ustawia errorMessage
                    Register->>Użytkownik: Wyświetla błędy (np. słabe hasło)
                else Użytkownik utworzony pomyślnie
                    %% Generowanie tokenu JWT
                    AuthService->>AuthService: GenerateJwtToken(user)
                    AuthService->>AuthService: Oblicza expiresAt<br/>(domyślnie +1440 min)
                    AuthService->>AuthService: Loguje sukces rejestracji
                    AuthService->>AuthService: Tworzy AuthResponse<br/>(UserId, Email, Token, ExpiresAt)
                    AuthService-->>AuthEndpoint: Result.Success(AuthResponse)
                    
                    %% Zwrot odpowiedzi
                    AuthEndpoint-->>RegisterCS: 201 Created<br/>(AuthResponse)
                    
                    %% Zapisywanie stanu autentykacji
                    RegisterCS->>ClientAuth: LoginAsync(token, expiresAt, email)
                    ClientAuth->>LocalStorage: saveAuthToken(token, expiresAt)<br/>(JavaScript Interop)
                    LocalStorage-->>ClientAuth: OK
                    ClientAuth->>LocalStorage: saveUsername(email)<br/>(JavaScript Interop)
                    LocalStorage-->>ClientAuth: OK
                    ClientAuth->>AuthStateProvider: NotifyAuthenticationStateChanged()
                    AuthStateProvider->>AuthStateProvider: Odświeża stan autentykacji
                    ClientAuth-->>RegisterCS: Zakończone
                    
                    %% Przekierowanie
                    RegisterCS->>Przeglądarka: NavigateTo("/", forceLoad: true)
                    Przeglądarka->>Użytkownik: Przekierowanie do strony głównej<br/>(zalogowany)
                end
            end
        end
    end
```

## Przepływ logowania użytkownika (US-002)

```mermaid
sequenceDiagram
    actor Użytkownik
    participant Przeglądarka as Przeglądarka<br/>(Blazor SSR)
    participant Login as Login.razor<br/>(Komponent)
    participant EditForm as EditForm<br/>(Walidacja)
    participant LoginCS as Login.razor.cs<br/>(Code-behind)
    participant AuthEndpoint as AuthEndpoints<br/>POST /api/auth/login
    participant AuthService as AuthService<br/>(IAuthService)
    participant UserManager as UserManager<br/>(ASP.NET Identity)
    participant Database as Baza danych<br/>(PostgreSQL)
    participant ClientAuth as ClientAuthenticationService
    participant LocalStorage as localStorage<br/>(JavaScript)
    participant AuthStateProvider as ClientAuthenticationStateProvider

    %% Wypełnianie formularza
    Użytkownik->>Przeglądarka: Otwiera /login
    Przeglądarka->>Login: Renderowanie strony logowania
    Login->>Użytkownik: Wyświetla formularz
    
    Użytkownik->>Login: Wypełnia dane (email, hasło)
    Użytkownik->>Login: Klika "Zaloguj się"
    
    %% Walidacja po stronie klienta
    Login->>EditForm: Triggeruje OnValidSubmit
    EditForm->>EditForm: Walidacja DataAnnotations<br/>(Email, Required)
    
    alt Walidacja nie powiodła się
        EditForm->>Login: Błędy walidacji
        Login->>Użytkownik: Wyświetla komunikaty błędów
    else Walidacja powiodła się
        EditForm->>LoginCS: HandleValidSubmit()
        
        %% Dodatkowa walidacja po stronie serwera
        LoginCS->>LoginCS: Sprawdza długość email (max 255)
        LoginCS->>LoginCS: Sprawdza długość hasła (max 100)
        
        alt Walidacja serwerowa nie powiodła się
            LoginCS->>Login: Ustawia errorMessage
            Login->>Użytkownik: Wyświetla komunikat błędu
        else Walidacja serwerowa powiodła się
            %% Mapowanie do DTO
            LoginCS->>LoginCS: Tworzy LoginRequest DTO
            
            %% Wywołanie API
            LoginCS->>AuthEndpoint: POST /api/auth/login<br/>(LoginRequest)
            AuthEndpoint->>AuthService: LoginUserAsync(request)
            
            %% Wyszukiwanie użytkownika
            AuthService->>UserManager: FindByEmailAsync(email)
            UserManager->>Database: SELECT * FROM Users WHERE Email = ?
            Database-->>UserManager: Wynik zapytania
            UserManager-->>AuthService: User lub null
            
            alt Użytkownik nie istnieje
                AuthService->>AuthService: Loguje ostrzeżenie<br/>("Login attempt with non-existent email")
                AuthService-->>AuthEndpoint: Result.Failure<br/>("Invalid email or password")
                AuthEndpoint-->>LoginCS: 401 Unauthorized<br/>(ErrorResponse)
                LoginCS->>LoginCS: Ustawia errorMessage
                LoginCS->>Login: Focus na pole email (WCAG)
                Login->>Użytkownik: Wyświetla: "Nieprawidłowy email lub hasło"
            else Użytkownik istnieje
                %% Weryfikacja hasła
                AuthService->>UserManager: CheckPasswordAsync(user, password)
                UserManager->>UserManager: Porównuje hash hasła
                UserManager-->>AuthService: bool (valid/invalid)
                
                alt Hasło nieprawidłowe
                    AuthService->>AuthService: Loguje ostrzeżenie<br/>("Invalid password for user")
                    AuthService-->>AuthEndpoint: Result.Failure<br/>("Invalid email or password")
                    AuthEndpoint-->>LoginCS: 401 Unauthorized
                    LoginCS->>LoginCS: Ustawia errorMessage
                    LoginCS->>Login: Focus na pole email
                    Login->>Użytkownik: Wyświetla: "Nieprawidłowy email lub hasło"
                else Hasło prawidłowe
                    %% Generowanie tokenu JWT
                    AuthService->>AuthService: GenerateJwtToken(user)
                    AuthService->>AuthService: Oblicza expiresAt<br/>(domyślnie +1440 min)
                    AuthService->>AuthService: Loguje sukces logowania
                    AuthService->>AuthService: Tworzy AuthResponse<br/>(UserId, Email, Token, ExpiresAt)
                    AuthService-->>AuthEndpoint: Result.Success(AuthResponse)
                    
                    %% Zwrot odpowiedzi
                    AuthEndpoint-->>LoginCS: 200 OK<br/>(AuthResponse)
                    
                    %% Zapisywanie stanu autentykacji
                    LoginCS->>ClientAuth: LoginAsync(token, expiresAt, email)
                    ClientAuth->>LocalStorage: saveAuthToken(token, expiresAt)<br/>(JavaScript Interop)
                    LocalStorage-->>ClientAuth: OK
                    ClientAuth->>LocalStorage: saveUsername(email)<br/>(JavaScript Interop)
                    LocalStorage-->>ClientAuth: OK
                    ClientAuth->>AuthStateProvider: NotifyAuthenticationStateChanged()
                    AuthStateProvider->>AuthStateProvider: Odświeża stan autentykacji
                    ClientAuth-->>LoginCS: Zakończone
                    
                    %% Przekierowanie
                    LoginCS->>Przeglądarka: NavigateTo("/", forceLoad: true)
                    Przeglądarka->>Użytkownik: Przekierowanie do strony głównej<br/>(zalogowany)
                end
            end
        end
    end
```

## Przepływ wylogowania użytkownika

```mermaid
sequenceDiagram
    actor Użytkownik
    participant Przeglądarka as Przeglądarka<br/>(Blazor SSR)
    participant Component as Komponent<br/>(np. NavMenu)
    participant AuthEndpoint as AuthEndpoints<br/>POST /api/auth/logout
    participant HttpContext as HttpContext
    participant AuthService as AuthService<br/>(IAuthService)
    participant Database as Baza danych<br/>(PostgreSQL)
    participant ClientAuth as ClientAuthenticationService
    participant LocalStorage as localStorage<br/>(JavaScript)
    participant AuthStateProvider as ClientAuthenticationStateProvider

    Użytkownik->>Component: Klika "Wyloguj się"
    
    %% Wywołanie API wylogowania
    Component->>AuthEndpoint: POST /api/auth/logout<br/>(z JWT token w Authorization header)
    AuthEndpoint->>HttpContext: Odczyt ClaimTypes.NameIdentifier
    
    alt Użytkownik nieautoryzowany lub błędny token
        HttpContext-->>AuthEndpoint: null lub nieprawidłowy userId
        AuthEndpoint-->>Component: 401 Unauthorized<br/>("Unauthorized")
        Component->>Użytkownik: Wyświetla komunikat błędu
    else Użytkownik autoryzowany
        HttpContext-->>AuthEndpoint: userId (Guid)
        AuthEndpoint->>AuthService: LogoutUserAsync(userId)
        
        %% Logika wylogowania (np. unieważnienie sesji, audit log)
        AuthService->>Database: Zapisuje zdarzenie wylogowania<br/>(dla audytu bezpieczeństwa)
        Database-->>AuthService: OK
        AuthService-->>AuthEndpoint: Result.Success
        
        AuthEndpoint-->>Component: 204 No Content
        
        %% Czyszczenie stanu po stronie klienta
        Component->>ClientAuth: LogoutAsync()
        ClientAuth->>LocalStorage: clearAuthToken()<br/>(JavaScript Interop)
        LocalStorage-->>ClientAuth: OK
        ClientAuth->>AuthStateProvider: NotifyAuthenticationStateChanged()
        AuthStateProvider->>AuthStateProvider: Odświeża stan (anonimowy)
        ClientAuth-->>Component: Zakończone
        
        Component->>Przeglądarka: NavigateTo("/login")
        Przeglądarka->>Użytkownik: Przekierowanie do strony logowania
    end
```

## Obsługa błędów i przypadków brzegowych

```mermaid
sequenceDiagram
    actor Użytkownik
    participant Component as Login/Register<br/>Component
    participant Service as AuthService
    participant Network as Sieć

    %% Timeout
    rect rgb(255, 240, 240)
        Note over Użytkownik,Network: Scenariusz: Timeout (przekroczenie czasu)
        Użytkownik->>Component: Wysyła żądanie
        Component->>Service: LoginAsync/RegisterAsync<br/>(timeout: 30s)
        Service->>Network: HTTP Request
        Network--xComponent: TaskCanceledException (timeout)
        Component->>Component: Catch TaskCanceledException
        Component->>Użytkownik: "Żądanie przekroczyło limit czasu.<br/>Spróbuj ponownie."
    end

    %% Błąd sieci
    rect rgb(255, 245, 230)
        Note over Użytkownik,Network: Scenariusz: Błąd połączenia sieciowego
        Użytkownik->>Component: Wysyła żądanie
        Component->>Service: LoginAsync/RegisterAsync
        Service->>Network: HTTP Request
        Network--xComponent: HttpRequestException
        Component->>Component: Catch HttpRequestException
        Component->>Użytkownik: "Błąd połączenia z serwerem.<br/>Sprawdź połączenie internetowe."
    end

    %% Ogólny wyjątek
    rect rgb(250, 240, 255)
        Note over Użytkownik,Service: Scenariusz: Nieoczekiwany błąd
        Użytkownik->>Component: Wysyła żądanie
        Component->>Service: LoginAsync/RegisterAsync
        Service--xComponent: Exception (nieoczekiwany)
        Component->>Component: Catch Exception<br/>Loguje błąd (bez danych użytkownika)
        Component->>Użytkownik: "Wystąpił błąd.<br/>Spróbuj ponownie później."
    end
```

## Sprawdzanie stanu autentykacji przy załadowaniu strony

```mermaid
sequenceDiagram
    actor Użytkownik
    participant Przeglądarka as Przeglądarka
    participant BlazorApp as Blazor App
    participant AuthStateProvider as ClientAuthenticationStateProvider
    participant LocalStorage as localStorage<br/>(JavaScript)
    participant JWTParser as JWT Parser

    Użytkownik->>Przeglądarka: Otwiera aplikację
    Przeglądarka->>BlazorApp: Inicjalizacja aplikacji
    BlazorApp->>AuthStateProvider: GetAuthenticationStateAsync()
    
    AuthStateProvider->>LocalStorage: getAuthToken()<br/>(JavaScript Interop)
    LocalStorage-->>AuthStateProvider: token (string) lub null
    
    alt Token nie istnieje
        AuthStateProvider->>AuthStateProvider: CreateAnonymousState()
        AuthStateProvider-->>BlazorApp: AuthenticationState (Anonymous)
        BlazorApp->>Przeglądarka: Renderuje widok dla niezalogowanych
    else Token istnieje
        AuthStateProvider->>LocalStorage: isTokenExpired()<br/>(JavaScript Interop)
        LocalStorage-->>AuthStateProvider: bool (expired/valid)
        
        alt Token wygasł
            AuthStateProvider->>AuthStateProvider: CreateAnonymousState()
            AuthStateProvider-->>BlazorApp: AuthenticationState (Anonymous)
            BlazorApp->>Przeglądarka: Renderuje widok dla niezalogowanych
        else Token ważny
            AuthStateProvider->>JWTParser: ParseClaimsFromJwt(token)
            JWTParser-->>AuthStateProvider: Claims (UserId, Email, itp.)
            
            alt Nie udało się sparsować claims
                AuthStateProvider->>AuthStateProvider: CreateAnonymousState()
                AuthStateProvider-->>BlazorApp: AuthenticationState (Anonymous)
            else Claims sparsowane poprawnie
                AuthStateProvider->>AuthStateProvider: Tworzy ClaimsIdentity<br/>(authenticationType: "jwt")
                AuthStateProvider->>AuthStateProvider: Tworzy ClaimsPrincipal
                AuthStateProvider-->>BlazorApp: AuthenticationState (Authenticated)
                BlazorApp->>Przeglądarka: Renderuje widok dla zalogowanych
            end
        end
    end
    
    Przeglądarka->>Użytkownik: Wyświetla odpowiedni interfejs
```

## Podsumowanie architektury autentykacji

### Komponenty systemu:

1. **Warstwa prezentacji (Blazor SSR)**:
   - `Login.razor` / `Register.razor` - komponenty UI
   - `Login.razor.cs` / `Register.razor.cs` - logika biznesowa komponentów
   - `EditForm` z `DataAnnotationsValidator` - walidacja po stronie klienta

2. **Warstwa API (ASP.NET Core)**:
   - `AuthEndpoints.cs` - endpointy REST API (POST /api/auth/register, /login, /logout)
   - Middleware autentykacji JWT
   - AntiForgeryToken dla ochrony CSRF

3. **Warstwa serwisów**:
   - `AuthService` (IAuthService) - główna logika autentykacji
   - `ClientAuthenticationService` - zarządzanie stanem po stronie klienta
   - `ClientAuthenticationStateProvider` - dostarczanie stanu autentykacji dla Blazor

4. **Warstwa danych**:
   - `UserManager<User>` (ASP.NET Core Identity)
   - Entity Framework Core
   - PostgreSQL

5. **Przechowywanie stanu**:
   - `localStorage` (przeglądarka) - przechowywanie JWT i danych użytkownika
   - JavaScript Interop - komunikacja między Blazor a JavaScript

### Bezpieczeństwo:

- **Hashowanie haseł**: ASP.NET Core Identity (domyślnie PBKDF2)
- **JWT tokens**: Podpisane tokeny z czasem wygaśnięcia (domyślnie 1440 minut)
- **HTTPS**: Wymagane dla produkcji
- **Walidacja wielopoziomowa**: klient → serwer → baza danych
- **Rate limiting**: Zalecane (opisane w komentarzach kodu)
- **Logging**: Audit trail dla zdarzeń bezpieczeństwa (logowanie, wylogowanie, niepowodzenia)
- **Ochrona CSRF**: AntiForgeryToken w formularzach

### Zgodność z PRD:

- ✅ **US-001**: Rejestracja konta z walidacją email i hasła
- ✅ **US-002**: Logowanie z bezpiecznym przechowywaniem danych
- ✅ **US-009**: Bezpieczny dostęp i autoryzacja (JWT, izolacja użytkowników)

### Wymagania RODO:

- Hasła są hashowane i nigdy nie są przechowywane w postaci jawnej
- Dane użytkownika są logowane z zachowaniem ostrożności (bez wrażliwych informacji w logach produkcyjnych)
- System przewiduje możliwość usunięcia konta (endpoint do implementacji zgodnie z US-023)

