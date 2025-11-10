<!-- 0e492d37-de88-4739-828f-916709176a56 9eddf819-d6a9-4c38-98f2-d5589d5e5a7a -->
# Plan Implementacji Widoku Uwierzytelniania

## 1. Przegląd

Widok uwierzytelniania składa się z dwóch oddzielnych stron Blazor SSR: **Login** i **Register**. Każda strona zawiera dedykowany formularz z walidacją po stronie klienta, integracją z API, obsługą błędów oraz przekierowaniami po udanym uwierzytelnieniu. Celem jest umożliwienie użytkownikom bezpiecznego logowania i rejestracji zgodnie z user stories US-001 i US-002.

## 2. Routing widoku

- **Strona rejestracji:** `/register` - dostępna dla niezalogowanych użytkowników
- **Strona logowania:** `/login` - dostępna dla niezalogowanych użytkowników
- **Przekierowanie po sukcesie:** `/generate` - główny widok generowania fiszek (po zalogowaniu/rejestracji)

## 3. Struktura komponentów

```
Components/Pages/
├── Register.razor (strona rejestracji)
├── Register.razor.cs (code-behind)
├── Login.razor (strona logowania)
└── Login.razor.cs (code-behind)

Services/
└── IAuthService.cs (już istnieje - do użycia w komponentach)
```

**Brak dodatkowych komponentów potomnych** - każda strona jest samodzielnym komponentem z wbudowanym formularzem.

## 4. Szczegóły komponentu

### 4.1 Komponent Register.razor

**Opis:** Strona rejestracji użytkownika z formularzem zawierającym pola email, hasło i potwierdzenie hasła.

**Główne elementy HTML:**

- `<EditForm Model="registerModel">` - kontener formularza Blazor z modelem danych
- `<DataAnnotationsValidator />` - walidator wykorzystujący atrybuty z modelu
- `<ValidationSummary />` - podsumowanie błędów walidacji
- Pola input: Email, Password, ConfirmPassword
- `<ValidationMessage For="@(() => registerModel.Email)" />` - inline komunikaty błędów
- Przycisk submit: "Zarejestruj się"
- Link do strony logowania
- Alert div dla błędów API (np. "Email już istnieje")

**Obsługiwane zdarzenia:**

- `OnValidSubmit` - wywołane po pomyślnej walidacji formularza, wysyła żądanie do API `/api/auth/register`
- Nawigacja onclick dla linku do `/login`

**Warunki walidacji:**

- Email: wymagany, format email, max 255 znaków
- Password: wymagany, min 8 znaków, max 100 znaków, wymaga wielkiej litery, małej litery, cyfry i znaku specjalnego
- ConfirmPassword: wymagany, musi być identyczny z Password

**Typy wymagane:**

- `RegisterRequest` (DTO request)
- `AuthResponse` (DTO response)
- `ErrorResponse` (błąd API)

**Propsy komponenu:** Brak (strona główna)

### 4.2 Komponent Login.razor

**Opis:** Strona logowania użytkownika z formularzem zawierającym pola email i hasło.

**Główne elementy HTML:**

- `<EditForm Model="loginModel">` - kontener formularza Blazor
- `<DataAnnotationsValidator />` - walidator
- `<ValidationSummary />` - podsumowanie błędów
- Pola input: Email, Password
- `<ValidationMessage />` - inline komunikaty błędów
- Przycisk submit: "Zaloguj się"
- Link do strony rejestracji
- Alert div dla błędów uwierzytelniania (np. "Nieprawidłowy email lub hasło")

**Obsługiwane zdarzenia:**

- `OnValidSubmit` - wysyła żądanie do API `/api/auth/login`
- Nawigacja onclick dla linku do `/register`

**Warunki walidacji:**

- Email: wymagany, format email
- Password: wymagany

**Typy wymagane:**

- `LoginRequest` (DTO request)
- `AuthResponse` (DTO response)
- `ErrorResponse` (błąd API)

**Propsy komponentu:** Brak (strona główna)

## 5. Typy

### 5.1 Modele Request (już zdefiniowane)

**RegisterRequest** (`Models/Requests/RegisterRequest.cs`)

```csharp
public sealed class RegisterRequest {
    public string Email { get; set; }           // string, required, email format, max 255
    public string Password { get; set; }        // string, required, min 8, max 100, regex pattern
    public string ConfirmPassword { get; set; } // string, required, compare with Password
}
```

**LoginRequest** (`Models/Requests/LoginRequest.cs`)

```csharp
public sealed class LoginRequest {
    public string Email { get; set; }    // string, required, email format
    public string Password { get; set; } // string, required
}
```

### 5.2 Modele Response (już zdefiniowane)

**AuthResponse** (`Models/Responses/AuthResponse.cs`)

