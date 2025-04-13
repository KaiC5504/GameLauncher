using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
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

        public GameManagementView(ObservableCollection<GameInfo> gameList)
        {
            InitializeComponent();
            games = gameList;
            GamesListView.ItemsSource = games;
        }

        private void GamesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedGame = GamesListView.SelectedItem as GameInfo;
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
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe",
                Title = "Select Game Executable"
            };

            if (dialog.ShowDialog() == true)
            {
                string executablePath = dialog.FileName;
                string gameName = System.IO.Path.GetFileNameWithoutExtension(executablePath);

                var inputDialog = new InputDialog("Enter Game Name", "Game Name:", gameName);
                if (inputDialog.ShowDialog() == true)
                {
                    if (!string.IsNullOrEmpty(inputDialog.Answer))
                    {
                        AddGame(inputDialog.Answer, executablePath);
                    }
                    else
                    {
                        MessageBox.Show("Game name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void AddGame(string name, string path)
        {
            bool success = await GameManager.Instance.AddGame(name, path, null);
            if (success)
            {
                MessageBox.Show($"Game '{name}' added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                GamesListView.Items.Refresh();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackToMainRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}