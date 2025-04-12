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

namespace GameLauncher
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
    }

}