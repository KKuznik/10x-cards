<!-- 6187fcff-b20f-4bb0-938d-be5e15ffb015 4d8867f7-ba32-4ccc-ae85-3822310e53be -->
# Plan testów jednostkowych z xUnit dla 10xCards

## 1. Struktura projektu testowego

Utworzymy nowy projekt testowy `10xCards.Tests` w strukturze:

```
10xCards.Tests/
├── Services/
│   ├── AuthServiceTests.cs
│   ├── FlashcardServiceTests.cs
│   ├── GenerationServiceTests.cs
│   ├── ChatGptServiceTests.cs
│   ├── OpenRouterServiceTests.cs
│   └── ClientAuthenticationServiceTests.cs
├── Utilities/
│   └── JwtHelperTests.cs
├── Validators/
│   ├── RegisterRequestValidatorTests.cs
│   ├── CreateFlashcardRequestValidatorTests.cs
│   ├── GenerateFlashcardsRequestValidatorTests.cs
│   └── UpdateFlashcardRequestValidatorTests.cs
├── Fixtures/
│   └── DatabaseFixture.cs
└── 10xCards.Tests.csproj
```

## 2. Pakiety NuGet

W pliku `10xCards.Tests.csproj` dodamy:

- `xunit` (framework testowy)
- `xunit.runner.visualstudio` (runner dla Visual Studio)
- `NSubstitute` (mockowanie zależności)
- `FluentAssertions` (czytelne asercje)
- `Microsoft.EntityFrameworkCore.InMemory` (in-memory database dla testów)
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (Identity dla testów)
- `coverlet.collector` (pokrycie kodu)

## 3. Zakres testów dla poszczególnych serwisów

### AuthService (`AuthServiceTests.cs`)

**Zależności do mockowania:** UserManager<User>, IConfiguration, ILogger

Scenariusze testowe:

- **RegisterUserAsync**
  - ✅ Pomyślna rejestracja nowego użytkownika
  - ❌ Rejestracja z istniejącym emailem (duplikat)
  - ❌ Błąd podczas tworzenia użytkownika (Identity errors)
  - ✅ Poprawne generowanie JWT tokenu
  - ✅ Logowanie ostrzeżeń dla duplikatów

- **LoginUserAsync**
  - ✅ Pomyślne logowanie z prawidłowymi danymi
  - ❌ Logowanie z nieistniejącym emailem
  - ❌ Logowanie z błędnym hasłem
  - ✅ Poprawne generowanie JWT tokenu
  - ✅ Logowanie ostrzeżeń dla nieudanych prób

- **LogoutUserAsync**
  - ✅ Pomyślne wylogowanie użytkownika
  - ❌ Wylogowanie z pustym Guid.Empty

### FlashcardService (`FlashcardServiceTests.cs`)

**Zależności do mockowania:** ApplicationDbContext (in-memory), ILogger

Scenariusze testowe:

- **ListFlashcardsAsync**
  - ✅ Pobieranie fiszek dla konkretnego użytkownika (row-level security)
  - ✅ Filtrowanie po źródle (source)
  - ✅ Wyszukiwanie w treści (front/back)
  - ✅ Sortowanie (createdAt, updatedAt, front)
  - ✅ Paginacja (page, pageSize)
  - ❌ Pusta userId (Guid.Empty)
  - ❌ Null query

- **GetFlashcardAsync**
  - ✅ Pobieranie istniejącej fiszki
  - ❌ Pobieranie nieistniejącej fiszki
  - ❌ Próba dostępu do fiszki innego użytkownika
  - ❌ Pusta userId

- **CreateFlashcardAsync**
  - ✅ Tworzenie nowej fiszki
  - ❌ Pusta userId
  - ✅ Poprawne ustawienie dat (CreatedAt, UpdatedAt)

- **CreateFlashcardsBatchAsync**
  - ✅ Tworzenie wielu fiszek w batch
  - ✅ Powiązanie z generationId
  - ❌ Pusta lista fiszek
  - ❌ Pusta userId

