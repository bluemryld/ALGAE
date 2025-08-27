using System;
using System.Collections.Generic;
using Algae.DAL.Models;
using ALGAE.Views;

namespace ALGAE.Services
{
    public interface ILauncherWindowManager
    {
        event EventHandler? WindowsChanged;
        LauncherWindow GetOrCreateForGame(Game game);
        bool TryGetForGame(int gameId, out LauncherWindow? window);
        IReadOnlyList<(int GameId, string GameName, LauncherWindow Window)> GetAll();
        void ShowWindowForGame(Game game);
        void CloseForGame(int gameId);
        void CloseAll();
    }
}
