using rex.Model;
using rex.MVVM;
using rex.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace rex.ViewModel
{
    internal class MainViewModel : ViewModelBase
    {
        public ObservableCollection<RegistryEntry> Entries { get; set; }

        public RelayCommand OpenAboutCommand => new RelayCommand(execute => OpenAbout());

        public MainViewModel()
        {
            Entries = new ObservableCollection<RegistryEntry>();
        }

        private void OpenAbout()
        {
            HelpWindow w = new HelpWindow();
            w.ShowDialog();
        }
    }
}
