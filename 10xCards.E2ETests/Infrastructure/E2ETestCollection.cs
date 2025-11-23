namespace _10xCards.E2ETests.Infrastructure;

/// <summary>
/// Defines the "E2E Tests" collection for xUnit
/// All test classes marked with [Collection("E2E Tests")] will share the same E2ETestCollectionFixture instance
/// This ensures tests run sequentially and share the same infrastructure (database, server, browser)
/// </summary>
[CollectionDefinition("E2E Tests")]
public class E2ETestCollection : ICollectionFixture<E2ETestCollectionFixture>
{
    // This class is never instantiated - it's just a marker for xUnit
    // xUnit uses it to understand that all tests in the "E2E Tests" collection
    // should share a single E2ETestCollectionFixture instance
}

