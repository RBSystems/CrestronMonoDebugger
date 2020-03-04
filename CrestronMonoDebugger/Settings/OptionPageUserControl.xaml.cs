using System.Windows;
using System.Windows.Controls;

namespace CrestronMonoDebugger.Settings
{
    /// <summary>
    /// Interaction logic for OptionPageUserControl.xaml
    /// </summary>
    public partial class OptionPageUserControl : UserControl
    {
        private readonly OptionPageGrid _settings;

        public OptionPageUserControl(OptionPageGrid settings)
        {
            InitializeComponent();

            _settings = settings;
            DataContext = settings;
        }

        private void RestoreDefaults_OnClick(object sender, RoutedEventArgs e)
        {
            _settings.ResetSettings();
        }
    }
}