```csharp
public sealed class AuthResponse {
    public Guid UserId { get; set; }       // Guid
    public string Email { get; set; }      // string
    public string Token { get; set; }      // string - JWT token
    public DateTime ExpiresAt { get; set; } // DateTime - token expiration
}
```

**ErrorResponse** (`Models/Responses/ErrorResponse.cs`)

```csharp
public sealed class ErrorResponse {
    public string Message { get; set; }                         // string - główny komunikat błędu
    public string? ErrorCode { get; set; }                      // string? - kod błędu (opcjonalny)
    public Dictionary<string, List<string>>? Errors { get; set; } // Dict? - błędy walidacji per pole
}
```

### 5.3 Nowe ViewModels (do utworzenia w komponentach)

**RegisterViewModel** - używany wewnątrz Register.razor.cs

```csharp
public class RegisterViewModel {
    [Required(ErrorMessage = "Email jest wymagany")]
    [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
    [MaxLength(255, ErrorMessage = "Email nie może przekraczać 255 znaków")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hasło jest wymagane")]
    [MinLength(8, ErrorMessage = "Hasło musi mieć co najmniej 8 znaków")]
    [MaxLength(100, ErrorMessage = "Hasło nie może przekraczać 100 znaków")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
        ErrorMessage = "Hasło musi zawierać wielką literę, małą literę, cyfrę i znak specjalny")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane")]
    [Compare(nameof(Password), ErrorMessage = "Hasła muszą być identyczne")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

**LoginViewModel** - używany wewnątrz Login.razor.cs

```csharp
public class LoginViewModel {
    [Required(ErrorMessage = "Email jest wymagany")]
    [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hasło jest wymagane")]
    public string Password { get; set; } = string.Empty;
}
```

## 6. Zarządzanie stanem

**Lokalna zmiana stanu w komponencie** - Blazor SSR używa prostego lokalnego stanu w komponentach.

**Stan formularza:**

- `registerModel` lub `loginModel` - instancja ViewModel przechowująca dane formularza
- `errorMessage` - string do wyświetlania błędów API (np. "Invalid email or password")
- `isSubmitting` - bool wskazujący, czy żądanie API jest w trakcie (do wyłączenia przycisku submit)

**Stan jest zarządzany bezpośrednio w code-behind (.razor.cs)** bez potrzeby custom hooków czy zaawansowanego state management (brak Redux/Flux w Blazor SSR MVP).

**Dane sesji (po udanym loginie/rejestracji):**

- JWT token przechowywany w **localStorage** (przez JavaScript Interop)
- Alternatywnie: użycie cookie-based authentication jeśli preferowane przez architekturę

## 7. Integracja API

### 7.1 Endpoint rejestracji

**Endpoint:** `POST /api/auth/register`

**Request Type:** `RegisterRequest`

```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!"
}
```

**Response Type:** `AuthResponse` (201 Created)

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-11-04T12:00:00Z"
}
```

**Error Response:** `ValidationProblem` (400 Bad Request)

```json
{
  "errors": {
    "email": ["Email is already registered"],
    "password": ["Password must be at least 8 characters"]
  }
}
```

**Implementacja w komponencie:**

- Wstrzyknięcie `IAuthService` przez DI
- Wywołanie `await authService.RegisterUserAsync(request, cancellationToken)`
- Mapowanie ViewModel -> RegisterRequest

### 7.2 Endpoint logowania

**Endpoint:** `POST /api/auth/login`

**Request Type:** `LoginRequest`

```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response Type:** `AuthResponse` (200 OK)

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-11-04T12:00:00Z"
}
```

**Error Response:** `ErrorResponse` (401 Unauthorized)

```json
{
  "message": "Invalid email or password"
}
```

**Implementacja w komponencie:**

- Wstrzyknięcie `IAuthService` przez DI
- Wywołanie `await authService.LoginUserAsync(request, cancellationToken)`
- Mapowanie ViewModel -> LoginRequest

## 8. Interakcje użytkownika

### 8.1 Strona Register

1. **Użytkownik wchodzi na `/register`**

   - Wyświetlany jest pusty formularz rejestracji

2. **Użytkownik wypełnia formularz**

   - Wprowadza email, hasło, potwierdzenie hasła
   - Walidacja inline działa podczas wpisywania (onblur)
   - ValidationMessage wyświetla błędy pod każdym polem

3. **Użytkownik klika "Zarejestruj się"**

   - Jeśli walidacja nie przechodzi: wyświetlane są komunikaty błędów, formularz nie jest wysyłany
   - Jeśli walidacja przechodzi:
     - Przycisk submit zostaje wyłączony (`isSubmitting = true`)
     - Wysyłane jest żądanie POST do `/api/auth/register`

