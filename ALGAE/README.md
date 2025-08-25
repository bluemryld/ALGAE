# ALGAE - Advanced Launcher for Games and Associated Executables

A modern, Material Design-based WPF application for managing and launching your game library with a clean, professional interface.

## 🎮 Features

### Current Features
- **Modern UI**: Clean Material Design interface with intuitive navigation
- **Game Library Management**: Add, edit, and organize your game collection
- **Multiple Navigation Options**: 
  - Sidebar navigation with visual menu items
  - Top menu bar with keyboard shortcuts
  - Keyboard shortcuts (`Ctrl+1` for Home, `Ctrl+2` for Games, `Ctrl+3` for Launcher, `F5` for Refresh)
- **Add/Edit Game Dialog**: 
  - Form validation with real-time feedback
  - Auto-generation of short names
  - Browse functionality for game folders and executables
  - Keyboard shortcuts for quick actions
- **Games View**: 
  - Professional card-based game display
  - Search and filtering capabilities
  - Launch, edit, and delete game actions
  - Empty state guidance for new users
  - Loading states with progress indicators
- **Launcher View**: 
  - Real-time monitoring of running games
  - Process performance statistics (CPU, memory usage)
  - Game session tracking with play time
  - Window management controls (minimize, bring to front, stop)
  - Recent sessions history

### Planned Features
- **Automatic Game Scanning**: Discover installed games automatically
- **Game Categories & Tags**: Organize games with custom categories
- **Launch Statistics**: Track play time and launch frequency
- **Custom Game Icons**: Support for custom game artwork
- **Import/Export**: Backup and restore your game library
- **Steam Integration**: Import games from Steam library

## 🛠️ Technology Stack

- **Framework**: .NET 8.0 with WPF
- **UI Framework**: Material Design In XAML Toolkit
- **Architecture**: MVVM pattern with CommunityToolkit.Mvvm
- **Database**: SQLite with Entity Framework Core
- **Dependency Injection**: Microsoft.Extensions.Hosting
- **Logging**: Serilog

## 📋 Prerequisites

- .NET 8.0 SDK or later
- Windows 10/11 (WPF requirement)
- Visual Studio 2022 or JetBrains Rider (recommended for development)

## 🚀 Getting Started

### Installation

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd ALGAE
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Build the application**:
   ```bash
   dotnet build
   ```

4. **Run the application**:
   ```bash
   dotnet run --project ALGAE
   ```

### First Time Setup

1. Launch the application
2. Click "Add Game" to start building your library
3. Use the browse buttons to select game folders and executables
4. Fill in game details (name, publisher, version, description)
5. Save and launch your games!

## 📁 Project Structure

```
ALGAE/
├── ALGAE/                          # Main WPF application
│   ├── Views/                      # XAML views and code-behind
│   │   ├── MainWindow.xaml         # Main application window
│   │   ├── HomeView.xaml           # Home/dashboard view
│   │   ├── GamesView.xaml          # Games library view
│   │   ├── LauncherView.xaml       # Game launcher/monitor view
│   │   └── AddEditGameDialog.xaml  # Add/edit game dialog
│   ├── ViewModels/                 # MVVM view models
│   │   ├── MainViewModel.cs        # Main navigation logic
│   │   ├── HomeViewModel.cs        # Home view logic
│   │   ├── GamesViewModel.cs       # Games management logic
│   │   ├── LauncherViewModel.cs    # Game launcher/monitor logic
│   │   └── AddEditGameViewModel.cs # Game editing logic
│   ├── Converters/                 # XAML value converters
│   └── App.xaml                    # Application resources and startup
├── ALGAE.Core/                     # Core business logic
├── ALGAE.DAL/                      # Data access layer
│   ├── Models/                     # Entity models
│   ├── Repositories/               # Data repositories
│   └── DatabaseContext.cs         # EF Core context
└── README.md                       # This file
```

## 🔧 Configuration

### Database
The application automatically creates a SQLite database:
- **Development**: `ALGAE-dev.db` in the project folder
- **Production**: `%AppData%/AlgaeApp/Database/ALGAE.db`

### Logging
Logs are stored in:
- **Development**: `logs/development-log.txt` in the project folder
- **Production**: `%AppData%/AlgaeApp/Logs/ALGAE-Log.txt`

## ⌨️ Keyboard Shortcuts

- `Ctrl+1` - Navigate to Home
- `Ctrl+2` - Navigate to Games
- `Ctrl+3` - Navigate to Launcher
- `F5` - Refresh current view
- `Ctrl+N` - Add new game (in Games view)
- `Enter` - Save (in dialogs)
- `Escape` - Cancel (in dialogs)

## 🏗️ Development

### Building from Source

1. Ensure you have .NET 8.0 SDK installed
2. Clone the repository
3. Open in Visual Studio or your preferred IDE
4. Build and run the solution

### Architecture Overview

The application follows MVVM (Model-View-ViewModel) architecture:

- **Models**: Located in ALGAE.DAL, represent data entities
- **Views**: XAML files defining the UI layout
- **ViewModels**: Handle UI logic and data binding
- **Services**: Business logic and data access (in ALGAE.Core and ALGAE.DAL)

### Key Dependencies

- `MaterialDesignThemes.Wpf` - Material Design UI components
- `CommunityToolkit.Mvvm` - MVVM helpers and source generators
- `Microsoft.EntityFrameworkCore.Sqlite` - Database access
- `Serilog` - Logging framework

## 🐛 Troubleshooting

### Common Issues

**Application won't start**
- Ensure .NET 8.0 runtime is installed
- Check for missing dependencies with `dotnet restore`

**Games view not showing**
- This was a known issue caused by missing value converters, now resolved

**Database errors**
- Delete the database file to reset (will lose all data)
- Check write permissions in the application data folder

### Logging

Check the log files for detailed error information:
- Development: `logs/development-log.txt`
- Production: `%AppData%/AlgaeApp/Logs/ALGAE-Log.txt`

## 🤝 Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

### Development Guidelines

1. Follow MVVM pattern for new features
2. Use Material Design components where possible
3. Add appropriate logging for new functionality
4. Include XML documentation for public methods
5. Test thoroughly before submitting PRs

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙋‍♂️ Support

If you encounter any issues or have questions:

1. Check the troubleshooting section above
2. Look through existing GitHub issues
3. Create a new issue with detailed information about the problem

## 🗺️ Roadmap

- [ ] Steam library integration
- [ ] Automatic game detection and scanning
- [ ] Custom game categories and tagging
- [ ] Game statistics and analytics
- [ ] Custom themes and appearance options
- [ ] Export/import functionality
- [ ] Multi-language support

---

**ALGAE** - Making game management simple and beautiful! 🎮✨
