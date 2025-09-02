# ALGAE Architecture Documentation

This document outlines the technical architecture, design patterns, and project structure of ALGAE.

## Overview

ALGAE follows a **layered architecture** with **MVVM pattern** for the presentation layer, built on .NET 8 and WPF with dependency injection.

## Architecture Layers

### 1. Presentation Layer (ALGAE)
**Technology**: WPF with Material Design, MVVM pattern  
**Responsibility**: User interface, user interaction, view logic

```
ALGAE/
├── Views/              # XAML views and code-behind
├── ViewModels/         # MVVM view models with business logic
├── Converters/         # XAML value converters
├── Services/           # UI-specific services
└── App.xaml(.cs)       # Application entry point and DI setup
```

### 2. Business Logic Layer (ALGAE.Core)
**Technology**: .NET 8 Class Library  
**Responsibility**: Core business logic, services, interfaces

```
ALGAE.Core/
└── Services/           # Business services and interfaces
    ├── ILogService.cs
    ├── LogService.cs
    └── ...
```

### 3. Data Access Layer (ALGAE.DAL)
**Technology**: Entity Framework Core, SQLite, Dapper  
**Responsibility**: Data persistence, database operations

```
ALGAE.DAL/
├── Models/             # Data entities
├── Repositories/       # Data access repositories  
└── DatabaseContext.cs  # Database context and connection management
```

### 4. Test Layer (ALGAE.Tests)
**Technology**: xUnit, Moq  
**Responsibility**: Unit and integration testing

## Design Patterns

### MVVM (Model-View-ViewModel)

ALGAE strictly follows the MVVM pattern for separation of concerns:

```
View (XAML) ←→ ViewModel ←→ Model/Services
     ↕              ↕           ↕
  UI Logic    Binding/    Business Logic
            Commands      Data Access
```

**ViewModels:**
- `MainViewModel` - Main window navigation and global state
- `HomeViewModel` - Dashboard and summary information
- `GamesViewModel` - Game library management
- `GameDetailViewModel` - Individual game details and profiles
- `GameSignaturesViewModel` - Game signature database management
- `CompanionsViewModel` - Companion applications management
- `LauncherViewModel` - Game process monitoring and control
- `GameScanProgressViewModel` - Game scanning progress tracking
- `GameVerificationViewModel` - Game verification and duplicate detection
- `AddEditProfileViewModel` - Profile creation and editing
- `AddEditCompanionViewModel` - Companion application configuration

**Key MVVM Features:**
- **Data Binding**: Two-way binding between Views and ViewModels
- **Commands**: `RelayCommand` for user actions
- **Observables**: `ObservableProperty` for change notifications
- **Messaging**: `WeakReferenceMessenger` for cross-VM communication

### Repository Pattern

Data access is abstracted through repository interfaces:

```csharp
// Interface definition
public interface IGameRepository
{
    Task<IEnumerable<Game>> GetAllAsync();
    Task<Game?> GetByIdAsync(int gameId);
    Task AddAsync(Game game);
    Task UpdateAsync(Game game);
    Task DeleteAsync(int gameId);
}

// Implementation
public class GameRepository : IGameRepository
{
    private readonly DatabaseContext _dbContext;
    // Implementation using Dapper for performance
}
```

**Repository Benefits:**
- **Testability**: Easy mocking for unit tests
- **Separation**: Business logic doesn't know about data storage
- **Flexibility**: Can switch between EF Core and Dapper as needed

### Dependency Injection

ALGAE uses Microsoft's built-in DI container configured in `App.xaml.cs`:

```csharp
services.AddSingleton<MainWindow>();
services.AddTransient<IGameRepository, GameRepository>();
services.AddSingleton<INotificationService, NotificationService>();
```

**DI Benefits:**
- **Loose Coupling**: Components depend on interfaces
- **Testability**: Easy to inject mock dependencies
- **Lifetime Management**: Proper object lifecycle management

### Service Layer Pattern

Services encapsulate business logic and cross-cutting concerns:

