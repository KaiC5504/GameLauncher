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

namespace GameLauncher.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Set the window startup location to center screen
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

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
        private void AddGameButton(object sender, RoutedEventArgs e)
        {
            ShowAddGameDialog();
        }
        private async void ShowAddGameDialog()
        {
            // Create a simple dialog for adding games
            // This is a basic example - you might want to create a more sophisticated UI

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe",
                Title = "Select Game Executable"
            };

            if (dialog.ShowDialog() == true)
            {
                string executablePath = dialog.FileName;
                string gameName = System.IO.Path.GetFileNameWithoutExtension(executablePath);

                // Optionally let user select an icon
                string? iconPath = null;

                // Ask user for game name
                var inputDialog = new InputDialog("Enter Game Name", "Game Name:", gameName);
                if (inputDialog.ShowDialog() == true)
                {
                    gameName = inputDialog.Answer;

                    // Add the game using GameManager
                    bool success = await GameManager.Instance.AddGame(gameName, executablePath, iconPath);

                    if (success)
                    {
                        MessageBox.Show($"Game '{gameName}' added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        // Update your UI as needed
                    }
                }
            }
        }

        // Simple input dialog class
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

}