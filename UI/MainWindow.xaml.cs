using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using GameLauncher.Functions;
using System.IO;
using System.Collections.ObjectModel;
using MaterialDesignThemes.Wpf;
using GameInfo = GameLauncher.Functions.GameManager.GameInfo;

namespace GameLauncher.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Reference to the game icon button in the top left
        private Button gameIconButton;
        private Image gameIconImage;
        private GameInfo? currentGame;

        // Content panels
        private Grid? mainContentGrid;
        private GameManagementView gameManagementView;
        private bool isGameManagementVisible = false;

        public MainWindow()
        {
            InitializeComponent();

            // Set the window startup location to center screen
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Reference the game icon button
            gameIconButton = (Button)FindName("GameIconButton");

            // Create the game icon image
            gameIconImage = new Image
            {
                Width = 55,
                Height = 55,
                Stretch = Stretch.Uniform
            };

            // Get reference to main content grid (the grid in the main border)
            mainContentGrid = FindMainContentGrid();

            // Initialize the game management view
            gameManagementView = new GameManagementView(GameManager.Instance.Games);
            gameManagementView.GameSelected += OnGameSelected;
            gameManagementView.GameLaunched += OnGameLaunched;
            gameManagementView.BackToMainRequested += OnBackToMainRequested;

            // Initially hide it
            gameManagementView.Visibility = Visibility.Collapsed;

            // Add the game management view to the main grid
            if (mainContentGrid != null)
            {
                mainContentGrid.Children.Add(gameManagementView);
                Grid.SetRow(gameManagementView, 0);
                Grid.SetRowSpan(gameManagementView, 2); // Span both rows
                gameManagementView.Margin = new Thickness(70, 0, 0, 0); // Same margin as other content
            }

            // Check if there are any games and update the icon accordingly
            UpdateGameIcon();

            // For proper rendering of rounded corners with WPF
            this.SourceInitialized += (s, e) =>
            {
                // Get the active screen's working area
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    // Set max height to screen height (without using Forms)
                    var screenHeight = SystemParameters.WorkArea.Height;
                    this.MaxHeight = screenHeight;
                }
            };
        }
        private Grid FindMainContentGrid()
        {
            // Find the main content grid where we'll add our game management view
            var mainBorder = (Border)FindName("MainBorder");
            if (mainBorder != null && mainBorder.Child is Grid grid)
            {
                return grid;
            }

            // If not found by name, try to find it in the visual tree
            var outerBorder = this.Content as Border;
            if (outerBorder != null)
            {
                // The main grid is inside this border
                if (outerBorder.Child is Grid outerGrid)
                {
                    // Look for the first Border inside the outer grid
                    foreach (var child in outerGrid.Children)
                    {
                        if (child is Border contentBorder && contentBorder.Child is Grid contentGrid)
                        {
                            return contentGrid;
                        }
                    }
                }
            }

            return null;
        }

        private void UpdateGameIcon()
        {
            try
            {
                // Check if we have games in our GameManager
                if (GameManager.Instance.Games.Count > 0)
                {
                    // Use the first game in the list as the current game
                    currentGame = GameManager.Instance.Games[0];

                    // Check if icon source exists
                    if (currentGame.IconSource == null)
                    {
                        // Try to extract the icon
                        var iconSource = GameManager.Instance.ExtractIconFromExecutable(currentGame.ExecutablePath ?? "");
                        if (iconSource != null)
                        {
                            // Update both the GameInfo object and our UI
                            currentGame.IconSource = iconSource;
                            gameIconImage.Source = iconSource;
                            gameIconButton.Content = gameIconImage;
                        }
                        else
                        {
                            // Create a fallback icon directly
                            CreateFallbackIcon();
                        }
                    }
                    else
                    {
                        // Update the game icon button to show the game icon
                        gameIconImage.Source = currentGame.IconSource;
                        gameIconButton.Content = gameIconImage;

                        // Force visual refresh
                        gameIconButton.UpdateLayout();
                    }

                    // Change the click handler to open the game library
                    gameIconButton.Click -= AddGameButton;
                    gameIconButton.Click += ShowGameLibraryButton;
                }
                else
                {
                    // No games, show the default "+" icon
                    gameIconButton.Content = new PackIcon
                    {
                        Kind = PackIconKind.PlusCircleOutline,
                        Width = 65,
                        Height = 65,
                        Foreground = Brushes.White
                    };

                    // Make sure the click handler is set to add a game
                    gameIconButton.Click -= ShowGameLibraryButton;
                    gameIconButton.Click += AddGameButton;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating game icon: {ex.Message}",
                    "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateFallbackIcon()
        {
            var drawingVisual = new DrawingVisual();
            using (DrawingContext context = drawingVisual.RenderOpen())
            {
                context.DrawRectangle(Brushes.DarkBlue, null, new Rect(0, 0, 55, 55));
                context.DrawText(
                    new FormattedText(
                        (currentGame.Name?.Substring(0, Math.Min(2, currentGame.Name?.Length ?? 0)) ?? "??").ToUpper(),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        24,
                        Brushes.White,
                        1.25),
                    new Point(10, 10));
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(55, 55, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(drawingVisual);
            if (rtb.CanFreeze)
                rtb.Freeze();

            gameIconImage.Source = rtb;
            gameIconButton.Content = gameIconImage;
        }

        // Event handlers for the GameManagementView
        private void OnGameSelected(object sender, GameInfo game)
        {
            currentGame = game;

            if (game.IconSource != null)
            {
                gameIconImage.Source = game.IconSource;
                gameIconButton.Content = gameIconImage;
            }

            ToggleGameManagementView(false); // Hide game management view
        }

        private async void OnGameLaunched(object sender, GameInfo game)
        {
            ToggleGameManagementView(false); // Hide game management view
            await GameManager.Instance.LaunchGameAsync(game.Id);
        }

        private void OnBackToMainRequested(object sender, EventArgs e)
        {
            ToggleGameManagementView(false);
        }

        private void ShowGameLibraryButton(object sender, RoutedEventArgs e)
        {
            ToggleGameManagementView(true);
        }

        private void AddGameButton(object sender, RoutedEventArgs e)
        {
            ToggleGameManagementView(true);
        }

        private void ToggleGameManagementView(bool show)
        {
            // Toggle visibility of main content elements vs game management view
            if (mainContentGrid != null)
            {
                foreach (UIElement child in mainContentGrid.Children)
                {
                    // Skip the game management view itself when setting visibility
                    if (child != gameManagementView)
                    {
                        child.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
                    }
                }
            }

            // Set game management view visibility
            if (gameManagementView != null)
            {
                gameManagementView.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

                // When showing the view, make sure it properly handles double-click
                if (show)
                {
                    // Remove and re-add the double click handler to ensure it's properly connected
                    if (gameManagementView.FindName("GamesListView") is ListView listView)
                    {
                        listView.MouseDoubleClick -= GameListView_MouseDoubleClick;
                        listView.MouseDoubleClick += GameListView_MouseDoubleClick;
                    }
                }
            }

            isGameManagementVisible = show;
        }
        private void GameListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is GameInfo selectedGame)
            {
                // Update the current game
                currentGame = selectedGame;

                if (selectedGame.IconSource != null)
                {
                    gameIconImage.Source = selectedGame.IconSource;
                    gameIconButton.Content = gameIconImage;
                }

                // Hide the game management view
                ToggleGameManagementView(false);

                // Optionally launch the game automatically on double-click
                // Uncomment if you want double-click to launch the game
                //GameManager.Instance.LaunchGameAsync(selectedGame.Id);
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                MaximizeIcon.Data = Geometry.Parse("M3,3H21V21H3V3M5,5V19H19V5H5Z"); // Restore Icon
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MaximizeIcon.Data = Geometry.Parse("M3,3H21V21H3V3"); // Maximize Icon
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        public async void ShowAddGameDialog()
        {
            // Instead of showing a dialog, just toggle to the game management view
            ToggleGameManagementView(true);
        }

        public class InputDialog : Window
        {
            private TextBox textBox;

            public string? Answer { get; private set; }

            public InputDialog(string title, string question, string defaultAnswer = "")
            {
                Title = title;
                Width = 300;
                Height = 150;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var label = new TextBlock { Text = question, Margin = new Thickness(10, 10, 10, 5) };
                Grid.SetRow(label, 0);

                textBox = new TextBox { Margin = new Thickness(10, 5, 10, 10), Text = defaultAnswer };
                Grid.SetRow(textBox, 1);

                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

                var okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 10, 10, 10), IsDefault = true };
                okButton.Click += (s, e) => { Answer = textBox.Text; DialogResult = true; };

                var cancelButton = new Button { Content = "Cancel", Width = 75, Margin = new Thickness(0, 10, 10, 10), IsCancel = true };

                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);
                Grid.SetRow(buttonPanel, 2);

                grid.Children.Add(label);
                grid.Children.Add(textBox);
                grid.Children.Add(buttonPanel);

                Content = grid;
            }
        }
    }
    public class GameLibraryDialog : Window
    {
        public GameInfo? SelectedGame { get; private set; }
        private ListView gameListView;
        private ObservableCollection<GameInfo> games;

        public GameLibraryDialog(ObservableCollection<GameInfo> gameList)
        {
            this.games = gameList;

            Title = "Game Library";
            Width = 600;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Simple Closing event handler - don't use isClosing flag
            this.Closing += (s, e) =>
            {
                if (!DialogResult.HasValue)
                {
                    DialogResult = false;
                }
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Create the game list view
            gameListView = new ListView
            {
                Margin = new Thickness(10),
                SelectionMode = SelectionMode.Single
            };

            // Set up the items template
            gameListView.ItemTemplate = CreateGameItemTemplate();
            gameListView.ItemsSource = games;
            gameListView.SelectionChanged += (s, e) =>
            {
                if (gameListView.SelectedItem != null)
                {
                    SelectedGame = (GameInfo)gameListView.SelectedItem;
                }
            };
            gameListView.MouseDoubleClick += (s, e) =>
            {
                if (SelectedGame != null)
                {
                    DialogResult = true;
                    Close();
                }
            };

            Grid.SetRow(gameListView, 0);
            grid.Children.Add(gameListView);

            // Buttons panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 10, 10)
            };

            var selectButton = new Button
            {
                Content = "Select",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                IsDefault = true
            };
            selectButton.Click += (s, e) =>
            {
                if (SelectedGame != null)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Please select a game.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };

            var addButton = new Button
            {
                Content = "Add Game",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };
            addButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
                // Delay the next dialog to ensure this one is properly closed
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Use the new method that's now properly defined
                    (Application.Current.MainWindow as MainWindow)?.ShowAddGameDialog();
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                IsCancel = true
            };
            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };

            var playButton = new Button
            {
                Content = "Play",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };
            playButton.Click += (s, e) =>
            {
                if (SelectedGame != null)
                {
                    DialogResult = true;
                    Close();
                    // Delay the game launch to ensure dialog is closed first
                    Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        await GameManager.Instance.LaunchGameAsync(SelectedGame.Id);
                    }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }
                else
                {
                    MessageBox.Show("Please select a game to play.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };

            buttonPanel.Children.Add(addButton);
            buttonPanel.Children.Add(playButton);
            buttonPanel.Children.Add(selectButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }

        private DataTemplate CreateGameItemTemplate()
        {
            var template = new DataTemplate();

            var factory = new FrameworkElementFactory(typeof(Grid));
            factory.SetValue(Grid.MarginProperty, new Thickness(5));

            var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(50));

            var col2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));

            var col3 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col3.SetValue(ColumnDefinition.WidthProperty, new GridLength(100));

            factory.AppendChild(col1);
            factory.AppendChild(col2);
            factory.AppendChild(col3);

            // Icon
            var iconFactory = new FrameworkElementFactory(typeof(Image));
            iconFactory.SetBinding(Image.SourceProperty, new Binding("IconSource"));
            iconFactory.SetValue(Image.WidthProperty, 40.0);
            iconFactory.SetValue(Image.HeightProperty, 40.0);
            iconFactory.SetValue(Image.StretchProperty, Stretch.Uniform);
            iconFactory.SetValue(Grid.ColumnProperty, 0);
            factory.AppendChild(iconFactory);

            // Game name and path
            var nameStackFactory = new FrameworkElementFactory(typeof(StackPanel));
            nameStackFactory.SetValue(Grid.ColumnProperty, 1);
            nameStackFactory.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
            nameStackFactory.SetValue(StackPanel.MarginProperty, new Thickness(10, 0, 0, 0));

            var nameTextFactory = new FrameworkElementFactory(typeof(TextBlock));
            nameTextFactory.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            nameTextFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            nameTextFactory.SetValue(TextBlock.FontSizeProperty, 14.0);

            var pathTextFactory = new FrameworkElementFactory(typeof(TextBlock));
            pathTextFactory.SetBinding(TextBlock.TextProperty, new Binding("ExecutablePath"));
            pathTextFactory.SetValue(TextBlock.FontSizeProperty, 11.0);
            pathTextFactory.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);

            nameStackFactory.AppendChild(nameTextFactory);
            nameStackFactory.AppendChild(pathTextFactory);
            factory.AppendChild(nameStackFactory);

            // Play time
            var timeFactory = new FrameworkElementFactory(typeof(TextBlock));
            timeFactory.SetValue(Grid.ColumnProperty, 2);
            timeFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            timeFactory.SetBinding(TextBlock.TextProperty, new Binding("PlayTime") { StringFormat = "{0:hh\\:mm}" });
            factory.AppendChild(timeFactory);

            template.VisualTree = factory;
            return template;
        }
    }

}