- `INotificationService` - User notifications and feedback
- `IGameLaunchService` - Game launching and validation
- `IGameDetectionService` - Automatic game discovery and scanning
- `IGameSignatureService` - Game signature database management
- `IGameProcessMonitorService` - Game process monitoring
- `ICompanionLaunchService` - Companion application management
- `ILauncherWindowManager` - Launcher window lifecycle management
- `ILogService` - Application logging

## Project Structure Detail

### ALGAE (Main Application)

```
ALGAE/
├── App.xaml.cs                     # DI container configuration
├── MainWindow.xaml                 # Shell window with navigation
├── Views/
│   ├── HomeView.xaml              # Dashboard
│   ├── GamesView.xaml             # Game library grid
│   ├── GameSignaturesView.xaml    # Game signatures management
│   ├── CompanionsView.xaml        # Companion applications view
│   ├── GameDetailView.xaml        # Game details and profiles
│   ├── LauncherView.xaml          # Process monitoring
│   ├── LauncherWindow.xaml        # Separate launcher window
│   ├── AddEditGameDialog.xaml     # Game creation/editing
│   ├── AddEditProfileDialog.xaml  # Profile creation/editing
│   ├── AddEditCompanionDialog.xaml # Companion configuration
│   ├── GameScanProgressDialog.xaml # Game scanning progress
│   ├── GameVerificationDialog.xaml # Game verification results
│   └── SearchPathManagementDialog.xaml # Search paths configuration
├── ViewModels/
│   ├── MainViewModel.cs           # Navigation and global state
│   ├── HomeViewModel.cs           # Dashboard logic
│   ├── GamesViewModel.cs          # Game management
│   ├── GameSignaturesViewModel.cs # Game signatures management
│   ├── CompanionsViewModel.cs     # Companion applications logic
│   ├── GameScanProgressViewModel.cs # Game scanning progress
│   ├── GameVerificationViewModel.cs # Game verification logic
│   ├── SearchPathManagementViewModel.cs # Search paths configuration
│   ├── GameDetailViewModel.cs     # Game details and profiles
│   ├── LauncherViewModel.cs       # Process monitoring
│   └── AddEdit*ViewModel.cs       # Dialog view models
├── Services/
│   ├── NotificationService.cs     # User notifications
│   ├── GameProcessMonitorService.cs # Process monitoring
│   └── CompanionLaunchService.cs  # Companion management
├── Converters/
│   ├── BooleanToVisibilityConverter.cs
│   └── NullToVisibilityConverter.cs
└── Models/
    └── GameSession.cs             # Runtime game session data
```

### ALGAE.DAL (Data Access Layer)

```
ALGAE.DAL/
├── DatabaseContext.cs             # Connection management
├── DatabaseInitializer.cs         # Schema creation and migration
├── Models/                        # Entity models
│   ├── Game.cs                    # Core game entity
│   ├── Profile.cs                 # Game launch profiles
│   ├── Companion.cs               # Companion applications
│   ├── CompanionProfile.cs        # Profile-companion associations
│   ├── GameSignature.cs           # Game detection signatures
│   ├── CompanionSignature.cs      # Companion detection signatures
│   └── SearchPath.cs              # Scan directories
└── Repositories/                  # Data access repositories
    ├── GameRepository.cs
    ├── ProfilesRepository.cs
    ├── CompanionRepository.cs
    ├── CompanionProfileRepository.cs
    ├── GameSignatureRepository.cs
    ├── CompanionSignatureRepository.cs
    └── SearchPathRepository.cs
```

## Data Flow Architecture

### Typical User Action Flow

```
1. User Action (Click/Input)
        ↓
2. View → Command Binding
        ↓  
3. ViewModel → Command Handler
        ↓
4. ViewModel → Service/Repository
        ↓
5. Service → Repository → Database
        ↓
6. Database → Repository → Service
        ↓
7. Service → ViewModel → Property Update
        ↓
8. ViewModel → View (Data Binding)
        ↓
9. View Updates UI
```

### Example: Adding a New Game

```csharp
// 1. User clicks "Add Game" button
// 2. Command binding triggers AddGameCommand

[RelayCommand]
private async Task AddGameAsync()
{
    // 3. ViewModel handles command
    var dialog = new AddEditGameDialog();
    var result = dialog.ShowDialog();
    
    if (result == true)
    {
        // 4. ViewModel calls service
        await _gameRepository.AddAsync(dialog.Game);
        
        // 5. Refresh view
        await LoadGamesAsync();
        
        // 6. Notify user
        _notificationService.ShowSuccess("Game added successfully!");
    }
}
```

