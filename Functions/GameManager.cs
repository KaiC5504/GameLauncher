using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Globalization;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfPoint = System.Windows.Point;
using System.Windows.Media;


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
            }
            catch (Exception)
            {
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
            catch (Exception )
            {
                //Error Handling
            }
        }
        public BitmapSource? ExtractIconFromExecutable(string executablePath)
        {
            try
            {
                // Try to extract using System.Drawing.Icon first
                using (Icon? icon = Icon.ExtractAssociatedIcon(executablePath))
                {
                    if (icon == null)
                    {
                        MessageBox.Show("Failed to extract icon (null icon returned)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }

                    // Convert the icon to a bitmap
                    using (Bitmap bitmap = icon.ToBitmap())
                    {
                        // Create a handle to the bitmap
                        IntPtr hBitmap = bitmap.GetHbitmap();

                        try
                        {
                            // Convert the bitmap to a BitmapSource using a more reliable method
                            BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap,
                                IntPtr.Zero,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());

                            // Freeze the BitmapSource to avoid cross-thread issues
                            if (bitmapSource.CanFreeze)
                                bitmapSource.Freeze();

                            return bitmapSource;
                        }
                        finally
                        {
                            // Delete the bitmap handle to avoid memory leaks
                            DeleteObject(hBitmap);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Try an alternative approach using Shell32
                try
                {
                    // Alternative icon extraction using a WPF-specific approach
                    BitmapSource? iconSource = null;
                    var sysicon = System.Drawing.SystemIcons.Application;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        sysicon.ToBitmap().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        ms.Position = 0;

                        BitmapImage bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.StreamSource = ms;
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        if (bmp.CanFreeze)
                            bmp.Freeze();

                        iconSource = bmp;
                    }

                    return iconSource;
                }
                catch (Exception fallbackEx)
                {
                    MessageBox.Show($"Fallback icon extraction also failed: {fallbackEx.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
        }

        // Import DeleteObject from gdi32.dll to clean up the HBitmap
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        // Add a new game
        public async Task<bool> AddGame(string name, string executablePath, string? iconPath = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(executablePath))
                    return false;

                if (!File.Exists(executablePath))
                {
                    MessageBox.Show($"Executable file not found: {executablePath}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Extract icon from executable with improved error handling
                var iconSource = ExtractIconFromExecutable(executablePath);

                if (iconSource == null)
                {
                    MessageBox.Show("Failed to extract icon from executable. Using default icon.",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // Create a default icon (blue square)
                    DrawingVisual visual = new DrawingVisual();
                    using (DrawingContext context = visual.RenderOpen())
                    {
                        // Fix ambiguous reference issues
                        context.DrawRectangle(WpfBrushes.RoyalBlue, null, new Rect(0, 0, 64, 64));
                        context.DrawText(
                            new FormattedText(
                                name.Substring(0, Math.Min(2, name.Length)).ToUpper(),
                                CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                new Typeface("Segoe UI"),
                                30,
                                WpfBrushes.White, // Fix ambiguous reference
                                1.25),
                            new WpfPoint(10, 10)); // Fix ambiguous reference
                    }

                    RenderTargetBitmap rtb = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(visual);
                    if (rtb.CanFreeze)
                        rtb.Freeze();

                    iconSource = rtb;
                }

                // Create a new game info object
                var game = new GameInfo
                {
                    Name = name,
                    ExecutablePath = executablePath,
                    IconPath = iconPath,
                    IconSource = iconSource,  // Store the extracted icon
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
                MessageBox.Show($"Error adding game: {ex.Message}\n{ex.StackTrace}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // Game information class
        public class GameInfo
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string? Name { get; set; }
            public string? ExecutablePath { get; set; }
            public string? IconPath { get; set; }

            private BitmapSource? _iconSource;
            public BitmapSource? IconSource
            {
                get => _iconSource;
                set => _iconSource = value;
            }

            public BitmapImage? GameIcon => string.IsNullOrEmpty(IconPath) ? null : new BitmapImage(new Uri(IconPath));
            public TimeSpan PlayTime { get; set; }
            public DateTime LastPlayed { get; set; }
            public DateTime DateAdded { get; set; }
        }

    }
}
