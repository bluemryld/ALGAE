# ALGAE Development TODO

> Last updated: 2025-01-25

## 🚀 High Priority

### Core Functionality
- [ ] **Game Launch System**
  - [ ] Implement game launching with proper error handling
  - [ ] Add launch parameters and working directory support
  - [ ] Process monitoring and cleanup
  - [ ] Launch history tracking

- [ ] **Game Management**
  - [ ] Complete CRUD operations for games
  - [ ] Bulk game operations (delete multiple, export)
  - [ ] Game validation (check if executable exists)
  - [ ] Duplicate detection

- [ ] **Search & Filtering**
  - [ ] Real-time search functionality
  - [ ] Advanced filtering (by publisher, genre, install date)
  - [ ] Sorting options (name, last played, date added)

## 📋 Features

### Auto-Discovery
- [ ] **Steam Integration**
  - [ ] Read Steam library from registry/config files
  - [ ] Import Steam games with metadata
  - [ ] Handle Steam shortcuts and launch options
  
- [ ] **Game Scanning**
  - [ ] Scan common installation directories
  - [ ] Detect popular game launchers (Epic, Origin, GOG)
  - [ ] Smart executable detection
  - [ ] Background scanning with progress

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
- [ ] **Performance**
  - [ ] Large game libraries loading slowly
  - [ ] Memory usage optimization
  - [ ] Database query optimization

- [ ] **UI Issues**
  - [ ] Window sizing and positioning persistence
  - [ ] High DPI scaling issues
  - [ ] Theme switching bugs

- [ ] **Data Issues**
  - [ ] Database migration handling
  - [ ] Corrupted game data recovery
  - [ ] Path validation improvements

## 🔧 Technical Debt

- [ ] **Code Quality**
  - [ ] Add unit tests for ViewModels
  - [ ] Integration tests for database operations
  - [ ] Code coverage reporting
  - [ ] Static code analysis setup

- [ ] **Architecture**
  - [ ] Implement proper service layer
  - [ ] Add dependency injection for services
  - [ ] Improve error handling and logging
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

### v1.0.0 - Core Release
- [ ] Basic game management (CRUD)
- [ ] Game launching functionality
- [ ] Search and filtering
- [ ] Stable UI with all major views

### v1.1.0 - Discovery Release
- [ ] Auto game scanning
- [ ] Steam integration
- [ ] Improved game detection

### v1.2.0 - Organization Release
- [ ] Categories and tags
- [ ] Advanced filtering
- [ ] Game statistics

### v2.0.0 - Advanced Release
- [ ] Multiple launcher support
- [ ] Cloud sync
- [ ] Plugin system

## 📊 Progress Tracking

### Current Sprint (Week of 2025-01-25)
- [x] Fix Games view display issue
- [x] Create comprehensive README
- [x] Set up development todo tracking
- [x] Implement game launch functionality (2025-01-25)
- [x] Add basic game validation (2025-01-25)

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

**Next Review:** End of January 2025
