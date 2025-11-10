<!-- 2ce3713a-1713-4a00-af52-52edcae7da1b da49ba70-1b36-4ba7-a220-35faabd5f592 -->
# Plan Implementacji Endpointu API: Logout

## 1. Przegląd punktu końcowego

Endpoint `POST /api/auth/logout` umożliwia użytkownikom wylogowanie się z aplikacji poprzez unieważnienie bieżącej sesji. W architekturze opartej na JWT (stateless authentication), logout polega na:

- Zalogowaniu zdarzenia wylogowania dla celów audytowych i bezpieczeństwa
- Zwróceniu odpowiedzi 204 No Content
- Pozostawieniu klientowi odpowiedzialności za usunięcie tokenu z pamięci lokalnej

**Uwaga:** Token JWT pozostaje technicznie ważny do czasu wygaśnięcia (7 dni). Dla zwiększenia bezpieczeństwa w przyszłych wersjach można rozważyć implementację mechanizmu blacklistingu tokenów.

## 2. Szczegóły żądania

- **Metoda HTTP:** POST
- **Struktura URL:** `/api/auth/logout`
- **Parametry:**
                                - **Wymagane:** 
                                                                - Header `Authorization: Bearer {token}` - ważny JWT token
                                - **Opcjonalne:** Brak
- **Request Body:** Brak (pusty)

## 3. Wykorzystywane typy

### Istniejące typy (do wykorzystania):

- `Result<T>` (`10xCards/Models/Common/Result.cs`) - wzorzec wyniku operacji
- `ErrorResponse` (`10xCards/Models/Responses/ErrorResponse.cs`) - format odpowiedzi błędu

### Nowe typy:

Brak nowych typów DTO jest wymaganych. Endpoint zwraca:

- **Sukces:** 204 No Content (bez ciała odpowiedzi)
- **Błąd:** `ErrorResponse` z kodem 401

## 4. Szczegóły odpowiedzi

### Sukces (204 No Content):

```
Status: 204 No Content
Body: (empty)
```

### Błędy:

**401 Unauthorized** - Brak, nieprawidłowy lub wygasły token:

```json
{
  "message": "Unauthorized"
}
```

**500 Internal Server Error** - Nieoczekiwany błąd serwera:

```json
{
  "message": "An error occurred while processing logout",
  "errorCode": "LOGOUT_ERROR"
}
```

## 5. Przepływ danych

```
1. Klient wysyła żądanie POST /api/auth/logout z tokenem Bearer
   ↓
2. ASP.NET Core Middleware weryfikuje token JWT
   ↓
3. [Sukces] Token ważny → Przekazanie do endpointu
   [Błąd] Token nieważny → Zwrot 401 Unauthorized
   ↓
4. Endpoint wywołuje AuthService.LogoutUserAsync(userId)
   ↓
5. AuthService:
   - Pobiera userId z ClaimsPrincipal
   - Loguje zdarzenie wylogowania (userId, timestamp, IP)
   - Zwraca Result<bool>.Success()
   ↓
6. Endpoint zwraca 204 No Content
   ↓
7. Klient usuwa token z localStorage/sessionStorage
```

**Brak interakcji z zewnętrznymi usługami.**

## 6. Względy bezpieczeństwa

### Uwierzytelnianie:

- Endpoint **MUSI** wymagać autoryzacji poprzez `.RequireAuthorization()`
- Token JWT weryfikowany przez middleware ASP.NET Core Authentication
- Wymagane claimy: `ClaimTypes.NameIdentifier` (userId)

### Autoryzacja:

- Użytkownik może wylogować tylko siebie (userId z tokenu)
- Brak dodatkowych wymagań autoryzacyjnych

### Walidacja:

- Token signature validation (automatyczne przez middleware)
- Token expiration check (automatyczne przez middleware)
- Token format validation (automatyczne przez middleware)

### Bezpieczeństwo:

- **HTTPS wymagane w production** - zapobiega przechwytywaniu tokenów
- **Rate limiting** - zalecane dla ochrony przed nadużyciami
- **Logging z IP address** - dla audytu bezpieczeństwa
- **Constant-time response** - zapobiega timing attacks (nie ujawniaj czy token jest ważny na podstawie czasu odpowiedzi)

### OWASP Considerations:

- A01:2021 Broken Access Control - Mitigated przez autoryzację endpointu
- A02:2021 Cryptographic Failures - Wymagane HTTPS w production
- A07:2021 Identification and Authentication Failures - Proper token validation

## 7. Obsługa błędów

### Potencjalne błędy:

| Scenariusz | Kod HTTP | Odpowiedź | Logowanie |

|-----------|----------|-----------|-----------|

