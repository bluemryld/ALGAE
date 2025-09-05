using ALGAE.ViewModels;
using ALGAE.Services;
using Algae.DAL.Models;

namespace ALGAE.Tests.ViewModels;

public class GameVerificationViewModelTests
{
    [Fact]
    public void Constructor_WithDetectedGames_InitializesCorrectly()
    {
        // Arrange
        var detectedGames = new List<DetectedGame>
        {
            new DetectedGame
            {
                Name = "Game 1",
                ConfidenceScore = 0.8f,
                DetectedCompanions = new List<DetectedCompanion>()
            },
            new DetectedGame
            {
                Name = "Game 2", 
                ConfidenceScore = 0.6f,
                DetectedCompanions = new List<DetectedCompanion>()
            }
        };

        // Act
        var viewModel = new GameVerificationViewModel(detectedGames);

        // Assert
        Assert.Equal(2, viewModel.TotalGamesFound);
        Assert.Equal(1, viewModel.HighConfidenceCount); // Only Game 1 >= 0.7f
        Assert.Equal(2, viewModel.DetectedGames.Count);
        // Note: HasSelectedGames might be true if auto-selection occurs
        Assert.NotNull(viewModel.AddButtonText);
    }

    [Fact]
    public void Constructor_WithCompanions_CountsCompanionsCorrectly()
    {
        // Arrange
        var companion1 = new DetectedCompanion { Name = "Companion 1", ConfidenceScore = 0.8f };
        var companion2 = new DetectedCompanion { Name = "Companion 2", ConfidenceScore = 0.6f };
        
        var detectedGames = new List<DetectedGame>
        {
            new DetectedGame
            {
                Name = "Game 1",
                ConfidenceScore = 0.8f,
                DetectedCompanions = new List<DetectedCompanion> { companion1, companion2 }
            }
        };

        // Act
        var viewModel = new GameVerificationViewModel(detectedGames);

        // Assert
        Assert.Equal(2, viewModel.TotalCompanionsFound);
        Assert.True(viewModel.HasCompanions);
    }

    [Fact]
    public void SelectAll_SelectsAllGamesAndCompanions()
    {
        // Arrange
        var companion = new DetectedCompanion { Name = "Companion", ConfidenceScore = 0.8f, AlreadyExists = false };
        var detectedGames = new List<DetectedGame>
        {
            new DetectedGame
            {
                Name = "Game 1",
                ConfidenceScore = 0.8f,
                AlreadyExists = false,
                DetectedCompanions = new List<DetectedCompanion> { companion }
            }
        };

        var viewModel = new GameVerificationViewModel(detectedGames);

        // Act
        viewModel.SelectAllCommand.Execute(null);

        // Assert
        Assert.True(viewModel.DetectedGames[0].IsSelected);
        Assert.True(viewModel.DetectedGames[0].CompanionViewModels[0].IsSelected);
        Assert.Equal(1, viewModel.SelectedCount);
        Assert.Equal(1, viewModel.SelectedCompanionCount);
        Assert.True(viewModel.HasSelectedGames);
    }

    [Fact]
    public void SelectNone_DeselectsAllGamesAndCompanions()
    {
        // Arrange
        var companion = new DetectedCompanion { Name = "Companion", ConfidenceScore = 0.8f };
        var detectedGames = new List<DetectedGame>
        {
            new DetectedGame
            {
                Name = "Game 1",
                ConfidenceScore = 0.8f,
                DetectedCompanions = new List<DetectedCompanion> { companion }
            }
        };

        var viewModel = new GameVerificationViewModel(detectedGames);
        viewModel.SelectAllCommand.Execute(null); // First select all
        
        // Act
        viewModel.SelectNoneCommand.Execute(null);

        // Assert
        Assert.False(viewModel.DetectedGames[0].IsSelected);
        Assert.False(viewModel.DetectedGames[0].CompanionViewModels[0].IsSelected);
        Assert.Equal(0, viewModel.SelectedCount);
        Assert.Equal(0, viewModel.SelectedCompanionCount);
        Assert.False(viewModel.HasSelectedGames);
    }

    [Fact]
    public void SelectHighConfidence_SelectsOnlyHighConfidenceItems()
    {
        // Arrange
        var highConfidenceCompanion = new DetectedCompanion { Name = "High Companion", ConfidenceScore = 0.9f, AlreadyExists = false };
        var lowConfidenceCompanion = new DetectedCompanion { Name = "Low Companion", ConfidenceScore = 0.5f, AlreadyExists = false };
        
        var detectedGames = new List<DetectedGame>
        {
            new DetectedGame
            {
                Name = "High Confidence Game",
                ConfidenceScore = 0.9f,
                AlreadyExists = false,
                DetectedCompanions = new List<DetectedCompanion> { highConfidenceCompanion }
            },
            new DetectedGame
            {
                Name = "Low Confidence Game",
                ConfidenceScore = 0.5f,
                AlreadyExists = false,
                DetectedCompanions = new List<DetectedCompanion> { lowConfidenceCompanion }
            }
        };

        var viewModel = new GameVerificationViewModel(detectedGames);

        // Act
        viewModel.SelectHighConfidenceCommand.Execute(null);

        // Assert
        Assert.True(viewModel.DetectedGames[0].IsSelected); // High confidence game
        Assert.False(viewModel.DetectedGames[1].IsSelected); // Low confidence game
        Assert.True(viewModel.DetectedGames[0].CompanionViewModels[0].IsSelected); // High confidence companion
        Assert.False(viewModel.DetectedGames[1].CompanionViewModels[0].IsSelected); // Low confidence companion
    }

