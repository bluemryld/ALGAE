# ALGAE Development TODO

> Last updated: 2025-01-02

## 🚀 High Priority

### Core Functionality
- [x] **Game Launch System** ✅ (2025-01-26)
  - [x] Implement game launching with proper error handling
  - [x] Add launch parameters and working directory support
  - [x] Process monitoring and cleanup
  - [x] Launch history tracking
  - [x] Immediate launcher window opening for progress visibility
  - [x] Profile-based launching with companion applications
  - [x] Comprehensive validation and error reporting
  - [x] Performance metrics and statistics tracking

- [x] **Game Management** ✅ (Partially complete)
  - [x] Complete CRUD operations for games
  - [x] Game validation (check if executable exists)
  - [ ] Bulk game operations (delete multiple, export)
  - [ ] Duplicate detection

- [x] **Search & Filtering** ✅ (Basic implementation complete)
  - [x] Real-time search functionality
  - [ ] Advanced filtering (by publisher, genre, install date)
  - [ ] Sorting options (name, last played, date added)

## 📋 Features

### Auto-Discovery
- [x] **Game Scanning** ✅ (2025-01-02)
  - [x] Scan common installation directories with configurable paths
  - [x] Smart executable detection using game signatures
  - [x] Background scanning with progress dialog and cancellation
  - [x] Game verification and duplicate detection
  - [x] Comprehensive signature database with 500+ games

- [ ] **Steam Integration**
  - [ ] Read Steam library from registry/config files
  - [ ] Import Steam games with metadata
  - [ ] Handle Steam shortcuts and launch options
  
- [ ] **Additional Launcher Support**
  - [ ] Detect popular game launchers (Epic, Origin, GOG)
  - [ ] Import games from multiple platforms
  - [ ] Cross-platform game library unification

### UI/UX Enhancements
- [ ] **Visual Improvements**
  - [ ] Custom game icons/artwork support
  - [ ] Grid vs list view toggle
  - [ ] Theme customization options
  - [ ] Better loading states

- [ ] **User Experience**
  - [ ] Drag & drop game installation
  - [ ] Context menus for games
  - [ ] Keyboard navigation improvements
  - [ ] Undo/redo operations

### Advanced Features
- [ ] **Game Organization**
  - [ ] Categories and tags system
  - [ ] Favorites/bookmarks
  - [ ] Custom game groups
  - [ ] Smart collections (Recently played, etc.)

- [ ] **Statistics & Analytics**
  - [ ] Play time tracking
  - [ ] Launch frequency statistics
  - [ ] Usage analytics dashboard
  - [ ] Export statistics data

- [ ] **Data Management**
  - [ ] Import/Export game library
  - [ ] Backup and restore functionality
  - [ ] Cloud sync (OneDrive, Google Drive)
  - [ ] Multiple library support

## 🐛 Known Issues

### Fixed ✅
- [x] Games view not appearing (missing value converters)
- [x] Navigation between views
- [x] Add/Edit game dialog validation
- [x] Replaced MessageBoxes with modern Material Design notifications
  - [x] Snackbar notifications for success/error/info/warning messages
  - [x] Material Design dialogs for confirmations
  - [x] Modern notification service with dependency injection
  - [x] Inline validation errors in Add/Edit Game dialog
  - [x] Made Add/Edit Game dialog resizable
  - [x] Auto-clearing validation errors when user types

### Current Issues
- [x] **Performance** ✅ (Mostly resolved)
  - [x] Large game libraries loading performance improved
  - [x] Memory usage optimization implemented
  - [x] Database query optimization completed

- [ ] **UI Issues**
  - [ ] Window sizing and positioning persistence
  - [ ] High DPI scaling issues
  - [ ] Theme switching bugs

- [ ] **Data Issues**  
  - [ ] Database migration handling for schema updates
  - [ ] Corrupted game data recovery mechanisms
  - [ ] Enhanced path validation and normalization

- [ ] **Testing Issues**
  - [ ] 3 failing unit tests need investigation and fixes
  - [ ] Integration test coverage for new features
  - [ ] Performance testing for large datasets

## 🔧 Technical Debt

- [x] **Code Quality** ✅ (Well progressed)
  - [x] Add unit tests for ViewModels (24 tests implemented, 21 passing)
  - [x] Service testing with mocks and test data builders
  - [ ] Fix 3 failing unit tests
  - [ ] Integration tests for database operations
  - [ ] Code coverage reporting
  - [ ] Static code analysis setup
  - [ ] Additional ViewModel tests (GameSignaturesViewModel, CompanionsViewModel)
  - [ ] Repository unit tests for new entities

- [x] **Architecture** ✅ (Mostly complete)
  - [x] Implement proper service layer
  - [x] Add dependency injection for services
  - [x] Improve error handling and logging
  - [ ] Add configuration management