| Brak tokenu | 401 | `{ "message": "Unauthorized" }` | Warning: Logout attempt without token |

| Token nieprawidłowy/wygasły | 401 | `{ "message": "Unauthorized" }` | Warning: Logout attempt with invalid token |

| Token prawidłowy | 204 | (empty) | Info: User logged out successfully (userId: {userId}) |

| Błąd serwera | 500 | `{ "message": "An error occurred...", "errorCode": "LOGOUT_ERROR" }` | Error: Logout failed (userId: {userId}), exception details |

### Strategia logowania:

```csharp
// Sukces
_logger.LogInformation(
    "User logged out successfully. UserId: {UserId}, IP: {IpAddress}, Timestamp: {Timestamp}",
    userId, ipAddress, DateTime.UtcNow);

// Błąd autoryzacji (obsługiwane przez middleware, dodatkowy log w endpoint)
_logger.LogWarning(
    "Logout attempt with invalid token. IP: {IpAddress}",
    ipAddress);

// Błąd serwera
_logger.LogError(exception,
    "Logout operation failed. UserId: {UserId}",
    userId);
```

### Error Flow:

- Błędy autentykacji obsługiwane przez middleware (zwrot 401)
- Błędy serwera przechwytywane w try-catch z odpowiednim logowaniem
- Wszystkie błędy zwracają ProblemDetails lub ErrorResponse zgodnie z RFC 7807

## 8. Rozważania dotyczące wydajności

### Optymalizacje:

- **Minimalny overhead** - endpoint nie wykonuje operacji bazodanowych
- **Szybkie logowanie** - użycie structured logging (ILogger) z niskim kosztem
- **No database lookups** - userId pobierany bezpośrednio z tokenu (claims)
- **Async operation** - nawet przy minimalnej logice, zachowanie async pattern

### Potencjalne wąskie gardła:

Brak istotnych wąskich gardeł dla MVP. Operacja jest bardzo lekka.

### Przyszłe optymalizacje (jeśli implementacja blacklistingu):

- **Redis cache** dla blacklisty tokenów (szybki lookup)
- **TTL na blacklist entries** - automatyczne usuwanie wygasłych tokenów
- **Batch cleanup** - okresowe czyszczenie wygasłych wpisów z cache

### Monitoring:

- Śledzenie liczby wylogowań na użytkownika (wykrywanie anomalii)
- Monitorowanie wzorców wylogowań (bezpieczeństwo)

## 9. Etapy wdrożenia

### Krok 1: Rozszerzenie interfejsu IAuthService

**Plik:** `10xCards/Services/IAuthService.cs`

Dodać deklarację metody:

```csharp
/// <summary>
/// Logs out the current user by invalidating their session
/// </summary>
/// <param name="userId">ID of the user to log out</param>
/// <param name="ipAddress">IP address of the logout request for audit logging</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Result indicating success or failure</returns>
Task<Result<bool>> LogoutUserAsync(
    Guid userId, 
    string? ipAddress, 
    CancellationToken cancellationToken = default);
```

### Krok 2: Implementacja w AuthService

**Plik:** `10xCards/Services/AuthService.cs`

Dodać metodę:

```csharp
public async Task<Result<bool>> LogoutUserAsync(
    Guid userId, 
    string? ipAddress,
    CancellationToken cancellationToken = default) {
    
    try {
        // Log successful logout for audit trail
        _logger.LogInformation(
            "User logged out successfully. UserId: {UserId}, IP: {IpAddress}, Timestamp: {Timestamp}",
            userId, 
            ipAddress ?? "Unknown", 
            DateTime.UtcNow);

        // For stateless JWT, logout is primarily client-side
        // Token remains valid until expiration
        // Future enhancement: implement token blacklist
        
        return await Task.FromResult(Result<bool>.Success(true));
    }
    catch (Exception ex) {
        _logger.LogError(ex, 
            "Logout operation failed. UserId: {UserId}", 
            userId);
        
        return Result<bool>.Failure("An error occurred while processing logout");
    }
}
```

**Uwagi:**

- Metoda jest oznaczona jako async dla zachowania spójności z interfejsem
- `await Task.FromResult()` umożliwia przyszłe dodanie operacji async (np. blacklist)
- Logowanie zawiera userId, IP i timestamp dla audytu bezpieczeństwa
- Try-catch zapewnia graceful error handling

### Krok 3: Dodanie endpointu w AuthEndpoints

**Plik:** `10xCards/Endpoints/AuthEndpoints.cs`

W metodzie `MapAuthEndpoints`, po istniejących endpointach, dodać:

