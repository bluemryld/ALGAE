using ALGAE.Services;
using ALGAE.Tests.TestData;
using System.Diagnostics;

namespace ALGAE.Tests.Services;

public class SimpleGameProcessMonitorServiceTests : IDisposable
{
    private readonly GameProcessMonitorService _service;

    public SimpleGameProcessMonitorServiceTests()
    {
        _service = new GameProcessMonitorService();
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Assert
        Assert.Null(_service.RunningGame);
        Assert.Null(_service.GameProcess);
        Assert.False(_service.HasRunningGame);
        Assert.Equal(TimeSpan.Zero, _service.SessionTime);
        Assert.Equal(0, _service.CpuUsage);
        Assert.Equal("0 MB", _service.MemoryUsage);
        Assert.Empty(_service.RecentSessions);
    }

    [Fact]
    public void StartMonitoring_WithValidProcess_StartsMonitoring()
    {
        // Arrange
        var game = new GameTestDataBuilder()
            .WithName("Test Game")
            .Build();

        var process = Process.Start("notepad.exe");
        
        try
        {
            Assert.NotNull(process);
            var gameStartedTriggered = false;
            _service.GameStarted += (sender, g) => gameStartedTriggered = true;

            // Act
            _service.StartMonitoring(game, process);

            // Assert
            Assert.Equal(game, _service.RunningGame);
            Assert.Equal(process, _service.GameProcess);
            Assert.True(_service.HasRunningGame);
            Assert.True(_service.SessionTime >= TimeSpan.Zero);
            Assert.True(gameStartedTriggered);
        }
        finally
        {
            try
            {
                process?.Kill();
                process?.WaitForExit(1000);
            }
            catch { }
        }
    }

    [Fact]
    public void StopMonitoring_WithRunningGame_StopsMonitoring()
    {
        // Arrange
        var game = new GameTestDataBuilder()
            .WithName("Test Game")
            .Build();

        var process = Process.Start("notepad.exe");
        
        try
        {
            Assert.NotNull(process);
            
            var gameStoppedTriggered = false;
            _service.GameStopped += (sender, g) => gameStoppedTriggered = true;

            _service.StartMonitoring(game, process);
            
            // Act
            _service.StopMonitoring();

            // Assert
            Assert.Null(_service.RunningGame);
            Assert.Null(_service.GameProcess);
            Assert.False(_service.HasRunningGame);
            Assert.Equal(TimeSpan.Zero, _service.SessionTime);
            Assert.True(gameStoppedTriggered);
            
            // Should have a session record
            Assert.Single(_service.RecentSessions);
            var session = _service.RecentSessions[0];
            Assert.Equal("Test Game", session.GameName);
            Assert.True(session.Duration > TimeSpan.Zero);
        }
        finally
        {
            try
            {
                process?.Kill();
                process?.WaitForExit(1000);
            }
            catch { }
        }
    }
}