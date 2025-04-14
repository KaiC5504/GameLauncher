using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using GameLauncher.Functions;
using static GameLauncher.UI.MainWindow;
using GameInfo = GameLauncher.Functions.GameManager.GameInfo;

namespace GameLauncher.UI
{
    public partial class GameManagementView : UserControl
    {
        public event EventHandler<GameInfo> GameSelected;
        public event EventHandler<GameInfo> GameLaunched;
        public event EventHandler BackToMainRequested;

        private readonly ObservableCollection<GameInfo> games;
        private GameInfo selectedGame;
        private string selectedGamePath;

        public GameManagementView(ObservableCollection<GameInfo> gameList)
        {
            InitializeComponent();
            games = gameList;
            GamesListView.ItemsSource = games;
        }

        private void GameIcon_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is GameInfo game)
            {
                selectedGame = game;
                GameSelected?.Invoke(this, selectedGame);
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedGame != null)
            {
                GameSelected?.Invoke(this, selectedGame);
            }
            else
            {
                MessageBox.Show("Please select a game.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedGame != null)
            {
                GameLaunched?.Invoke(this, selectedGame);
            }
            else
            {
                MessageBox.Show("Please select a game to play.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddNewGameButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ShowAddGamePanel();
        }
        // Show the add game panel
        private void ShowAddGamePanel()
        {
            // Clear previous inputs
            GameNameTextBox.Text = string.Empty;
            GamePathTextBox.Text = string.Empty;
            selectedGamePath = string.Empty;

            // Show the panel
            AddGamePanel.Visibility = Visibility.Visible;

            // Focus on the name textbox
            GameNameTextBox.Focus();
        }

        // Browse button click handler
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe",
                Title = "Select Game Executable"
            };

            if (dialog.ShowDialog() == true)
            {
                selectedGamePath = dialog.FileName;
                GamePathTextBox.Text = selectedGamePath;

                // Auto-fill game name from executable if empty
                if (string.IsNullOrEmpty(GameNameTextBox.Text))
                {
                    GameNameTextBox.Text = System.IO.Path.GetFileNameWithoutExtension(selectedGamePath);
                    GameNameTextBox.SelectAll();
                    GameNameTextBox.Focus();
                }
            }
        }

        // Cancel button click handler
        private void CancelAddGame_Click(object sender, RoutedEventArgs e)
        {
            AddGamePanel.Visibility = Visibility.Collapsed;
        }

        // Confirm add game button click handler
        private void ConfirmAddGame_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GameNameTextBox.Text))
            {
                MessageBox.Show("Please enter a game name.", "Missing Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                GameNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedGamePath))
            {
                MessageBox.Show("Please select a game executable.", "Missing Executable", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Add the game
            AddGame(GameNameTextBox.Text, selectedGamePath);

            // Hide the panel
            AddGamePanel.Visibility = Visibility.Collapsed;
        }

        // Handle Enter key in the game name textbox
        private void GameNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!string.IsNullOrEmpty(selectedGamePath))
                {
                    ConfirmAddGame_Click(sender, e);
                }
                else
                {
                    BrowseButton_Click(sender, e);
                }
                e.Handled = true;
            }
        }

        private async void AddGame(string name, string path)
        {
            bool success = await GameManager.Instance.AddGame(name, path, null);
        }
        private void Background_MouseDown(object sender, MouseButtonEventArgs e)
        {
            object originalSource = e.OriginalSource;
            
            // If we're clicking in the background, but not on a button or interactive element
            if (!(originalSource is Button) && 
                !(originalSource is TextBlock) &&
                !(originalSource is Image) &&
                !IsChildOfButton(originalSource as DependencyObject))
            {
                BackToMainRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }
        
        // Modify ContentGrid_MouseDown to include debugging
        private void ScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            object originalSource = e.OriginalSource;
            
            // Check if we're clicking on a game item
            if (!(originalSource is Button) && 
                !(originalSource is TextBlock && ((TextBlock)originalSource).Text == "ADD GAME") &&
                !IsChildOfButton(originalSource as DependencyObject) &&
                !IsInsideAddGameButton(originalSource as DependencyObject))
            {
                // If not on a game item, return to main window
                BackToMainRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void WrapPanel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // If clicking directly on the WrapPanel background and not on any game item
            if ((e.OriginalSource is WrapPanel && sender == e.OriginalSource) || 
                (e.OriginalSource is Grid && ((Grid)e.OriginalSource).Name == "GameIconsPanel"))
            {
                BackToMainRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private bool IsInsideAddGameButton(DependencyObject element)
        {
            if (element == null)
                return false;
                
            // Walk up the visual tree
            DependencyObject parent = element;
            while (parent != null)
            {
                if (parent is Button button && button.Name == "AddNewGameButton")
                    return true;
                    
                parent = VisualTreeHelper.GetParent(parent);
            }
            
            return false;
        }
        
        // Helper method to check if an element is inside a button
        private bool IsChildOfButton(DependencyObject element)
        {
            if (element == null)
                return false;
                
            // Walk up the visual tree
            DependencyObject parent = element;
            while (parent != null)
            {
                if (parent is Button)
                    return true;
                    
                // Get the parent in the visual tree
                parent = VisualTreeHelper.GetParent(parent);
            }
            
            return false;
        }

        private void GamePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
