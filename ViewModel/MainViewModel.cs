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
using Microsoft.Win32;
using System.Windows;
using System.Diagnostics;
using System.Windows.Shell;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Threading;

namespace rex.ViewModel
{
    internal class MainViewModel : ViewModelBase
    {
        public ObservableCollection<RegistryEntry> Entries { get; set; }

        public bool searchInactive = true;
        public bool SearchInactive {
            get { return searchInactive; }
            set {
                searchInactive = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            Entries = new ObservableCollection<RegistryEntry>();
            FetchRegistryEntriesAsync();
        }

        public ObservableCollection<bool> UsedRootKeys { get; set; }

        List<RegistryKey> RootKeys = [
            Registry.ClassesRoot,
            Registry.CurrentUser,
            Registry.LocalMachine,
            Registry.Users,
            Registry.CurrentConfig
        ];

        public RelayCommand OpenAboutCommand => new(execute => OpenAbout());
        public RelayCommand LoadDataCommand => new(async (execute) => await FetchRegistryEntries());

        public MainViewModel()
        {
            Entries = [];
            UsedRootKeys = [false, false, false, false, false];
        }

        private async Task FetchRegistryEntries()
        {
            SearchInactive = false;
            Entries.Clear();
            LoadingProgress = 0;
            List<RegistryKey> rootKeys = RootKeys
                .Zip(UsedRootKeys, (obj, used) => new { obj, used })
                .Where(x => x.used)
                .Select(x => x.obj)
                .ToList();

            foreach (RegistryKey rootKey in rootKeys)
            {
                await Task.Run(() => RecursiveRegistryValueCollector(rootKey, ""));
                LoadingProgress += 100 / rootKeys.Count;
            }
            SearchInactive = true;
        }

        private void RecursiveRegistryValueCollector(RegistryKey baseKey, string subKey)
            {
            try
            {
            using (RegistryKey key = baseKey.OpenSubKey(subKey))
                {
                if (key != null)
                    {
                    foreach (string valueName in key.GetValueNames())
                        {
                        Application.Current.Dispatcher.Invoke(() => Entries.Add(new RegistryEntry(key, valueName)));
                    }

                    foreach (string subKeyName in key.GetSubKeyNames())
                        {
                        RecursiveRegistryValueCollector(key, subKeyName);
                }
            }
                }
            }
            catch (System.Security.SecurityException)
            {
                Debug.WriteLine($"Not allowed to open key {subKey}");
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                ReList.ForEach(Entries.Add);
            });
        }

        private void OpenAbout()
        {
            HelpWindow w = new();
            w.ShowDialog();
        }
    }
}
