using ALGAE.ViewModels;
using ALGAE.Services;
using ALGAE.DAL.Repositories;
using ALGAE.Tests.TestData;
using Algae.DAL.Models;
using System.Collections.ObjectModel;

namespace ALGAE.Tests.ViewModels;

public class GamesViewModelTests
{
    [Fact]
    public async Task LoadGamesAsync_WithValidGames_PopulatesGamesCollection()
    {
        // Arrange
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

        // Act
        await viewModel.LoadGamesCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(2, viewModel.Games.Count);
        Assert.Equal("Game 1", viewModel.Games[0].Name);
        Assert.Equal("Game 2", viewModel.Games[1].Name);
        Assert.Equal(2, viewModel.FilteredGames.Count);
        Assert.False(viewModel.IsEmpty);
    }

    [Fact]
    public async Task LoadGamesAsync_WithNoGames_SetsIsEmptyTrue()
    {
        // Arrange
        var mocker = new AutoMocker();
        mocker.GetMock<IGameRepository>()
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Game>());

        var viewModel = mocker.CreateInstance<GamesViewModel>();

        // Act
        await viewModel.LoadGamesCommand.ExecuteAsync(null);

        // Assert
        Assert.Empty(viewModel.Games);
        Assert.Empty(viewModel.FilteredGames);
        Assert.True(viewModel.IsEmpty);
    }

    [Fact]
    public void SearchText_UpdatesFilteredGames()
    {
        // Arrange
        var mocker = new AutoMocker();
        mocker.GetMock<IGameRepository>()
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Game>());

        var viewModel = mocker.CreateInstance<GamesViewModel>();
        
        // Add test games manually
        viewModel.Games.Add(new GameTestDataBuilder().WithName("Minecraft").Build());
        viewModel.Games.Add(new GameTestDataBuilder().WithName("World of Warcraft").Build());
        viewModel.Games.Add(new GameTestDataBuilder().WithName("Counter Strike").Build());

        // Act
        viewModel.SearchText = "craft";

        // Assert
        Assert.Equal(2, viewModel.FilteredGames.Count);
        Assert.True(viewModel.IsFiltered);
        Assert.Contains(viewModel.FilteredGames, g => g.Name == "Minecraft");
        Assert.Contains(viewModel.FilteredGames, g => g.Name == "World of Warcraft");
    }

    [Fact]
    public void ClearSearch_ResetsFilteredGames()
    {
        // Arrange
        var mocker = new AutoMocker();
        mocker.GetMock<IGameRepository>()
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Game>());

        var viewModel = mocker.CreateInstance<GamesViewModel>();
        
        viewModel.Games.Add(new GameTestDataBuilder().WithName("Game 1").Build());
        viewModel.Games.Add(new GameTestDataBuilder().WithName("Game 2").Build());
        viewModel.SearchText = "1"; // Apply filter first

        // Act
        viewModel.ClearSearchCommand.Execute(null);

        // Assert
        Assert.Empty(viewModel.SearchText);
        Assert.Equal(2, viewModel.FilteredGames.Count);
        Assert.False(viewModel.IsFiltered);
        Assert.False(viewModel.HasSearchText);
    }

    [Fact]
    public async Task LaunchGameWithProfileAsync_WithInvalidGame_ShowsValidationError()
    {
        // Arrange
        var mocker = new AutoMocker();
        var game = new GameTestDataBuilder()
            .WithInvalidExecutable()
            .Build();

        var validationResult = GameValidationResult.Failure("Game executable not specified");

        mocker.GetMock<IGameLaunchService>()
            .Setup(x => x.ValidateGameAsync(It.IsAny<Game>()))
            .ReturnsAsync(validationResult);

        mocker.GetMock<INotificationService>()
            .Setup(x => x.ShowWarningConfirmationAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .ReturnsAsync(false); // User cancels

        var viewModel = mocker.CreateInstance<GamesViewModel>();

        // Act
        await viewModel.LaunchGameWithProfileAsync(game, null);

        // Assert
        mocker.GetMock<IGameLaunchService>()
            .Verify(x => x.ValidateGameAsync(It.IsAny<Game>()), Times.Once);
        
        mocker.GetMock<INotificationService>()
            .Verify(x => x.ShowWarningConfirmationAsync(
                "Cannot Launch Game",
                It.Is<string>(msg => msg.Contains("Game executable not specified")),
                "Edit Game",
                "Cancel"), Times.Once);
        
        // Should not attempt to launch
        mocker.GetMock<IGameLaunchService>()
            .Verify(x => x.LaunchGameAsync(It.IsAny<Game>()), Times.Never);
    }

    [Fact]
    public async Task LaunchGameWithProfileAsync_WithValidGame_LaunchesSuccessfully()
    {
        // Arrange
        var mocker = new AutoMocker();
        var game = new GameTestDataBuilder().Build();

        var validationResult = GameValidationResult.Success();
        var launchResult = GameLaunchResult.Successful(null!); // We don't need an actual process for the test

        mocker.GetMock<IGameLaunchService>()
            .Setup(x => x.ValidateGameAsync(It.IsAny<Game>()))
            .ReturnsAsync(validationResult);

        mocker.GetMock<IGameLaunchService>()
            .Setup(x => x.LaunchGameAsync(It.IsAny<Game>()))
            .ReturnsAsync(launchResult);

        var viewModel = mocker.CreateInstance<GamesViewModel>();

        // Act
        await viewModel.LaunchGameWithProfileAsync(game, null);

        // Assert
        mocker.GetMock<IGameLaunchService>()
            .Verify(x => x.ValidateGameAsync(It.IsAny<Game>()), Times.Once);
        
        mocker.GetMock<IGameLaunchService>()
            .Verify(x => x.LaunchGameAsync(It.IsAny<Game>()), Times.Once);

        mocker.GetMock<INotificationService>()
            .Verify(x => x.ShowSuccess(It.Is<string>(msg => msg.Contains("Successfully launched"))), Times.Once);
    }

    [Fact]
    public async Task LaunchGameWithProfileAsync_WithProfile_UsesProfileArguments()
    {
        // Arrange
        var mocker = new AutoMocker();
        var game = new GameTestDataBuilder()
            .WithGameArgs("--default-args")
            .Build();

        var profile = new Profile
        {
            ProfileId = 1,
            ProfileName = "Test Profile",
            CommandLineArgs = "--profile-specific-args",
            GameId = game.GameId
        };

        var validationResult = GameValidationResult.Success();
        var launchResult = GameLaunchResult.Successful(null!);

        mocker.GetMock<IGameLaunchService>()
            .Setup(x => x.ValidateGameAsync(It.IsAny<Game>()))
            .ReturnsAsync(validationResult);

        mocker.GetMock<IGameLaunchService>()
            .Setup(x => x.LaunchGameAsync(It.IsAny<Game>()))
            .ReturnsAsync(launchResult);

        var viewModel = mocker.CreateInstance<GamesViewModel>();

        // Act
        await viewModel.LaunchGameWithProfileAsync(game, profile);

        // Assert
        mocker.GetMock<IGameLaunchService>()
            .Verify(x => x.ValidateGameAsync(It.Is<Game>(g => 
                g.GameArgs == "--profile-specific-args")), Times.Once);

        mocker.GetMock<ICompanionLaunchService>()
            .Verify(x => x.LaunchCompanionsForProfileAsync(profile.ProfileId), Times.Once);
    }
}
