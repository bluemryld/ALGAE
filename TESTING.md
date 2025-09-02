# Testing Documentation

This document describes the testing approach, patterns, and usage for ALGAE.

## Testing Overview

ALGAE uses a comprehensive testing strategy with xUnit, Moq, and AutoMocker to ensure code quality and reliability.

### Test Coverage Status

✅ **Current Coverage (21 passing, 3 failing out of 24 tests)**
- **GameLaunchService**: Validation logic, file system checks, launch scenarios
- **GamesViewModel**: Data loading, search/filter, launch commands  
- **Test Infrastructure**: Data builders, mocking patterns

⚠️ **Failing Tests (3)**
- Need investigation and fixes for 3 currently failing unit tests

❌ **Missing Coverage**
- GameDetailViewModel, LauncherViewModel, other ViewModels
- Repository integration tests
- CompanionLaunchService, NotificationService
- End-to-end integration tests

## Test Project Structure

```
ALGAE.Tests/
├── Services/                    # Service layer tests
│   └── GameLaunchServiceTests.cs
├── ViewModels/                  # ViewModel unit tests  
│   └── GamesViewModelTests.cs
├── TestData/                    # Test data builders
│   └── GameTestDataBuilder.cs
└── MainWindowViewModelTests.cs  # Legacy test example
```

## Testing Patterns

### Test Data Builders

Use fluent builders for consistent test data creation:

```csharp
// Create test games easily
var game = new GameTestDataBuilder()
    .WithName("Test Game")
    .WithId(1) 
    .WithInstallPath(@"C:\Games\TestGame")
    .WithExecutableName("game.exe")
    .Build();

// Or test invalid scenarios
var invalidGame = new GameTestDataBuilder()
    .WithInvalidExecutable()
    .WithInvalidPath()
    .Build();
```

**Benefits:**
- Consistent test data across all tests
- Easy to modify game properties for specific test scenarios
- Readable and maintainable test setup

### Mocking with AutoMocker

AutoMocker automatically creates mocks for all dependencies:

```csharp
[Fact]
public async Task LaunchGameAsync_WithValidGame_LaunchesSuccessfully()
{
    // Arrange - AutoMocker creates all dependencies
    var mocker = new AutoMocker();
    var game = new GameTestDataBuilder().Build();
    
    // Setup specific mock behaviors
    mocker.GetMock<IGameLaunchService>()
        .Setup(x => x.ValidateGameAsync(It.IsAny<Game>()))
        .ReturnsAsync(GameValidationResult.Success());
        
    var viewModel = mocker.CreateInstance<GamesViewModel>();

    // Act
    await viewModel.LaunchGameWithProfileAsync(game, null);

    // Assert - Verify service interactions
    mocker.GetMock<IGameLaunchService>()
        .Verify(x => x.LaunchGameAsync(It.IsAny<Game>()), Times.Once);
}
```

**Benefits:**
- Automatic dependency injection for tests
- Easy to verify service interactions
- Isolates unit under test from dependencies

### Arrange-Act-Assert Pattern

All tests follow the standard AAA pattern:

```csharp
[Fact] 
public async Task LoadGamesAsync_WithValidGames_PopulatesGamesCollection()
{
    // Arrange - Set up test data and mocks
    var mocker = new AutoMocker();
    var games = new List<Game> 
    { 
        new GameTestDataBuilder().WithName("Game 1").WithId(1),
        new GameTestDataBuilder().WithName("Game 2").WithId(2)
    };
    
    mocker.GetMock<IGameRepository>()
        .Setup(x => x.GetAllAsync())
        .ReturnsAsync(games);

    var viewModel = mocker.CreateInstance<GamesViewModel>();

    // Act - Execute the method under test  
    await viewModel.LoadGamesCommand.ExecuteAsync(null);

    // Assert - Verify expected outcomes
    Assert.Equal(2, viewModel.Games.Count);
    Assert.Equal("Game 1", viewModel.Games[0].Name);
    Assert.False(viewModel.IsEmpty);
}
```

## Running Tests

### Command Line

```bash
# Run all tests
dotnet test ALGAE.Tests

# Run with detailed output
dotnet test ALGAE.Tests --logger console --verbosity normal

# Run specific test class
dotnet test ALGAE.Tests --filter "GameLaunchServiceTests"

# Run specific test method
dotnet test ALGAE.Tests --filter "LaunchGameAsync_WithValidGame_LaunchesSuccessfully"
```

### Visual Studio

- **Test Explorer**: View → Test Explorer
- **Run All Tests**: Test → Run All Tests
- **Debug Tests**: Right-click test → Debug

### Expected Output

```
Failed!  - Failed: 3, Passed: 21, Skipped: 0, Total: 24, Duration: 1 s
Test summary: total: 24, failed: 3, succeeded: 21, skipped: 0
```

## Test Categories

### Service Tests

**Purpose**: Test business logic in service classes

**Example**: GameLaunchServiceTests
- Validation logic (missing files, invalid paths)
- Launch success/failure scenarios  
- File system interaction testing

```csharp
[Fact]
public async Task ValidateGameAsync_WithMissingExecutable_ReturnsInvalid()
{
    // Arrange
    var mocker = new AutoMocker();
    var service = mocker.CreateInstance<GameLaunchService>();
    var game = new GameTestDataBuilder()
        .WithInvalidExecutable()
        .Build();

    // Act  
    var result = await service.ValidateGameAsync(game);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("executable", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
}
```

### ViewModel Tests

**Purpose**: Test UI logic and command handling

**Example**: GamesViewModelTests
- Data loading and binding
- Search and filter functionality
- Command execution and validation

