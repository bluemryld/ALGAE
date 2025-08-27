using ALGAE.Services;
using ALGAE.Tests.TestData;
using Algae.DAL.Models;

namespace ALGAE.Tests.Services;

public class GameLaunchServiceTests
{
    [Fact]
    public async Task ValidateGameAsync_WithValidGame_ReturnsValid()
    {
        // Arrange
        var mocker = new AutoMocker();
        var service = mocker.CreateInstance<GameLaunchService>();
        var game = new GameTestDataBuilder()
            .WithName("Valid Game")
            .WithInstallPath(Path.GetTempPath()) // Use temp path that exists
            .WithExecutableName("notepad.exe") // Use Windows notepad as a known executable
            .Build();

        // Create a dummy executable file for testing
        var executablePath = Path.Combine(game.InstallPath, game.ExecutableName ?? "test.exe");
        await File.WriteAllTextAsync(executablePath, "dummy");

        try
        {
            // Act
            var result = await service.ValidateGameAsync(game);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Warnings);
            Assert.Null(result.ErrorMessage);
        }
        finally
        {
            // Cleanup
            if (File.Exists(executablePath))
                File.Delete(executablePath);
        }
    }

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

    [Fact]
    public async Task ValidateGameAsync_WithNonExistentPath_ReturnsInvalid()
    {
        // Arrange
        var mocker = new AutoMocker();
        var service = mocker.CreateInstance<GameLaunchService>();
        var game = new GameTestDataBuilder()
            .WithInvalidPath()
            .WithExecutableName("nonexistent.exe")
            .Build();

        // Act
        var result = await service.ValidateGameAsync(game);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateGameAsync_WithNullExecutableName_ReturnsInvalid()
    {
        // Arrange
        var mocker = new AutoMocker();
        var service = mocker.CreateInstance<GameLaunchService>();
        var game = new GameTestDataBuilder()
            .WithInvalidExecutable() // Sets ExecutableName to null
            .Build();

        // Act
        var result = await service.ValidateGameAsync(game);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("executable", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateGameAsync_WithEmptyExecutableName_ReturnsInvalid(string executableName)
    {
        // Arrange
        var mocker = new AutoMocker();
        var service = mocker.CreateInstance<GameLaunchService>();
        var game = new GameTestDataBuilder()
            .WithExecutableName(executableName)
            .Build();

        // Act
        var result = await service.ValidateGameAsync(game);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("executable", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LaunchGameAsync_WithValidGame_ReturnsSuccessResult()
    {
        // Arrange
        var mocker = new AutoMocker();
        var service = mocker.CreateInstance<GameLaunchService>();
        
        // Use notepad as a simple, reliable Windows executable for testing
        var game = new GameTestDataBuilder()
            .WithName("Notepad Test")
            .WithInstallPath(@"C:\Windows\System32")
            .WithExecutableName("notepad.exe")
            .WithGameArgs("")
            .Build();

        // Act
        var result = await service.LaunchGameAsync(game);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Process);
        Assert.Null(result.ErrorMessage);

        // Cleanup - close the process we just started
        if (result.Process != null && !result.Process.HasExited)
        {
            result.Process.Kill();
            result.Process.Dispose();
        }
    }

    [Fact]
    public async Task LaunchGameAsync_WithInvalidGame_ReturnsFailureResult()
    {
        // Arrange
        var mocker = new AutoMocker();
        var service = mocker.CreateInstance<GameLaunchService>();
        var game = new GameTestDataBuilder()
            .WithInvalidPath()
            .WithExecutableName("nonexistent.exe")
            .Build();

        // Act
        var result = await service.LaunchGameAsync(game);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Process);
        Assert.NotNull(result.ErrorMessage);
    }
}