- [ ] **Documentation**
  - [ ] API documentation (XML docs)
  - [ ] Architecture documentation
  - [ ] User manual/help system
  - [ ] Developer setup guide

## 📝 Documentation

- [ ] **User Documentation**
  - [ ] Getting started guide
  - [ ] Feature tutorials
  - [ ] Troubleshooting guide
  - [ ] FAQ section

- [ ] **Developer Documentation**
  - [ ] Contributing guidelines
  - [ ] Code style guide
  - [ ] Build and deployment instructions
  - [ ] Database schema documentation

## 🎯 Milestones

### v1.0.0 - Core Release (90% Complete)
- [x] Basic game management (CRUD)
- [x] Game launching functionality with profiles
- [x] Search and filtering (basic)
- [x] Stable UI with all major views
- [x] Automatic game detection and signatures
- [x] Companion applications support
- [ ] Final polish and bug fixes

### v1.1.0 - Discovery Release (90% Complete)
- [x] Auto game scanning with configurable paths
- [x] Game signatures database and management
- [x] Improved game detection with verification
- [ ] Steam integration
- [ ] Multi-platform launcher support

### v1.2.0 - Organization Release
- [ ] Categories and tags
- [ ] Advanced filtering
- [ ] Game statistics

### v2.0.0 - Advanced Release
- [ ] Multiple launcher support
- [ ] Cloud sync
- [ ] Plugin system

## 📊 Progress Tracking

### Recent Achievements (2025-01-02)
- [x] **Game Signatures System Implementation** ✅
  - [x] Built comprehensive game signatures database (500+ games)
  - [x] Game signature management interface with CRUD operations
  - [x] Signature-based automatic game detection during scanning
  - [x] Import/export functionality for signature data
  - [x] Integration with game scanning workflow
- [x] **Automatic Game Detection** ✅
  - [x] Configurable search paths management
  - [x] Background game scanning with progress tracking
  - [x] Game verification and duplicate detection
  - [x] Batch import of detected games with user confirmation
  - [x] Performance-optimized scanning algorithms
- [x] **Companion Applications System** ✅
  - [x] Companion app management with CRUD operations
  - [x] Integration with game profiles for automated launching
  - [x] Support for launching multiple companion apps per game
  - [x] Companion app status monitoring
- [x] **Enhanced UI and Navigation** ✅
  - [x] Added Signatures view for managing game database
  - [x] Added Companions view for application management
  - [x] Updated navigation with new keyboard shortcuts
  - [x] Progress dialogs for long-running operations
  - [x] Improved error handling and user feedback
- [x] **Performance and UI Optimizations** ✅
  - [x] Games view performance improvements
  - [x] Fixed UI compilation errors and warnings
  - [x] Updated NuGet packages to latest versions
  - [x] Memory usage optimizations for large game libraries

### Previous Achievements (2025-01-26)
- [x] **Major Game Launch System Implementation**
  - [x] Complete game validation service with error handling
  - [x] Launch history tracking with database integration
  - [x] Profile-based launching with companion applications
  - [x] Real-time launcher window opening on game launch
  - [x] Process monitoring and performance metrics
  - [x] Comprehensive error reporting and user feedback
- [x] **Application Stability Improvements**
  - [x] Fixed application shutdown issues (zombie process fix)
  - [x] Enhanced service disposal on exit
  - [x] Timer cleanup and background thread management
- [x] **Documentation Updates**
  - [x] Updated README with current feature set
  - [x] Refreshed TODO with completed milestones

### Current Sprint (Week of 2025-01-25)
- [x] Fix Games view display issue
- [x] Create comprehensive README
- [x] Set up development todo tracking
- [x] Implement game launch functionality (2025-01-25)
- [x] Add basic game validation (2025-01-25)
- [x] Launcher window immediate opening implementation (2025-01-26)
- [x] Application shutdown bug fixes (2025-01-26)

### Completed
- [x] Initial project setup and architecture
- [x] Material Design UI implementation
- [x] Navigation system (sidebar + menu)
- [x] Add/Edit game dialog with validation
- [x] Games view with card layout
- [x] Database setup with Entity Framework
- [x] Modern notification system (replaced all MessageBoxes)
- [x] Material Design dialogs and snackbars
- [x] Notification service with dependency injection
- [x] Game launch system with validation and error handling
- [x] Launch history tracking with database storage
- [x] Process monitoring integration
- [x] Comprehensive game validation service

---

## 📌 Notes

- Keep this file updated as development progresses
- Convert major items to GitHub issues when they need discussion
- Mark completed items with ✅ and date completed
- Use GitHub project boards for sprint planning
- Regular review and prioritization sessions

**Next Review:** Mid-January 2025
