using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.IO;

namespace GameLauncher.Functions
{
    public class GameManager
    {
        // Singleton pattern for GameManager
        private static GameManager? _instance;
        public static GameManager Instance => _instance ??= new GameManager();

        // Collection of games that will be displayed in the UI
        public ObservableCollection<GameInfo> Games { get; private set; }

        // Constructor
        private GameManager()
        {
            Games = new ObservableCollection<GameInfo>();
            LoadGames();
        }

        // Add a new game
        public async Task<bool> AddGame(string name, string executablePath, string? iconPath = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(executablePath))
                    return false;

                if (!File.Exists(executablePath))
                    return false;

                // Create a new game info object
                var game = new GameInfo
                {
                    Name = name,
                    ExecutablePath = executablePath,
                    IconPath = iconPath,
                    PlayTime = TimeSpan.Zero,
                    DateAdded = DateTime.Now
                };

                // Add to collection
                Games.Add(game);

                // Save games to storage
                await SaveGamesAsync();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // Launch a game
        public async Task<bool> LaunchGameAsync(string gameId)
        {
            try
            {
                var game = Games.FirstOrDefault(g => g.Id == gameId);
                if (game == null)
                    return false;

                // Start the process
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = game.ExecutablePath;
                process.Start();

                // Update last played time
                game.LastPlayed = DateTime.Now;

                // Start tracking play time
                var startTime = DateTime.Now;

                // Wait for the process to exit
                await Task.Run(() => process.WaitForExit());

                // Update play time
                var playSession = DateTime.Now - startTime;
                game.PlayTime += playSession;

                // Save changes
                await SaveGamesAsync();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // Remove a game from the collection
        public async Task<bool> RemoveGameAsync(string gameId)
        {
            var game = Games.FirstOrDefault(g => g.Id == gameId);
            if (game == null)
                return false;

            Games.Remove(game);
            await SaveGamesAsync();
            return true;
        }

        // Get play time for a specific game
        public TimeSpan GetPlayTime(string gameId)
        {
            var game = Games.FirstOrDefault(g => g.Id == gameId);
            return game?.PlayTime ?? TimeSpan.Zero;
        }

        // Load games from storage
        private void LoadGames()
        {
            try
            {
                // This is a placeholder - you'll need to implement your own storage solution
                // Options: JSON file, SQLite database, application settings, etc.

                // For testing, add some sample games
                Games.Add(new GameInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Sample Game 1",
                    ExecutablePath = @"C:\Games\SampleGame1\game.exe",
                    PlayTime = TimeSpan.FromHours(2.5),
                    LastPlayed = DateTime.Now.AddDays(-2),
                    DateAdded = DateTime.Now.AddMonths(-1)
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading games: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Save games to storage
        private async Task SaveGamesAsync()
        {
            try
            {
                // This is a placeholder - implement your own storage solution
                await Task.Delay(100); // Simulate saving
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving games: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Game information class
    public class GameInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? Name { get; set; }
        public string? ExecutablePath { get; set; }
        public string? IconPath { get; set; }
        public BitmapImage? GameIcon => string.IsNullOrEmpty(IconPath) ? null : new BitmapImage(new Uri(IconPath));
        public TimeSpan PlayTime { get; set; }
        public DateTime LastPlayed { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