```csharp
// POST /api/auth/logout
group.MapPost("/logout", async (
    HttpContext httpContext,
    IAuthService authService,
    CancellationToken cancellationToken) => {

    // Extract userId from authenticated user claims
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
        return Results.Json(
            new ErrorResponse { Message = "Unauthorized" },
            statusCode: StatusCodes.Status401Unauthorized
        );
    }

    // Extract IP address for audit logging
    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

    var result = await authService.LogoutUserAsync(userId, ipAddress, cancellationToken);

    if (!result.IsSuccess) {
        return Results.Json(
            new ErrorResponse { 
                Message = result.ErrorMessage ?? "An error occurred while processing logout",
                ErrorCode = "LOGOUT_ERROR"
            },
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    return Results.NoContent();
})
.RequireAuthorization()
.WithName("Logout")
.WithSummary("Logout current user")
.WithDescription("Invalidates the current user session and logs the logout event for security auditing")
.Produces(StatusCodes.Status204NoContent)
.Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
.Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
```

**Uwagi:**

- `.RequireAuthorization()` - **KRYTYCZNE** - wymusza uwierzytelnienie
- `HttpContext` injected dla dostępu do User.Claims i RemoteIpAddress
- userId pobierany z ClaimTypes.NameIdentifier
- IP address przekazywany do service dla logowania audytowego
- Zwraca 204 No Content na sukces (zgodnie ze specyfikacją)
- Proper error handling z odpowiednimi kodami statusu

### Krok 4: Testowanie

#### Testy manualne:

1. **Test sukcesu:**
   ```bash
   curl -X POST https://localhost:5001/api/auth/logout \
     -H "Authorization: Bearer {valid_token}"
   # Oczekiwane: 204 No Content
   ```

2. **Test bez tokenu:**
   ```bash
   curl -X POST https://localhost:5001/api/auth/logout
   # Oczekiwane: 401 Unauthorized
   ```

3. **Test z nieprawidłowym tokenem:**
   ```bash
   curl -X POST https://localhost:5001/api/auth/logout \
     -H "Authorization: Bearer invalid_token"
   # Oczekiwane: 401 Unauthorized
   ```


#### Weryfikacja logów:

- Sprawdzić logi aplikacji dla wpisu "User logged out successfully"
- Zweryfikować obecność userId, IP address, timestamp

#### Edge cases:

- Token wygasły (powinien zwrócić 401)
- Wielokrotne wywołania logout z tym samym tokenem (każde powinno zwrócić 204)
- Concurrent logout requests (powinny działać poprawnie)

### Krok 5: Dokumentacja

#### Swagger/OpenAPI:

Endpoint automatycznie pojawi się w Swagger UI dzięki `.WithName()`, `.WithSummary()` i `.WithDescription()`.

#### Dokumentacja dla frontend:

- **Endpoint:** POST /api/auth/logout
- **Headers:** `Authorization: Bearer {token}`
- **Response:** 204 No Content (sukces), 401 Unauthorized (błąd)
- **Akcja klienta po sukcesie:** Usunąć token z localStorage/sessionStorage

### Krok 6: Przyszłe ulepszenia (opcjonalne)

Dla zwiększenia bezpieczeństwa, rozważyć implementację:

1. **Token Blacklist:**

                                                - Nowa tabela: `token_blacklist` (token_jti, user_id, expires_at)
                                                - Redis cache dla szybkiego lookup
                                                - Middleware sprawdzający blacklist przy każdym request

2. **Refresh Tokens:**

                                                - Krótkoterminowe access tokens (15 min)
                                                - Długoterminowe refresh tokens (7 dni)
                                                - Możliwość unieważnienia refresh tokens

3. **Session Management:**

                                                - Śledzenie aktywnych sesji użytkownika
                                                - "Logout from all devices" functionality
                                                - Lista aktywnych sesji w profilu użytkownika

## Podsumowanie

Implementacja endpointu logout jest stosunkowo prosta dla architektury opartej na JWT. Kluczowe aspekty:

- ✅ Wymaga autoryzacji (.RequireAuthorization())
- ✅ Loguje zdarzenia dla audytu bezpieczeństwa
- ✅ Zwraca 204 No Content zgodnie ze specyfikacją
- ✅ Proper error handling z odpowiednimi kodami statusu
- ✅ Follows existing codebase patterns (AuthService, minimal APIs)
- ✅ Gotowe do przyszłych ulepszeń (token blacklist)

Czas implementacji: ~30-45 minut

### To-dos

- [ ] Rozszerzyć IAuthService o metodę LogoutUserAsync
- [ ] Zaimplementować LogoutUserAsync w AuthService z logowaniem audytowym
- [ ] Dodać endpoint POST /api/auth/logout w AuthEndpoints.cs z autoryzacją
- [ ] Przetestować endpoint (sukces, błędy, edge cases)