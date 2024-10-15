using System.Windows;
using rex.ViewModel;

namespace rex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainViewModel vm = new();
            DataContext = vm;
        }
    }
}