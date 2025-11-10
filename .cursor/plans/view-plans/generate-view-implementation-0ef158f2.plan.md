<!-- 0ef158f2-8eb3-4c5e-9d06-098e9d085e5c 1b139a5f-a351-4deb-a761-36f9a09214b6 -->
# Plan Implementacji Widoku Generowania Fiszek

## 1. Przegląd

Widok `/generate` umożliwia użytkownikowi:

- Wklejenie tekstu źródłowego (1000-10000 znaków)
- Wygenerowanie propozycji fiszek przy użyciu AI
- Przegląd wygenerowanych fiszek
- Akceptację, edycję lub odrzucenie każdej fiszki
- Zapisanie wybranych fiszek do bazy danych

## 2. Routing widoku

- **Ścieżka:** `/generate`
- **Plik:** `10xCards/Components/Pages/Generate.razor`
- **Dyrektywa:** `@page "/generate"`
- **Wymagana autoryzacja:** Tak (użytkownik musi być zalogowany)

## 3. Struktura komponentów

```
Generate.razor (główna strona)
├── GenerationForm (formularz wprowadzania tekstu i generowania)
│   ├── textarea (pole tekstowe dla źródłowego tekstu)
│   ├── character counter (licznik znaków)
│   └── button (przycisk "Generuj fiszki")
├── LoadingSkeleton (wskaźnik ładowania podczas generowania)
└── FlashcardsList (lista wygenerowanych propozycji)
    ├── FlashcardItem (pojedyncza propozycja fiszki)
    │   ├── FlashcardContent (przód/tył fiszki)
    │   └── FlashcardActions (przyciski: akceptuj/edytuj/odrzuć)
    └── BulkActions (akcje zbiorcze: zapisz wszystkie/zapisz zaakceptowane)
```

## 4. Szczegóły komponentów

### 4.1 Generate.razor (Główny komponent strony)

**Opis:** Główny komponent orkiestrujący cały proces generowania i zatwierdzania fiszek.

**Elementy HTML:**

- `<div class="container">` - główny kontener Bootstrap
- Tytuł strony `<h1>`
- Alert dla komunikatów o błędach/sukcesach
- Sekcja formularza generowania
- Sekcja listy wygenerowanych fiszek (conditional rendering)

**Komponenty dzieci:**

- Formularz wprowadzania tekstu (inline w tym komponencie)
- Lista fiszek (conditional rendering po wygenerowaniu)

**Obsługiwane zdarzenia:**

- `OnGenerateClick` - wywołanie API generowania
- `OnAcceptFlashcard` - zaznaczenie fiszki jako zaakceptowanej
- `OnEditFlashcard` - edycja treści fiszki
- `OnRejectFlashcard` - usunięcie fiszki z listy propozycji
- `OnSaveAll` - zapisanie wszystkich fiszek
- `OnSaveAccepted` - zapisanie tylko zaakceptowanych fiszek

**Warunki walidacji:**

- Długość tekstu źródłowego: 1000-10000 znaków
- Co najmniej jedna fiszka zaakceptowana przed zapisaniem
- Model AI musi być wybrany (domyślnie: "openai/gpt-4o-mini")

**Typy:**

- `GenerateFlashcardsRequest` - request do API
- `GenerationResponse` - response z API
- `FlashcardViewModel` - lokalny ViewModel dla fiszek
- `CreateFlashcardsBatchRequest` - request zapisu fiszek
- `GenerationState` - enum stanu generowania

**Stan komponentu:**

- `sourceText` (string) - tekst źródłowy
- `model` (string) - wybrany model AI
- `generationState` (GenerationState) - stan procesu (Idle/Generating/Generated/Saving/Saved)
- `generationResponse` (GenerationResponse?) - odpowiedź z API
- `flashcardViewModels` (List<FlashcardViewModel>) - lista fiszek z lokalnymi zmianami
- `errorMessage` (string?) - komunikat błędu
- `successMessage` (string?) - komunikat sukcesu

### 4.2 FlashcardItem (Komponent pojedynczej fiszki)

**Opis:** Wyświetla pojedynczą fiszkę z możliwością edycji i zmiany statusu (accept/reject).

**Elementy HTML:**

- `<div class="card mb-3">` - karta Bootstrap
- `<div class="card-body">` - treść karty
- Pola edycyjne `<textarea>` dla przodu i tyłu (conditional)
- Przyciski akcji (`<button>`)

**Propsy:**