4. **Odpowiedź API (sukces - 201 Created):**

   - Token JWT zapisywany w localStorage
   - Użytkownik przekierowywany do `/generate`

5. **Odpowiedź API (błąd - 400 Bad Request):**

   - Wyświetlane są błędy walidacji z serwera (np. "Email is already registered")
   - Formularz pozostaje aktywny, użytkownik może poprawić dane

6. **Link "Masz już konto? Zaloguj się"**

   - Przekierowuje użytkownika do `/login`

### 8.2 Strona Login

1. **Użytkownik wchodzi na `/login`**

   - Wyświetlany jest pusty formularz logowania

2. **Użytkownik wypełnia formularz**

   - Wprowadza email i hasło
   - Walidacja inline działa podczas wpisywania

3. **Użytkownik klika "Zaloguj się"**

   - Jeśli walidacja nie przechodzi: wyświetlane są komunikaty błędów
   - Jeśli walidacja przechodzi:
     - Przycisk submit zostaje wyłączony
     - Wysyłane jest żądanie POST do `/api/auth/login`

4. **Odpowiedź API (sukces - 200 OK):**

   - Token JWT zapisywany w localStorage
   - Użytkownik przekierowywany do `/generate`

5. **Odpowiedź API (błąd - 401 Unauthorized):**

   - Wyświetlany jest komunikat "Nieprawidłowy email lub hasło"
   - Formularz pozostaje aktywny

6. **Link "Nie masz konta? Zarejestruj się"**

   - Przekierowuje użytkownika do `/register`

## 9. Warunki i walidacja

### 9.1 Walidacja po stronie klienta (Blazor)

**Register.razor:**

- Email:
  - Weryfikacja: Required, EmailAddress, MaxLength(255)
  - Wyświetlanie: ValidationMessage inline pod polem input

- Password:
  - Weryfikacja: Required, MinLength(8), MaxLength(100), RegularExpression (uppercase, lowercase, digit, special char)
  - Wyświetlanie: ValidationMessage inline

- ConfirmPassword:
  - Weryfikacja: Required, Compare z Password
  - Wyświetlanie: ValidationMessage inline

**Login.razor:**

- Email:
  - Weryfikacja: Required, EmailAddress
  - Wyświetlanie: ValidationMessage inline

- Password:
  - Weryfikacja: Required
  - Wyświetlanie: ValidationMessage inline

**Wpływ na UI:**

- Przycisk submit wyłączony podczas `isSubmitting == true`
- Komunikaty walidacji wyświetlane na czerwono pod polami
- ValidationSummary wyświetla wszystkie błędy na górze formularza

### 9.2 Walidacja po stronie serwera (API)

**Register endpoint:**

- Zwraca 400 Bad Request z `ValidationProblem` zawierającym errors dictionary
- Przykładowe błędy:
  - `email: ["Email is already registered"]`
  - `password: ["Password must be at least 8 characters"]`

**Login endpoint:**

- Zwraca 401 Unauthorized z `ErrorResponse.Message = "Invalid email or password"`

**Obsługa w komponencie:**

- Parsowanie błędów API i wyświetlanie ich w alert div lub dodawanie do ValidationSummary programowo

## 10. Obsługa błędów

### 10.1 Błędy walidacji (400 Bad Request - Register)

**Scenariusz:** Email już istnieje w bazie, serwer zwraca 400

**Obsługa:**

- Komponent przechwytuje response z `errors` dictionary
- Wyświetla komunikat błędu w czerwonym alert div nad formularzem: "Email is already registered"
- Użytkownik może zmienić email i spróbować ponownie

### 10.2 Błędy uwierzytelniania (401 Unauthorized - Login)

**Scenariusz:** Nieprawidłowy email lub hasło

**Obsługa:**

- Komponent przechwytuje response z `message`
- Wyświetla komunikat w czerwonym alert div: "Nieprawidłowy email lub hasło"
- Formularz pozostaje aktywny do ponownej próby

### 10.3 Błędy sieciowe / 500 Internal Server Error

**Scenariusz:** Brak połączenia z serwerem lub nieoczekiwany błąd serwera

**Obsługa:**

- Try-catch w metodzie submit
- Wyświetlenie ogólnego komunikatu: "Wystąpił błąd. Spróbuj ponownie później."
- Logowanie błędu do konsoli przeglądarki

### 10.4 Timeouty API

**Scenariusz:** Żądanie trwa zbyt długo

**Obsługa:**

- Ustawienie CancellationToken z timeoutem (opcjonalne)
- Wyświetlenie komunikatu: "Żądanie przekroczyło czas oczekiwania. Spróbuj ponownie."

## 11. Kroki implementacji