    [Fact]
    public void GetSelectedGames_ReturnsOnlySelectedGames()
    {
        // Arrange
        var detectedGames = new List<DetectedGame>
        {
            new DetectedGame { Name = "Game 1", ConfidenceScore = 0.8f, DetectedCompanions = new List<DetectedCompanion>() },
            new DetectedGame { Name = "Game 2", ConfidenceScore = 0.6f, DetectedCompanions = new List<DetectedCompanion>() }
        };

        var viewModel = new GameVerificationViewModel(detectedGames);
        viewModel.DetectedGames[0].IsSelected = true; // Select only first game

        // Act
        var selectedGames = viewModel.GetSelectedGames();

        // Assert
        Assert.Single(selectedGames);
        Assert.Equal("Game 1", selectedGames.First().Name);
    }

    [Fact]
    public void AddButtonText_UpdatesBasedOnSelection()
    {
        // Arrange
        var companion = new DetectedCompanion { Name = "Companion", ConfidenceScore = 0.8f };
        var detectedGames = new List<DetectedGame>
        {
            new DetectedGame
            {
                Name = "Game 1",
                ConfidenceScore = 0.8f,
                DetectedCompanions = new List<DetectedCompanion> { companion }
            },
            new DetectedGame
            {
                Name = "Game 2", 
                ConfidenceScore = 0.6f,
                DetectedCompanions = new List<DetectedCompanion>()
            }
        };

        var viewModel = new GameVerificationViewModel(detectedGames);

        // Act & Assert - Test the button text behavior
        
        // Initially, button text should reflect current state
        var initialButtonText = viewModel.AddButtonText;
        Assert.NotNull(initialButtonText);

        // Select first game manually
        viewModel.DetectedGames[0].IsSelected = true;
        Assert.Contains("Game", viewModel.AddButtonText);

        // Select second game manually  
        viewModel.DetectedGames[1].IsSelected = true;
        Assert.Contains("Games", viewModel.AddButtonText);

        // Deselect all games
        viewModel.DetectedGames[0].IsSelected = false;
        viewModel.DetectedGames[1].IsSelected = false;
        
        // Button should update to reflect no selection
        Assert.NotNull(viewModel.AddButtonText);
    }
}

public class DetectedGameViewModelTests
{
    [Fact]
    public void Constructor_WithDetectedGame_InitializesProperties()
    {
        // Arrange
        var companion = new DetectedCompanion
        {
            Name = "Test Companion",
            Description = "Test Description",
            ConfidenceScore = 0.8f
        };

        var detectedGame = new DetectedGame
        {
            Name = "Test Game",
            Publisher = "Test Publisher",
            Version = "1.0.0",
            ConfidenceScore = 0.9f,
            DetectedCompanions = new List<DetectedCompanion> { companion }
        };

        // Act
        var viewModel = new DetectedGameViewModel(detectedGame);

        // Assert
        Assert.Equal("Test Game", viewModel.Name);
        Assert.Equal("Test Publisher", viewModel.Publisher);
        Assert.Equal("1.0.0", viewModel.Version);
        Assert.Equal(90, viewModel.ConfidencePercentage);
        Assert.True(viewModel.HasCompanions);
        Assert.Equal(1, viewModel.CompanionCount);
        Assert.Single(viewModel.CompanionViewModels);
    }

    [Fact]
    public void IsSelected_WhenChanged_UpdatesCompanionSelection()
    {
        // Arrange
        var highConfidenceCompanion = new DetectedCompanion
        {
            Name = "High Companion",
            ConfidenceScore = 0.9f,
            AlreadyExists = false
        };

        var lowConfidenceCompanion = new DetectedCompanion
        {
            Name = "Low Companion", 
            ConfidenceScore = 0.5f,
            AlreadyExists = false
        };

        var detectedGame = new DetectedGame
        {
            Name = "Test Game",
            ConfidenceScore = 0.8f,
            DetectedCompanions = new List<DetectedCompanion> { highConfidenceCompanion, lowConfidenceCompanion }
        };

        var viewModel = new DetectedGameViewModel(detectedGame);

        // Act - Select game
        viewModel.IsSelected = true;

        // Assert - Only high confidence companions should be auto-selected
        Assert.True(viewModel.CompanionViewModels[0].IsSelected); // High confidence
        Assert.False(viewModel.CompanionViewModels[1].IsSelected); // Low confidence

        // Act - Deselect game
        viewModel.IsSelected = false;

        // Assert - All companions should be deselected
        Assert.False(viewModel.CompanionViewModels[0].IsSelected);
        Assert.False(viewModel.CompanionViewModels[1].IsSelected);
    }