- `Flashcard` (FlashcardViewModel) - dane fiszki
- `OnStatusChange` (EventCallback<FlashcardStatusChangeArgs>) - zmiana statusu
- `OnEdit` (EventCallback<FlashcardEditArgs>) - edycja treści

**Obsługiwane zdarzenia:**

- Click na "Akceptuj" - zmienia status na Accepted
- Click na "Edytuj" - przełącza tryb edycji
- Click na "Zapisz zmiany" - potwierdza edycję i zmienia source na "ai-edited"
- Click na "Odrzuć" - zmienia status na Rejected

**Warunki walidacji:**

- Front: max 200 znaków, required
- Back: max 500 znaków, required
- W trybie edycji walidacja inline przy zapisie

### 4.3 BulkActions (Akcje zbiorcze)

**Opis:** Przyciski do zapisywania fiszek zbiorczego.

**Elementy HTML:**

- `<div class="d-flex gap-2 mt-3">` - kontener z przyciskami
- `<button class="btn btn-primary">` - Zapisz zaakceptowane
- `<button class="btn btn-secondary">` - Zapisz wszystkie
- Licznik zaakceptowanych fiszek

**Propsy:**

- `TotalCount` (int) - liczba wszystkich fiszek
- `AcceptedCount` (int) - liczba zaakceptowanych
- `OnSaveAccepted` (EventCallback) - zapisz zaakceptowane
- `OnSaveAll` (EventCallback) - zapisz wszystkie
- `IsSaving` (bool) - czy trwa zapisywanie

**Warunki walidacji:**

- Przyciski disabled gdy IsSaving = true
- "Zapisz zaakceptowane" disabled gdy AcceptedCount = 0

## 5. Typy i modele

### 5.1 Istniejące typy (z API)

```csharp
// Request dla generowania
GenerateFlashcardsRequest {
    string SourceText (1000-10000 chars)
    string Model (required)
}

// Response z generowania
GenerationResponse {
    long Id
    Guid UserId
    string Model
    int GeneratedCount
    int? AcceptedUneditedCount
    int? AcceptedEditedCount
    string SourceTextHash
    int SourceTextLength
    int GenerationDuration
    DateTime CreatedAt
    DateTime UpdatedAt
    List<ProposedFlashcardDto> Flashcards
}

// Propozycja fiszki (niezapisana)
ProposedFlashcardDto {
    string Front
    string Back
}

// Request zapisu fiszek
CreateFlashcardsBatchRequest {
    long GenerationId
    List<BatchFlashcardItem> Flashcards (1-50)
}

// Item w batch
BatchFlashcardItem {
    string Front (1-200 chars)
    string Back (1-500 chars)
    string Source ("ai-full" | "ai-edited")
}

// Response zapisu
CreateFlashcardsBatchResponse {
    int Created
    List<FlashcardResponse> Flashcards
}
```

### 5.2 Nowe ViewModels (do utworzenia)

```csharp
// Plik: 10xCards/Models/ViewModels/FlashcardViewModel.cs
public class FlashcardViewModel {
    public string OriginalFront { get; set; } // Oryginalny tekst z AI
    public string OriginalBack { get; set; }
    public string Front { get; set; } // Aktualny tekst (może być edytowany)
    public string Back { get; set; }
    public FlashcardStatus Status { get; set; } // Accepted/Pending/Rejected
    public bool IsEdited { get; set; } // Czy użytkownik edytował
    public bool IsInEditMode { get; set; } // Czy w trybie edycji
    public string Source => IsEdited ? "ai-edited" : "ai-full";
}

// Enum statusu fiszki
public enum FlashcardStatus {
    Pending,   // Domyślny - czeka na decyzję
    Accepted,  // Zaakceptowana
    Rejected   // Odrzucona
}

// Enum stanu generowania
public enum GenerationState {
    Idle,       // Początkowy stan
    Generating, // Trwa generowanie
    Generated,  // Wygenerowano propozycje
    Saving,     // Trwa zapisywanie
    Saved,      // Zapisano
    Error       // Błąd
}

// Args dla zdarzeń
public class FlashcardStatusChangeArgs {
    public int Index { get; set; }
    public FlashcardStatus NewStatus { get; set; }
}

public class FlashcardEditArgs {
    public int Index { get; set; }
    public string Front { get; set; }
    public string Back { get; set; }
}
```

## 6. Zarządzanie stanem

### Stan w komponencie Generate.razor