### Krok 1: Utworzenie ViewModels

- Zdefiniować `RegisterViewModel` i `LoginViewModel` w plikach .cs komponentów
- Dodać odpowiednie atrybuty walidacji (DataAnnotations)

### Krok 2: Implementacja Register.razor

- Utworzyć plik `Components/Pages/Register.razor`
- Dodać dyrektywę `@page "/register"`
- Zbudować EditForm z polami: Email, Password, ConfirmPassword
- Dodać DataAnnotationsValidator i ValidationSummary
- Dodać ValidationMessage dla każdego pola
- Stworzyć div dla komunikatów błędów API
- Dodać link do strony logowania

### Krok 3: Implementacja Register.razor.cs (code-behind)

- Utworzyć partial class `Register`
- Wstrzyknąć `IAuthService`, `NavigationManager`
- Zdefiniować zmienne stanu: `registerModel`, `errorMessage`, `isSubmitting`
- Zaimplementować metodę `HandleValidSubmit`:
  - Mapowanie RegisterViewModel -> RegisterRequest
  - Wywołanie authService.RegisterUserAsync
  - Obsługa sukcesu: zapisanie tokenu, nawigacja do `/generate`
  - Obsługa błędów: parsowanie errors, wyświetlanie errorMessage

### Krok 4: Implementacja Login.razor

- Utworzyć plik `Components/Pages/Login.razor`
- Dodać dyrektywę `@page "/login"`
- Zbudować EditForm z polami: Email, Password
- Dodać DataAnnotationsValidator, ValidationSummary, ValidationMessages
- Stworzyć div dla komunikatów błędów API
- Dodać link do strony rejestracji

### Krok 5: Implementacja Login.razor.cs (code-behind)

- Utworzyć partial class `Login`
- Wstrzyknąć `IAuthService`, `NavigationManager`
- Zdefiniować zmienne stanu: `loginModel`, `errorMessage`, `isSubmitting`
- Zaimplementować metodę `HandleValidSubmit`:
  - Mapowanie LoginViewModel -> LoginRequest
  - Wywołanie authService.LoginUserAsync
  - Obsługa sukcesu: zapisanie tokenu, nawigacja do `/generate`
  - Obsługa błędów: wyświetlanie errorMessage

### Krok 6: Integracja z localStorage (JWT token)

- Zaimplementować JavaScript Interop do zapisywania tokenu w localStorage
- Utworzyć plik `wwwroot/js/auth.js` z funkcjami `saveToken(token)` i `getToken()`
- Wywołać funkcję JS z Blazor przez IJSRuntime

### Krok 7: Stylizacja z Bootstrap

- Użyć klas Bootstrap do stylowania formularzy: `form-control`, `form-group`, `btn btn-primary`
- Dodać responsywne klasy dla layoutu: `container`, `row`, `col-md-6 offset-md-3`
- Stworzyć czytelne komunikaty błędów z klasą `alert alert-danger`

### Krok 8: Testowanie formularzy

- Ręczne testowanie walidacji po stronie klienta (puste pola, nieprawidłowy email, hasła nie pasują)
- Testowanie integracji z API (poprawna rejestracja, poprawne logowanie)
- Testowanie scenariuszy błędów (email już istnieje, nieprawidłowe hasło)
- Weryfikacja przekierowań i zapisywania tokenu

### Krok 9: Obsługa accessibility

- Dodać atrybuty `aria-label` do pól formularza
- Zapewnić obsługę nawigacji klawiaturą (tab, enter)
- Testowanie z czytnikami ekranu

### Krok 10: Finalizacja i przegląd kodu

- Upewnić się, że wszystkie komunikaty błędów są wyświetlane poprawnie
- Sprawdzić, czy kod jest zgodny z praktykami clean code (early returns, guard clauses)
- Dodać komentarze XML do metod publicznych
- Code review

### To-dos

- [ ] Zdefiniować ViewModels (RegisterViewModel, LoginViewModel) z atrybutami walidacji
- [ ] Utworzyć Register.razor z EditForm, polami formularza i komponentami walidacji Blazor
- [ ] Zaimplementować Register.razor.cs z logiką obsługi formularza i integracją API
- [ ] Utworzyć Login.razor z EditForm, polami formularza i komponentami walidacji Blazor
- [ ] Zaimplementować Login.razor.cs z logiką obsługi formularza i integracją API
- [ ] Utworzyć JavaScript Interop dla zapisywania JWT tokenu w localStorage
- [ ] Zastosować klasy Bootstrap do formularzy i layoutu dla responsywnego designu
- [ ] Przetestować walidację formularzy, scenariusze błędów i integrację API
- [ ] Dodać atrybuty aria-label i zapewnić obsługę nawigacji klawiaturą