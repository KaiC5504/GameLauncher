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
using System.Runtime.InteropServices;


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
                // Initialize COM for the current thread
                CoInitialize(IntPtr.Zero);

                // Create a ShellItem for the executable
                Guid shellItemGuid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"); // IShellItem GUID
                if (SHCreateItemFromParsingName(executablePath, IntPtr.Zero, ref shellItemGuid, out IShellItem? shellItem) != 0 || shellItem == null)
                {
                    MessageBox.Show("Failed to create ShellItem for the executable.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                // Get the IShellItemImageFactory interface
                Guid imageFactoryGuid = new Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B"); // IShellItemImageFactory GUID
                if (shellItem is not IShellItemImageFactory imageFactory)
                {
                    MessageBox.Show("Failed to get IShellItemImageFactory interface.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                // Request a 256x256 icon
                SIZE size = new SIZE { cx = 256, cy = 256 };
                IntPtr hBitmap = IntPtr.Zero;
                int hr = imageFactory.GetImage(size, SIIGBF.SIIGBF_BIGGERSIZEOK, out hBitmap);

                if (hr != 0 || hBitmap == IntPtr.Zero)
                {
                    MessageBox.Show("Failed to extract high-resolution icon.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                try
                {
                    // Convert the HBITMAP to a BitmapSource
                    BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    // Apply rounded corners to the bitmap
                    return ApplyRoundedCorners(bitmapSource, 30);
                }
                finally
                {
                    // Clean up the HBITMAP
                    DeleteObject(hBitmap);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting high-resolution icon: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            finally
            {
                // Uninitialize COM for the current thread
                CoUninitialize();
            }
        }
        private BitmapSource ApplyRoundedCorners(BitmapSource source, double cornerRadius)
        {
            if (source == null) return null;

            // Create a drawing visual
            DrawingVisual drawingVisual = new DrawingVisual();

            // Get the source dimensions
            double width = source.PixelWidth;
            double height = source.PixelHeight;

            // Create a render target bitmap to draw on
            RenderTargetBitmap result = new RenderTargetBitmap(
                (int)width, (int)height,
                96, 96,
                PixelFormats.Pbgra32);

            // Draw the rounded rectangle with the image as a brush
            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                // Create a rectangle with rounded corners
                RectangleGeometry clipGeometry = new RectangleGeometry(
                    new Rect(0, 0, width, height),
                    cornerRadius, cornerRadius);

                // Apply the clipping geometry
                dc.PushClip(clipGeometry);

                // Draw the original image
                dc.DrawImage(source, new Rect(0, 0, width, height));

                // Remove the clip
                dc.Pop();
            }

            // Render the visual to the bitmap
            result.Render(drawingVisual);

            // Freeze the bitmap to improve performance
            if (result.CanFreeze)
                result.Freeze();

            return result;
        }

        // P/Invoke declarations
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem? ppv);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("ole32.dll")]
        private static extern void CoInitialize(IntPtr pvReserved);

        [DllImport("ole32.dll")]
        private static extern void CoUninitialize();

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
        private interface IShellItemImageFactory
        {
            [PreserveSig]
            int GetImage(SIZE size, SIIGBF flags, out IntPtr phbm);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        private interface IShellItem
        {
        }

        private enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0x00,
            SIIGBF_BIGGERSIZEOK = 0x01,
            SIIGBF_MEMORYONLY = 0x02,
            SIIGBF_ICONONLY = 0x04,
            SIIGBF_THUMBNAILONLY = 0x08,
            SIIGBF_INCACHEONLY = 0x10
        }

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