**Zmienne stanu (kod C# w @code block):**

```csharp
// Formularz
private string sourceText = string.Empty;
private string selectedModel = "openai/gpt-4o-mini";

// Stan generowania
private GenerationState state = GenerationState.Idle;
private long? currentGenerationId = null;

// Propozycje fiszek
private List<FlashcardViewModel> flashcards = new();

// Komunikaty
private string? errorMessage = null;
private string? successMessage = null;

// Walidacja
private bool IsSourceTextValid => 
    sourceText.Length >= 1000 && sourceText.Length <= 10000;

private int AcceptedCount => 
    flashcards.Count(f => f.Status == FlashcardStatus.Accepted);
```

**Metody zarządzania stanem:**

- `ClearMessages()` - czyści komunikaty błędów/sukcesu
- `ResetState()` - resetuje stan do Idle
- `UpdateFlashcardStatus(int index, FlashcardStatus status)` - aktualizuje status fiszki
- `UpdateFlashcardContent(int index, string front, string back)` - aktualizuje treść po edycji

**Nie jest wymagany custom hook** - zarządzanie stanem odbywa się bezpośrednio w komponencie Blazor.

## 7. Integracja API

### 7.1 Generowanie fiszek

**Endpoint:** `POST /api/generations`

**Typ żądania:** `GenerateFlashcardsRequest`

```csharp
new GenerateFlashcardsRequest {
    SourceText = sourceText,
    Model = selectedModel
}
```

**Typ odpowiedzi:** `GenerationResponse`

**Obsługa:**

```csharp
private async Task GenerateFlashcardsAsync() {
    ClearMessages();
    
    if (!IsSourceTextValid) {
        errorMessage = "Tekst musi zawierać od 1000 do 10000 znaków";
        return;
    }
    
    state = GenerationState.Generating;
    
    try {
        var request = new GenerateFlashcardsRequest {
            SourceText = sourceText,
            Model = selectedModel
        };
        
        var response = await Http.PostAsJsonAsync("/api/generations", request);
        
        if (response.IsSuccessStatusCode) {
            var result = await response.Content.ReadFromJsonAsync<GenerationResponse>();
            currentGenerationId = result.Id;
            
            // Mapowanie na ViewModels
            flashcards = result.Flashcards.Select(f => new FlashcardViewModel {
                OriginalFront = f.Front,
                OriginalBack = f.Back,
                Front = f.Front,
                Back = f.Back,
                Status = FlashcardStatus.Pending,
                IsEdited = false,
                IsInEditMode = false
            }).ToList();
            
            state = GenerationState.Generated;
        } else {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            errorMessage = error?.Message ?? "Błąd podczas generowania fiszek";
            state = GenerationState.Error;
        }
    } catch (Exception ex) {
        errorMessage = "Wystąpił błąd podczas komunikacji z serwerem";
        state = GenerationState.Error;
    }
}
```

### 7.2 Zapisywanie fiszek

**Endpoint:** `POST /api/flashcards/batch`

**Typ żądania:** `CreateFlashcardsBatchRequest`

```csharp
new CreateFlashcardsBatchRequest {
    GenerationId = currentGenerationId.Value,
    Flashcards = selectedFlashcards
}
```

**Typ odpowiedzi:** `CreateFlashcardsBatchResponse`

**Obsługa:**

```csharp
private async Task SaveAcceptedFlashcardsAsync() {
    if (!currentGenerationId.HasValue) return;
    
    var acceptedFlashcards = flashcards
        .Where(f => f.Status == FlashcardStatus.Accepted)
        .Select(f => new BatchFlashcardItem {
            Front = f.Front,
            Back = f.Back,
            Source = f.Source
        })
        .ToList();
    
    if (acceptedFlashcards.Count == 0) {
        errorMessage = "Nie zaakceptowano żadnych fiszek";
        return;
    }
    
    state = GenerationState.Saving;
    
    try {
        var request = new CreateFlashcardsBatchRequest {
            GenerationId = currentGenerationId.Value,
            Flashcards = acceptedFlashcards
        };
        
        var response = await Http.PostAsJsonAsync("/api/flashcards/batch", request);
        
        if (response.IsSuccessStatusCode) {
            var result = await response.Content.ReadFromJsonAsync<CreateFlashcardsBatchResponse>();
            successMessage = $"Pomyślnie zapisano {result.Created} fiszek";
            state = GenerationState.Saved;
            
            // Po 3 sekundach reset
            await Task.Delay(3000);
            ResetState();
        } else {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            errorMessage = error?.Message ?? "Błąd podczas zapisywania fiszek";
            state = GenerationState.Error;
        }
    } catch (Exception ex) {
        errorMessage = "Wystąpił błąd podczas zapisywania fiszek";
        state = GenerationState.Error;
    }
}
```

**Autoryzacja:**

- Token JWT dodawany automatycznie przez `HttpClient` (konfiguracja w `Program.cs`)
- Jeśli brak tokenu lub wygasł → przekierowanie na `/login`

## 8. Interakcje użytkownika

### 8.1 Wprowadzanie tekstu

1. Użytkownik wpisuje/wkleja tekst do textarea
2. Licznik znaków aktualizuje się na bieżąco
3. Kolor licznika zmienia się:

   - Czerwony: < 1000 lub > 10000 znaków
   - Zielony: 1000-10000 znaków

4. Przycisk "Generuj" jest disabled gdy tekst niewłaściwy

### 8.2 Generowanie

1. Click "Generuj fiszki"
2. Przycisk zmienia się na spinner + "Generuję..."
3. Wyświetla się skeleton loader (3-5 placeholder kart)
4. Po otrzymaniu odpowiedzi:

   - Skeleton znika
   - Pojawia się lista fiszek
   - Scroll do listy fiszek

### 8.3 Przeglądanie propozycji

1. Każda fiszka wyświetlana w karcie Bootstrap
2. Domyślny status: Pending (żółta ramka)
3. Hover nad kartą - subtle shadow effect
4. Widoczne przyciski akcji

### 8.4 Akceptacja fiszki

1. Click "Akceptuj" (ikona checkmark)
2. Karta zmienia kolor na zielony (border)
3. Status zmienia się na Accepted
4. Licznik zaakceptowanych aktualizuje się

### 8.5 Edycja fiszki

1. Click "Edytuj" (ikona pencil)
2. Front i Back zamieniają się na textarea
3. Pojawia się "Zapisz zmiany" i "Anuluj"
4. Po zapisie:

   - Tryb edycji wyłączony
   - IsEdited = true
   - Status automatycznie Accepted
   - Karta zielona (border)

### 8.6 Odrzucenie fiszki

1. Click "Odrzuć" (ikona X)
2. Karta zmienia kolor na czerwony (border) lub znika (fade out animation)
3. Status = Rejected
4. Fiszka nie będzie zapisana

### 8.7 Zapisywanie

1. Click "Zapisz zaakceptowane" lub "Zapisz wszystkie"
2. Przyciski disabled
3. Spinner w przycisku
4. Po zapisie:

   - Alert sukcesu z liczbą zapisanych
   - Po 3 sekundach: czyszczenie formularza i lista

## 9. Warunki i walidacja

### 9.1 Walidacja formularza generowania

**Komponent:** Generate.razor - formularz

**Warunki:**

- `sourceText.Length >= 1000` AND `sourceText.Length <= 10000`

**Wpływ na UI:**

- Przycisk "Generuj" disabled gdy warunek niespełniony
- Komunikat pod textarea: "Wprowadź od 1000 do 10000 znaków (obecnie: {count})"
- Kolor komunikatu: czerwony gdy niespełniony, zielony gdy OK

### 9.2 Walidacja edycji fiszki

**Komponent:** FlashcardItem - tryb edycji

**Warunki:**

- `Front.Length >= 1` AND `Front.Length <= 200`
- `Back.Length >= 1` AND `Back.Length <= 500`

**Wpływ na UI:**

- Przycisk "Zapisz zmiany" disabled gdy warunki niespełnione
- Inline validation message pod polem
- Border czerwony przy nieprawidłowym polu

### 9.3 Walidacja zapisywania

**Komponent:** BulkActions

**Warunki:**

- Co najmniej jedna fiszka z statusem Accepted
- `currentGenerationId` nie null

**Wpływ na UI:**

- Przycisk "Zapisz zaakceptowane" disabled gdy AcceptedCount = 0
- Tooltip wyjaśniający dlaczego disabled

## 10. Obsługa błędów

### 10.1 Błędy walidacji (400)

**Scenariusz:** Nieprawidłowe dane w request

**Obsługa:**

- Parsowanie `errors` z response
- Wyświetlenie listy błędów w alert danger
- Czerwony border przy błędnych polach

### 10.2 Błąd autoryzacji (401)

**Scenariusz:** Token wygasł lub brak tokenu

**Obsługa:**

- Przekierowanie na `/login`
- Query parameter: `returnUrl=/generate`

### 10.3 Błąd API AI (500)

**Scenariusz:** Problem z OpenRouter API

**Obsługa:**

- Alert danger: "Nie udało się wygenerować fiszek. Spróbuj ponownie."
- Przycisk "Spróbuj ponownie"
- Stan wraca do Idle

### 10.4 Błąd sieciowy

**Scenariusz:** Brak połączenia

**Obsługa:**

- Alert warning: "Sprawdź połączenie internetowe"
- Przycisk "Spróbuj ponownie"

### 10.5 Not Found (404)

**Scenariusz:** GenerationId nie istnieje przy zapisie

**Obsługa:**

- Alert danger: "Sesja generowania wygasła"
- Przycisk reset formularza

## 11. Kroki implementacji

### Krok 1: Utworzenie ViewModels

- Plik: `10xCards/Models/ViewModels/FlashcardViewModel.cs`
- Definicje: `FlashcardViewModel`, `FlashcardStatus`, `GenerationState`
- Definicje Args dla EventCallback

### Krok 2: Implementacja głównego komponentu Generate.razor

- Struktura HTML z Bootstrap
- @code block ze zmiennymi stanu
- Metoda `GenerateFlashcardsAsync()`
- Formularz z textarea i walidacją
- Licznik znaków

### Krok 3: Implementacja listy fiszek (inline w Generate.razor)

- Conditional rendering po wygenerowaniu
- @foreach przez flashcards
- Wyświetlenie każdej fiszki jako Bootstrap card

### Krok 4: Dodanie interakcji dla pojedynczej fiszki

- Przyciski akceptuj/edytuj/odrzuć
- Tryb edycji (textarea dla Front/Back)
- Metody: `AcceptFlashcard()`, `EditFlashcard()`, `RejectFlashcard()`
- Aktualizacja statusu w state

### Krok 5: Implementacja akcji zbiorczych

- Sekcja BulkActions
- Przycisk "Zapisz zaakceptowane"
- Przycisk "Zapisz wszystkie"
- Metoda `SaveAcceptedFlashcardsAsync()`
- Metoda `SaveAllFlashcardsAsync()`

### Krok 6: Integracja z API zapisywania

- HTTP POST do `/api/flashcards/batch`
- Mapowanie ViewModels na BatchFlashcardItem
- Obsługa response i błędów

### Krok 7: Styling i UX

- Bootstrap classes dla responsywności
- Custom CSS dla statusów (zielony/czerwony/żółty border)
- Animacje fade-in/fade-out
- Loading spinners
- Skeleton placeholders

### Krok 8: Walidacja i obsługa błędów

- Inline validation messages
- Alert components dla błędów globalnych
- Toast notifications dla sukcesu
- Disabled states dla przycisków

### Krok 9: Autoryzacja

- Sprawdzenie czy użytkownik zalogowany
- Redirect na login jeśli nie
- Dodanie [Authorize] attribute jeśli potrzebne

### Krok 10: Testowanie manualne

- Happy path: generuj → edytuj → zapisz
- Edge cases: tekst za krótki/długi
- Błędy API
- Brak połączenia
- Responsywność (mobile/desktop)

### Krok 11: Refaktoryzacja (opcjonalnie)

- Wydzielenie FlashcardItem do osobnego komponentu .razor
- Wydzielenie BulkActions do osobnego komponentu
- Service dla API calls (FlashcardApiService)

### To-dos

- [ ] Utworzyć ViewModels: FlashcardViewModel, FlashcardStatus, GenerationState i EventArgs
- [ ] Zaimplementować formularz generowania z walidacją i licznikiem znaków
- [ ] Zintegrować wywołanie API POST /api/generations z obsługą błędów
- [ ] Zaimplementować listę wygenerowanych fiszek z kartami Bootstrap
- [ ] Dodać interakcje dla pojedynczej fiszki (akceptuj/edytuj/odrzuć)
- [ ] Zaimplementować akcje zbiorcze (zapisz zaakceptowane/wszystkie)
- [ ] Zintegrować wywołanie API POST /api/flashcards/batch
- [ ] Dodać styling, animacje i loading states (skeleton, spinners)
- [ ] Zaimplementować kompleksową obsługę błędów i komunikaty użytkownika
- [ ] Przeprowadzić testowanie manualne wszystkich scenariuszy użycia