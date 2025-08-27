using Algae.DAL.Models;

namespace ALGAE.Tests.TestData;

public class GameTestDataBuilder
{
    private Game _game = new()
    {
        GameId = 1,
        Name = "Test Game",
        ShortName = "TestGame",
        Description = "A test game for unit testing",
        InstallPath = @"C:\Games\TestGame",
        ExecutableName = "TestGame.exe",
        GameArgs = "--test-mode",
        GameWorkingPath = @"C:\Games\TestGame",
        Version = "1.0.0",
        Publisher = "Test Publisher",
        ThemeName = "Default",
        GameImage = null
    };

    public GameTestDataBuilder WithId(int id)
    {
        _game.GameId = id;
        return this;
    }

    public GameTestDataBuilder WithName(string name)
    {
        _game.Name = name;
        _game.ShortName = name.Replace(" ", "");
        return this;
    }

    public GameTestDataBuilder WithInstallPath(string installPath)
    {
        _game.InstallPath = installPath;
        return this;
    }

    public GameTestDataBuilder WithExecutableName(string executableName)
    {
        _game.ExecutableName = executableName;
        return this;
    }

    public GameTestDataBuilder WithGameArgs(string gameArgs)
    {
        _game.GameArgs = gameArgs;
        return this;
    }

    public GameTestDataBuilder WithWorkingPath(string workingPath)
    {
        _game.GameWorkingPath = workingPath;
        return this;
    }

    public GameTestDataBuilder WithInvalidExecutable()
    {
        _game.ExecutableName = null;
        return this;
    }

    public GameTestDataBuilder WithInvalidPath()
    {
        _game.InstallPath = @"C:\NonExistentPath";
        return this;
    }

    public Game Build() => _game;

    public static implicit operator Game(GameTestDataBuilder builder) => builder.Build();
}
