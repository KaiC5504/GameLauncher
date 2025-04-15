using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GameLauncher.UI
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ModdingView : UserControl
    {
        // Event to notify MainWindow when the modding view should be closed
        public event EventHandler BackToMainRequested;

        private string selectedModPath;

        public ModdingView()
        {
            InitializeComponent();
        }

        // Handle closing the modding view
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            BackToMainRequested?.Invoke(this, EventArgs.Empty);
        }

        // Show the install mod panel
        private void InstallModButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous inputs
            ModNameTextBox.Text = string.Empty;
            ModPathTextBox.Text = string.Empty;
            selectedModPath = string.Empty;

            // Show the panel
            InstallModPanel.IsOpen = true;

            // Focus on the name textbox
            ModNameTextBox.Focus();
        }

        // Cancel button click handler
        private void CancelInstallMod_Click(object sender, RoutedEventArgs e)
        {
            InstallModPanel.IsOpen = false;
        }

        // Confirm install mod button click handler
        private void ConfirmInstallMod_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ModNameTextBox.Text))
            {
                MessageBox.Show("Please enter a mod name.", "Missing Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                ModNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedModPath))
            {
                MessageBox.Show("Please select a mod file.", "Missing File", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: Implement mod installation logic here
            MessageBox.Show($"Mod '{ModNameTextBox.Text}' would be installed from: {selectedModPath}",
                          "Mod Installation", MessageBoxButton.OK, MessageBoxImage.Information);

            // Hide the panel
            InstallModPanel.IsOpen = false;
        }

        // Browse button click handler
        private void BrowseModButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Mod Files (*.zip;*.dll;*.pak)|*.zip;*.dll;*.pak|All files (*.*)|*.*",
                Title = "Select Mod File"
            };

            if (dialog.ShowDialog() == true)
            {
                selectedModPath = dialog.FileName;
                ModPathTextBox.Text = selectedModPath;

                // Auto-fill mod name from file if empty
                if (string.IsNullOrEmpty(ModNameTextBox.Text))
                {
                    ModNameTextBox.Text = System.IO.Path.GetFileNameWithoutExtension(selectedModPath);
                    ModNameTextBox.SelectAll();
                    ModNameTextBox.Focus();
                }
            }
        }
      

    }
}