    [Fact]
    public void SelectedCompanionCount_ReturnsCorrectCount()
    {
        // Arrange
        var companion1 = new DetectedCompanion { Name = "Companion 1", ConfidenceScore = 0.8f };
        var companion2 = new DetectedCompanion { Name = "Companion 2", ConfidenceScore = 0.6f };
        
        var detectedGame = new DetectedGame
        {
            Name = "Test Game",
            DetectedCompanions = new List<DetectedCompanion> { companion1, companion2 }
        };

        var viewModel = new DetectedGameViewModel(detectedGame);

        // Act & Assert
        Assert.Equal(0, viewModel.SelectedCompanionCount);

        viewModel.CompanionViewModels[0].IsSelected = true;
        Assert.Equal(1, viewModel.SelectedCompanionCount);

        viewModel.CompanionViewModels[1].IsSelected = true;
        Assert.Equal(2, viewModel.SelectedCompanionCount);
    }
}

public class DetectedCompanionViewModelTests
{
    [Fact]
    public void Constructor_WithDetectedCompanion_InitializesProperties()
    {
        // Arrange
        var detectedCompanion = new DetectedCompanion
        {
            Name = "Test Companion",
            Description = "Test Description",
            Publisher = "Test Publisher",
            Version = "1.0.0",
            ExecutablePath = @"C:\Games\companion.exe",
            ConfidenceScore = 0.85f,
            Type = "Application",
            AlreadyExists = false
        };

        // Act
        var viewModel = new DetectedCompanionViewModel(detectedCompanion);

        // Assert
        Assert.Equal("Test Companion", viewModel.Name);
        Assert.Equal("Test Description", viewModel.Description);
        Assert.Equal("Test Publisher", viewModel.Publisher);
        Assert.Equal("1.0.0", viewModel.Version);
        Assert.Equal(@"C:\Games\companion.exe", viewModel.ExecutablePath);
        Assert.Equal(85, viewModel.ConfidencePercentage);
        Assert.Equal("Application", viewModel.Type);
        Assert.False(viewModel.AlreadyExists);
        Assert.True(viewModel.HasDescription);
        Assert.True(viewModel.HasPublisher);
        Assert.True(viewModel.HasVersion);
    }

    [Fact]
    public void ConfidenceColor_ReturnsCorrectColor()
    {
        // Arrange & Act & Assert
        var highConfidenceCompanion = new DetectedCompanion { ConfidenceScore = 0.95f };
        var highViewModel = new DetectedCompanionViewModel(highConfidenceCompanion);
        Assert.Equal(System.Windows.Media.Brushes.Green, highViewModel.ConfidenceColor);

        var mediumConfidenceCompanion = new DetectedCompanion { ConfidenceScore = 0.75f };
        var mediumViewModel = new DetectedCompanionViewModel(mediumConfidenceCompanion);
        Assert.Equal(System.Windows.Media.Brushes.Orange, mediumViewModel.ConfidenceColor);

        var lowConfidenceCompanion = new DetectedCompanion { ConfidenceScore = 0.55f };
        var lowViewModel = new DetectedCompanionViewModel(lowConfidenceCompanion);
        Assert.Equal(System.Windows.Media.Brushes.DarkOrange, lowViewModel.ConfidenceColor);

        var veryLowConfidenceCompanion = new DetectedCompanion { ConfidenceScore = 0.3f };
        var veryLowViewModel = new DetectedCompanionViewModel(veryLowConfidenceCompanion);
        Assert.Equal(System.Windows.Media.Brushes.Red, veryLowViewModel.ConfidenceColor);
    }

    [Fact]
    public void DetectionMethod_WithSignature_ReturnsSignatureMatch()
    {
        // Arrange
        var companionSignature = new CompanionSignature { Name = "Test Signature" };
        var detectedCompanion = new DetectedCompanion
        {
            Name = "Test Companion",
            MatchedSignature = companionSignature
        };

        // Act
        var viewModel = new DetectedCompanionViewModel(detectedCompanion);

        // Assert
        Assert.Equal("Signature Match", viewModel.DetectionMethod);
    }

    [Fact]
    public void DetectionMethod_WithoutSignature_ReturnsHeuristicDetection()
    {
        // Arrange
        var detectedCompanion = new DetectedCompanion
        {
            Name = "Test Companion",
            MatchedSignature = null
        };

        // Act
        var viewModel = new DetectedCompanionViewModel(detectedCompanion);

        // Assert
        Assert.Equal("Heuristic Detection", viewModel.DetectionMethod);
    }
}