```csharp
[Fact]
public void SearchText_UpdatesFilteredGames()
{
    // Arrange
    var mocker = new AutoMocker();
    var viewModel = mocker.CreateInstance<GamesViewModel>();
    
    viewModel.Games.Add(new GameTestDataBuilder().WithName("Minecraft").Build());
    viewModel.Games.Add(new GameTestDataBuilder().WithName("World of Warcraft").Build());

    // Act
    viewModel.SearchText = "craft";

    // Assert
    Assert.Equal(2, viewModel.FilteredGames.Count);
    Assert.True(viewModel.IsFiltered);
}
```

## Advanced Testing Patterns

### File System Testing

For services that interact with files:

```csharp
[Fact]
public async Task ValidateGameAsync_WithValidGame_ReturnsValid()
{
    // Create temporary test file
    var tempPath = Path.GetTempPath();
    var game = new GameTestDataBuilder()
        .WithInstallPath(tempPath)
        .WithExecutableName("test.exe")
        .Build();
        
    var executablePath = Path.Combine(tempPath, "test.exe");
    await File.WriteAllTextAsync(executablePath, "dummy");

    try
    {
        // Test with real file
        var result = await service.ValidateGameAsync(game);
        Assert.True(result.IsValid);
    }
    finally
    {
        // Always cleanup
        if (File.Exists(executablePath))
            File.Delete(executablePath);
    }
}
```

### Process Testing  

For services that launch processes:

```csharp
[Fact]
public async Task LaunchGameAsync_WithValidGame_ReturnsSuccessResult()
{
    // Use a safe, known Windows executable
    var game = new GameTestDataBuilder()
        .WithInstallPath(@"C:\Windows\System32")
        .WithExecutableName("notepad.exe")
        .Build();

    var result = await service.LaunchGameAsync(game);

    Assert.True(result.Success);
    Assert.NotNull(result.Process);

    // Always cleanup launched processes
    if (result.Process != null && !result.Process.HasExited)
    {
        result.Process.Kill();
        result.Process.Dispose();
    }
}
```

### Async Testing

Always use async/await for async methods:

```csharp
[Fact]
public async Task AsyncMethod_WithValidInput_ReturnsExpectedResult()
{
    // Arrange
    var mocker = new AutoMocker();
    
    // Act - Always await async methods
    var result = await service.SomeAsyncMethod();
    
    // Assert
    Assert.NotNull(result);
}
```

## Creating New Tests

### Adding Service Tests

1. **Create test file**: `ALGAE.Tests/Services/YourServiceTests.cs`
2. **Follow naming**: `[MethodName]_[Scenario]_[ExpectedBehavior]`
3. **Use AutoMocker**: `var mocker = new AutoMocker();`
4. **Create service**: `var service = mocker.CreateInstance<YourService>();`

### Adding ViewModel Tests

1. **Create test file**: `ALGAE.Tests/ViewModels/YourViewModelTests.cs`  
2. **Mock repositories**: Setup return values for data methods
3. **Test commands**: Execute commands and verify state changes
4. **Test properties**: Verify property notifications and binding

### Adding Test Data

1. **Create builder**: `ALGAE.Tests/TestData/YourEntityTestDataBuilder.cs`
2. **Fluent interface**: Add `With*()` methods for properties
3. **Common scenarios**: Add methods like `WithInvalidData()`
4. **Implicit conversion**: Add `public static implicit operator`

## Best Practices

### ✅ Do

- **Use descriptive test names** that explain the scenario
- **Follow AAA pattern** consistently
- **Clean up resources** (files, processes) in finally blocks
- **Test both success and failure paths**
- **Use test data builders** for consistent setup
- **Mock external dependencies** to isolate units under test
- **Verify service interactions** with `.Verify()`

### ❌ Don't

- **Test implementation details** - focus on behavior
- **Use hard-coded paths** - use temp directories or known system paths
- **Leave processes running** - always cleanup
- **Create overly complex test setups** 
- **Test multiple behaviors** in a single test
- **Ignore async/await** patterns

## Debugging Tests

### Common Issues

**Test fails intermittently:**
- Usually file system timing issues
- Add proper cleanup in `finally` blocks
- Use `Path.GetTempPath()` for temporary files

**Mock not working:**
- Check method signatures match exactly
- Verify generic types are correct
- Use `It.IsAny<Type>()` for flexible matching

**Async test hangs:**
- Always `await` async method calls
- Don't mix `.Result` or `.Wait()` with async

### Debugging Techniques

```csharp
[Fact]
public async Task DebuggingExample()
{
    // Add debug output
    System.Diagnostics.Debug.WriteLine("Starting test...");
    
    // Verify mock setup
    mocker.GetMock<IService>()
        .Verify(x => x.Method(), Times.Once, "Service method should be called once");
        
    // Check intermediate values
    Assert.True(someCondition, $"Expected condition failed. Actual value: {actualValue}");
}
```

## Future Testing Plans

### High Priority

1. **GameDetailViewModel Tests** - Profile management, launch validation
2. **CompanionLaunchService Tests** - Companion startup logic  
3. **Repository Tests** - Database integration tests

### Medium Priority

1. **Integration Tests** - End-to-end launch workflows
2. **UI Tests** - View interaction and binding tests
3. **Performance Tests** - Large dataset handling

### Infrastructure

1. **Code Coverage** - Add coverlet for coverage reporting
2. **CI/CD Integration** - Run tests in build pipeline  
3. **Test Categorization** - Organize tests by speed/type

## Conclusion

The testing infrastructure provides a solid foundation for ensuring ALGAE's reliability. The current 24 tests cover critical user paths (game launching with validation), and the patterns established make it easy to add comprehensive coverage for remaining components.

Focus on testing **business logic** and **user-facing functionality** rather than implementation details. This approach ensures tests remain valuable as the codebase evolves.
