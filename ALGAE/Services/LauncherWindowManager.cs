using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Algae.DAL.Models;
using ALGAE.Views;
using ALGAE.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ALGAE.Services
{
    public class LauncherWindowManager : ILauncherWindowManager, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<int, (Game Game, LauncherWindow Window)> _gameWindows;

        public event EventHandler? WindowsChanged;

        public LauncherWindowManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _gameWindows = new Dictionary<int, (Game, LauncherWindow)>();
        }

        public LauncherWindow GetOrCreateForGame(Game game)
        {
            if (_gameWindows.TryGetValue(game.GameId, out var existing))
            {
                return existing.Window;
            }

            // Create a new scoped launcher view model for this game
            var viewModel = _serviceProvider.GetRequiredService<LauncherViewModel>();
            viewModel.SetCurrentGame(game);

            var window = new LauncherWindow(viewModel, game);
            window.Closing += OnWindowClosing;

            _gameWindows[game.GameId] = (game, window);
            WindowsChanged?.Invoke(this, EventArgs.Empty);

            return window;
        }

        public bool TryGetForGame(int gameId, out LauncherWindow? window)
        {
            if (_gameWindows.TryGetValue(gameId, out var existing))
            {
                window = existing.Window;
                return true;
            }

            window = null;
            return false;
        }

        public IReadOnlyList<(int GameId, string GameName, LauncherWindow Window)> GetAll()
        {
            return _gameWindows.Values
                .Select(x => (x.Game.GameId, x.Game.Name, x.Window))
                .ToList()
                .AsReadOnly();
        }

        public void ShowWindowForGame(Game game)
        {
            var window = GetOrCreateForGame(game);
            window.ShowAndActivate();
        }

        public void CloseForGame(int gameId)
        {
            if (_gameWindows.TryGetValue(gameId, out var existing))
            {
                existing.Window.Close();
            }
        }

        public void CloseAll()
        {
            var windows = _gameWindows.Values.ToList();
            foreach (var (_, window) in windows)
            {
                window.Close();
            }
        }

        private void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            if (sender is LauncherWindow window)
            {
                // Find and remove the window
                var itemToRemove = _gameWindows.FirstOrDefault(kvp => kvp.Value.Window == window);
                if (itemToRemove.Key != 0)
                {
                    _gameWindows.Remove(itemToRemove.Key);
                    WindowsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void Dispose()
        {
            CloseAll();
            _gameWindows.Clear();
        }
    }
}
