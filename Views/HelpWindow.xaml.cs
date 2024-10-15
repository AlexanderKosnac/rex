using rex.ViewModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace rex.Views
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            HelpViewModel vm = new HelpViewModel();
            DataContext = vm;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void CloseHelp(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