## Configuration Architecture

### Environment Detection

ALGAE automatically detects development vs production environments:

```csharp
public static bool IsDevelopmentEnvironment()
{
    #if DEBUG
        return true;
    #endif
    
    // Check for development indicators:
    // - Source directory structure
    // - Debug build paths  
    // - Development database files
    // - Development tools running
}
```

### Database Configuration

**Development Environment:**
- Database: `ALGAE-dev.db` in project root
- Logs: `logs/development-log.txt`
- Full debug logging enabled

**Production Environment:**  
- Database: `%AppData%/AlgaeApp/Database/ALGAE.db`
- Logs: `%AppData%/AlgaeApp/Logs/ALGAE-Log.txt`
- Optimized logging levels

### Dependency Injection Configuration

```csharp
// In App.xaml.cs CreateHostBuilder()
services.AddSingleton<MainWindow>();
services.AddTransient<GamesViewModel>();

// Repositories
services.AddTransient<IGameRepository, GameRepository>();

// Services  
services.AddSingleton<INotificationService, NotificationService>();

// Database
services.AddSingleton(_ => new DatabaseContext(GetDatabasePath()));
services.AddTransient<DatabaseInitializer>();
```

## Performance Considerations

### Database Performance

- **Dapper for Queries**: High-performance SQL execution
- **EF Core for Migrations**: Schema management and complex operations
- **Connection Pooling**: Efficient connection reuse
- **Async Operations**: Non-blocking database access

### UI Performance

- **Async ViewModels**: Non-blocking UI operations
- **ObservableProperty**: Efficient change notifications
- **Weak References**: Prevent memory leaks in messaging
- **Virtual Collections**: Handle large datasets efficiently

### Memory Management

- **Dispose Pattern**: Proper resource cleanup
- **Using Statements**: Automatic disposal of connections
- **Weak Messaging**: Prevent ViewModel memory leaks
- **Image Optimization**: Efficient handling of game artwork

## Testing Architecture

### Test Structure

```
ALGAE.Tests/
├── ViewModels/         # ViewModel unit tests
├── Services/           # Service unit tests  
├── Repositories/       # Repository integration tests
└── TestHelpers/        # Test utilities and mocks
```

### Testing Patterns

**Repository Testing:**
```csharp
[Test]
public async Task GetByIdAsync_ShouldReturnGame_WhenGameExists()
{
    // Arrange
    var repository = new GameRepository(_testDbContext);
    var game = new Game { Name = "Test Game" };
    await repository.AddAsync(game);
    
    // Act
    var result = await repository.GetByIdAsync(game.GameId);
    
    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Name, Is.EqualTo("Test Game"));
}
```

**ViewModel Testing with Mocks:**
```csharp
[Test]
public async Task LoadGamesAsync_ShouldPopulateGames()
{
    // Arrange
    var mockRepository = new Mock<IGameRepository>();
    mockRepository.Setup(r => r.GetAllAsync())
              .ReturnsAsync(new List<Game> { new Game() });
    
    var viewModel = new GamesViewModel(mockRepository.Object);
    
    // Act
    await viewModel.LoadGamesAsync();
    
    // Assert  
    Assert.That(viewModel.Games.Count, Is.EqualTo(1));
}
```

## Future Architecture Considerations

### Planned Enhancements

1. **Plugin Architecture**: Support for companion plugins
2. **Event Sourcing**: Audit trail for game launches and changes
3. **Caching Layer**: Redis for frequently accessed data
4. **Microservices**: Split into companion service + launcher service
5. **API Layer**: REST API for external integrations

### Scalability Patterns

1. **CQRS**: Separate read/write models for complex operations
2. **Message Queue**: Async processing for heavy operations
3. **Database Partitioning**: Separate signature database from user data
4. **Background Services**: Long-running tasks for process monitoring

This architecture provides a solid foundation for ALGAE's current functionality while remaining extensible for future enhancements.