- **UpdateFlashcardAsync**
  - ✅ Aktualizacja istniejącej fiszki
  - ❌ Aktualizacja nieistniejącej fiszki
  - ❌ Próba aktualizacji fiszki innego użytkownika
  - ✅ Aktualizacja UpdatedAt

- **DeleteFlashcardAsync**
  - ✅ Usunięcie istniejącej fiszki
  - ❌ Usunięcie nieistniejącej fiszki
  - ❌ Próba usunięcia fiszki innego użytkownika

### GenerationService (`GenerationServiceTests.cs`)

**Zależności do mockowania:** ApplicationDbContext (in-memory), IChatGptService, ILogger

Scenariusze testowe:

- **ListGenerationsAsync**
  - ✅ Pobieranie generacji dla konkretnego użytkownika
  - ✅ Sortowanie (asc/desc)
  - ✅ Paginacja
  - ✅ Obliczanie AcceptanceRate
  - ✅ Obliczanie statystyk użytkownika
  - ❌ Pusta userId

- **GetGenerationDetailsAsync**
  - ✅ Pobieranie szczegółów generacji z fiszkami
  - ❌ Pobieranie nieistniejącej generacji
  - ❌ Próba dostępu do generacji innego użytkownika

- **GenerateFlashcardsAsync**
  - ✅ Pomyślne generowanie fiszek przez AI
  - ✅ Zapis generacji do bazy danych
  - ✅ Pomiar czasu generowania (GenerationDuration)
  - ✅ Obliczanie hash źródła
  - ❌ Błąd API (ChatGptService rzuca wyjątek)
  - ❌ Zapis logu błędów do GenerationErrorLog
  - ❌ Pusta userId
  - ❌ Pusty sourceText

### ChatGptService (`ChatGptServiceTests.cs`)

**Zależności do mockowania:** HttpClient (HttpMessageHandler), IConfiguration, ILogger

Scenariusze testowe:

- **GenerateFlashcardsAsync**
  - ✅ Pomyślne wywołanie OpenAI API
  - ✅ Poprawne parsowanie odpowiedzi JSON
  - ❌ Pusty sourceText (ArgumentException)
  - ❌ Pusty model (ArgumentException)
  - ❌ Brak klucza API (InvalidOperationException)
  - ❌ Błąd HTTP (404, 500)
  - ❌ Nieprawidłowa odpowiedź JSON
  - ✅ Poprawne ustawienie nagłówków Authorization

### OpenRouterService (`OpenRouterServiceTests.cs`)

**Zależności do mockowania:** HttpClient (HttpMessageHandler), IConfiguration, ILogger

Scenariusze testowe (analogiczne do ChatGptService):

- **GenerateFlashcardsAsync**
  - ✅ Pomyślne wywołanie OpenRouter API
  - ✅ Poprawne parsowanie odpowiedzi JSON
  - ❌ Walidacja parametrów wejściowych
  - ❌ Obsługa błędów API
  - ✅ Poprawne ustawienie nagłówków

### ClientAuthenticationService (`ClientAuthenticationServiceTests.cs`)

**Zależności do mockowania:** IClientAuthenticationStateProvider

Scenariusze testowe:

- **LoginAsync** - test zapisywania tokenu
- **LogoutAsync** - test usuwania tokenu
- **GetTokenAsync** - test pobierania tokenu

## 4. Testy walidacji modeli Request

Wykorzystamy `DataAnnotationsValidator` do testowania atrybutów walidacyjnych:

### RegisterRequestValidatorTests

- ✅ Prawidłowe dane
- ❌ Email: pusty, nieprawidłowy format, > 255 znaków
- ❌ Password: < 8 znaków, > 100 znaków, brak uppercase/lowercase/cyfry/znaku specjalnego
- ❌ ConfirmPassword: niezgodne z Password

