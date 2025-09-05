using ALGAE.Services;
using ALGAE.DAL.Repositories;
using Algae.DAL.Models;
using Moq.AutoMock;

namespace ALGAE.Tests.Services;

public class SimpleGameDetectionServiceTests
{
    [Fact]
    public async Task GetCommonGameDirectoriesAsync_ReturnsExpectedPaths()
    {
        // Arrange
        var mocker = new AutoMocker();
        var service = mocker.CreateInstance<GameDetectionService>();

        // Act
        var result = await service.GetCommonGameDirectoriesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        // Should contain common game directory patterns
        Assert.Contains(result, path => path.Contains("Program Files"));
    }

    [Fact]
    public async Task GetSearchPathsAsync_ReturnsSearchPaths()
    {
        // Arrange
        var mocker = new AutoMocker();
        var searchPaths = new List<SearchPath>
        {
            new SearchPath { SearchPathId = 1, Path = @"C:\Games" },
            new SearchPath { SearchPathId = 2, Path = @"C:\Steam\steamapps\common" }
        };

        mocker.GetMock<ISearchPathRepository>()
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(searchPaths);

        var service = mocker.CreateInstance<GameDetectionService>();

        // Act
        var result = await service.GetSearchPathsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, sp => sp.Path == @"C:\Games");
        Assert.Contains(result, sp => sp.Path == @"C:\Steam\steamapps\common");
    }
}