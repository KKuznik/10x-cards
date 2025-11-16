<!-- e223da09-c216-4e00-ac83-76aea51bbf44 aee99cad-15e1-4edd-9625-1789bdd3b972 -->
# Plan naprawy testów

## Problem 1: ClientAuthenticationServiceTests (10 testów)

**Przyczyna**: `ClientAuthenticationStateProvider` jest klasą `sealed`, więc NSubstitute nie może jej mockować.

**Rozwiązanie**: Usunąć słowo kluczowe `sealed` z klasy `ClientAuthenticationStateProvider` w pliku `10xCards/Services/ClientAuthenticationStateProvider.cs`.

## Problem 2: FlashcardService - komunikaty błędów (4 testy)

**Testy**:

- `UpdateFlashcardAsync_NonExistentFlashcard_ReturnsFailure`
- `UpdateFlashcardAsync_OtherUserFlashcard_ReturnsFailure`
- `DeleteFlashcardAsync_NonExistentFlashcard_ReturnsFailure`
- `DeleteFlashcardAsync_OtherUserFlashcard_ReturnsFailure`

**Przyczyna**: Testy oczekują komunikatu "Flashcard not found", ale implementacja zwraca "Flashcard not found or does not belong to user".

**Rozwiązanie**: Zaktualizować testy, aby oczekiwały poprawnego komunikatu.

## Problem 3: CreateFlashcardsBatchAsync - walidacja (2 testy)

**Testy**:

- `CreateFlashcardsBatchAsync_EmptyList_ReturnsFailure`
- `CreateFlashcardsBatchAsync_ValidRequest_CreatesMultipleFlashcards`

**Przyczyna 1**: Brak walidacji pustej listy flashcards w `FlashcardService.CreateFlashcardsBatchAsync`.
**Przyczyna 2**: Test oczekuje źródła "ai-generated", ale `BatchFlashcardItem` nie ma ustawionego domyślnego źródła.

**Rozwiązanie**:

1. Dodać walidację w `FlashcardService.CreateFlashcardsBatchAsync` sprawdzającą czy lista flashcards nie jest pusta
2. Sprawdzić model `BatchFlashcardItem` i upewnić się, że Source jest poprawnie ustawiane

## Problem 4: UpdateFlashcardAsync - UpdatedAt nie aktualizuje się (1 test)

**Test**: `UpdateFlashcardAsync_ExistingFlashcard_UpdatesSuccessfully`

**Przyczyna**: Komentarz w kodzie mówi "UpdatedAt is automatically updated by database trigger", ale trigger może nie działać lub nie istnieć.

**Rozwiązanie**: Ręcznie ustawić `flashcard.UpdatedAt = DateTime.UtcNow` w metodzie `UpdateFlashcardAsync`.

## Problem 5: ListFlashcardsAsync - wyszukiwanie (1 test)

**Test**: `ListFlashcardsAsync_WithSearchFilter_ReturnsMatchingResults`

**Przyczyna**: Wyszukiwanie używa `EF.Functions.ILike`, które może nie działać poprawnie w testach z SQLite.

**Rozwiązanie**: Sprawdzić dane testowe i upewnić się, że wyszukiwanie działa poprawnie. Może być problem z case-sensitivity lub brakiem danych.

## Problem 6: GenerationService - GenerationDuration (1 test)

**Test**: `GenerateFlashcardsAsync_SuccessfulGeneration_SavesGenerationAndReturnsFlashcards`

**Przyczyna**: Stopwatch może nie mierzyć czasu poprawnie w testach, ponieważ mock zwraca dane natychmiast.

**Rozwiązanie**: Test powinien sprawdzać `BeGreaterThanOrEqualTo(0)` zamiast `BeGreaterThan(0)`, lub dodać małe opóźnienie w teście.

## Pliki do modyfikacji:

1. `10xCards/Services/ClientAuthenticationStateProvider.cs` - usunąć `sealed`
2. `10xCards/Services/FlashcardService.cs` - dodać walidację pustej listy i ustawić UpdatedAt
3. `10xCards.Tests/Services/FlashcardServiceTests.cs` - zaktualizować oczekiwane komunikaty błędów
4. `10xCards.Tests/Services/GenerationServiceTests.cs` - zaktualizować asercję dla GenerationDuration
5. Sprawdzić `BatchFlashcardItem` model i dane testowe

### To-dos

- [ ] Usunąć słowo kluczowe sealed z ClientAuthenticationStateProvider
- [ ] Dodać walidację pustej listy w CreateFlashcardsBatchAsync
- [ ] Ręcznie ustawić UpdatedAt w UpdateFlashcardAsync
- [ ] Zaktualizować oczekiwane komunikaty błędów w testach FlashcardService
- [ ] Sprawdzić i naprawić ustawianie Source w BatchFlashcardItem
- [ ] Naprawić test wyszukiwania flashcards
- [ ] Zaktualizować asercję dla GenerationDuration w teście
- [ ] Uruchomić wszystkie testy i zweryfikować poprawki