### CreateFlashcardRequestValidatorTests

- ✅ Prawidłowe dane
- ❌ Front: pusty, > 200 znaków
- ❌ Back: pusty, > 500 znaków

### GenerateFlashcardsRequestValidatorTests

- ✅ Prawidłowe dane
- ❌ SourceText: < 1000 znaków, > 10000 znaków, pusty
- ❌ Model: pusty, > 100 znaków

### UpdateFlashcardRequestValidatorTests

- ✅ Prawidłowe dane
- ❌ Analogiczne walidacje jak CreateFlashcard

## 5. Testy JwtHelper

### JwtHelperTests

- ✅ Ekstraktowanie userId z prawidłowego tokenu
- ❌ Null token (zwraca Guid.Empty)
- ❌ Pusty string (zwraca Guid.Empty)
- ❌ Nieprawidłowy format tokenu (zwraca Guid.Empty)
- ❌ Token bez claim NameIdentifier (zwraca Guid.Empty)
- ❌ Token z nieprawidłowym Guid (zwraca Guid.Empty)
- ✅ Token z claim "sub" zamiast NameIdentifier

## 6. Narzędzia i konfiguracja

### DatabaseFixture.cs

Implementacja `IDisposable` do zarządzania in-memory database:

- Konfiguracja ApplicationDbContext z InMemoryDatabase
- Seedowanie testowych danych
- Czyszczenie po testach
- Wykorzystanie `IClassFixture<DatabaseFixture>` w testach

### Mockowanie HttpClient

Wykorzystanie `HttpMessageHandler` do mockowania odpowiedzi HTTP:

```csharp
var handlerMock = Substitute.For<HttpMessageHandler>();
var httpClient = new HttpClient(handlerMock);
```

### Mockowanie UserManager

UserManager wymaga specjalnej konfiguracji z UserStore, które również mockujemy za pomocą NSubstitute.

## 7. Konwencje nazewnictwa testów

Stosujemy wzorzec: `MethodName_Scenario_ExpectedBehavior`

Przykłady:

- `RegisterUserAsync_ValidRequest_ReturnsSuccessWithToken`
- `RegisterUserAsync_DuplicateEmail_ReturnsFailure`
- `ListFlashcardsAsync_WithSearchFilter_ReturnsFilteredResults`
- `GenerateFlashcardsAsync_EmptySourceText_ThrowsArgumentException`

## 8. Pliki kluczowe do wykorzystania

- `10xCards/Services/AuthService.cs` - implementacja do testowania
- `10xCards/Services/FlashcardService.cs` - logika CRUD
- `10xCards/Models/Common/Result.cs` - wzorzec Result do asercji
- `10xCards/Utilities/JwtHelper.cs` - utility do testowania
- `10xCards/Models/Requests/*.cs` - modele z atrybutami walidacyjnymi

### To-dos

- [ ] Utworzenie projektu testowego 10xCards.Tests z konfiguracją xUnit, NSubstitute, FluentAssertions i pakietami EF InMemory
- [ ] Implementacja DatabaseFixture dla testów z in-memory database
- [ ] Utworzenie JwtHelperTests - testy dla utility class (najprostsze, bez zależności)
- [ ] Utworzenie testów walidacji dla wszystkich modeli Request (RegisterRequest, CreateFlashcardRequest, GenerateFlashcardsRequest, UpdateFlashcardRequest)
- [ ] Utworzenie AuthServiceTests z mockowaniem UserManager, IConfiguration i testami rejestracji/logowania
- [ ] Utworzenie FlashcardServiceTests z in-memory database i testami CRUD
- [ ] Utworzenie GenerationServiceTests z mockowaniem IChatGptService i in-memory database
- [ ] Utworzenie ChatGptServiceTests z mockowaniem HttpMessageHandler
- [ ] Utworzenie OpenRouterServiceTests z mockowaniem HttpMessageHandler
- [ ] Utworzenie ClientAuthenticationServiceTests