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

        // Handle background click to close
        private void Background_MouseDown(object sender, MouseButtonEventArgs e)
        {
            object originalSource = e.OriginalSource;

            // If we're clicking in the background, but not on a button or interactive element
            if (!(originalSource is Button) &&
                !(originalSource is TextBlock) &&
                !(originalSource is Image) &&
                !(originalSource is ToggleButton) &&
                !IsChildOfInteractiveElement(originalSource as DependencyObject))
            {
                BackToMainRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void ScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            object originalSource = e.OriginalSource;

            // Only handle clicks on the ScrollViewer itself, not its contents
            if (originalSource is ScrollViewer && !IsChildOfModItem(e.OriginalSource as DependencyObject))
            {
                BackToMainRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void ModsPanel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // If clicking directly on the background StackPanel and not on any mod item
            if (e.OriginalSource is StackPanel && sender == e.OriginalSource)
            {
                BackToMainRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        // Show the install mod panel
        private void InstallModButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous inputs
            ModNameTextBox.Text = string.Empty;
            ModPathTextBox.Text = string.Empty;
            selectedModPath = string.Empty;

            // Show the panel
            InstallModPanel.Visibility = Visibility.Visible;

            // Focus on the name textbox
            ModNameTextBox.Focus();
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

        // Cancel button click handler
        private void CancelInstallMod_Click(object sender, RoutedEventArgs e)
        {
            InstallModPanel.Visibility = Visibility.Collapsed;
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
            InstallModPanel.Visibility = Visibility.Collapsed;
        }

        // Helper method to check if an element is inside an interactive element (button, checkbox, etc.)
        private bool IsChildOfInteractiveElement(DependencyObject element)
        {
            if (element == null)
                return false;

            // Walk up the visual tree
            DependencyObject parent = element;
            while (parent != null)
            {
                if (parent is Button || parent is ToggleButton ||
                    parent is TextBox || parent is ComboBox ||
                    (parent is Border && FindParentOfType<Button>(parent) != null))
                    return true;

                parent = VisualTreeHelper.GetParent(parent);
            }

            return false;
        }

        // Helper method to check if an element is part of a mod item
        private bool IsChildOfModItem(DependencyObject element)
        {
            if (element == null)
                return false;

            // Walk up the visual tree
            DependencyObject parent = element;
            while (parent != null)
            {
                if (parent is Border border && border.Style != null &&
                    border.Style.TargetType == typeof(Border) &&
                    border.Style.Equals(FindResource("ModItemStyle")))
                    return true;

                parent = VisualTreeHelper.GetParent(parent);
            }

            return false;
        }

        // Helper method to find a parent of specific type
        private T FindParentOfType<T>(DependencyObject element) where T : DependencyObject
        {
            if (element == null)
                return null;

            DependencyObject parent = VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }
    }
}
