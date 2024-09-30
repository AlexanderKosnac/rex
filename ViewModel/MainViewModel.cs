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

        public string pathSearch = "";
        public string PathSearch
        {
            get { return pathSearch; }
            set
            {
                pathSearch = value;
                OnPropertyChanged();
            }
        }

        public string nameSearch = "";
        public string NameSearch
        {
            get { return nameSearch; }
            set
            {
                nameSearch = value;
                OnPropertyChanged();
            }
        }

        public string valueSearch = "";
        public string ValueSearch
        {
            get { return valueSearch; }
            set
            {
                valueSearch = value;
                OnPropertyChanged();
            }
        }

        public bool searchInactive = true;
        public bool SearchInactive {
            get { return searchInactive; }
            set {
                searchInactive = value;
                OnPropertyChanged();
            }
        }

        List<RegistryValueKind> kindsSearch;

        private int loadingProgress = 0;
        public int LoadingProgress
        {
            get { return loadingProgress; }
            set
            {
                loadingProgress = value;
                OnPropertyChanged();
            }
        }

        private int maxValues = 0;
        public int MaxValues
        {
            get { return maxValues; }
            set
            {
                maxValues = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<bool> UsedValueKinds { get; set; }

        readonly List<RegistryValueKind> ValueKinds = [
            RegistryValueKind.Binary,
            RegistryValueKind.MultiString,
            RegistryValueKind.None,
            RegistryValueKind.Unknown,
            RegistryValueKind.String,
            RegistryValueKind.ExpandString,
            RegistryValueKind.DWord,
            RegistryValueKind.QWord
        ];

        public ObservableCollection<bool> UsedRootKeys { get; set; }

        readonly List<RegistryKey> RootKeys = [
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
            UsedValueKinds = new(Enumerable.Repeat(true, ValueKinds.Count).ToList());
            UsedRootKeys = new(Enumerable.Repeat(false, RootKeys.Count).ToList());
        }

        private async Task FetchRegistryEntries()
        {
            SearchInactive = false;
            Entries.Clear();
            LoadingProgress = 0;
            kindsSearch = MainViewModel.GetSelectedItems(ValueKinds, UsedValueKinds.ToList());
            List<RegistryKey> rootKeys = MainViewModel.GetSelectedItems(RootKeys, UsedRootKeys.ToList());

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
                            RegistryEntry re = new(key, valueName);
                            MaxValues++;

                            bool matchesByPath = PathSearch == "" || re.KeyPath.Contains(PathSearch);
                            bool matchesByName = NameSearch == "" || re.ValueName.Contains(NameSearch);
                            bool matchesByValue = ValueSearch == "" || (re.Value ?? "NULL").ToString().Contains(ValueSearch);
                            bool matchesByKind = kindsSearch.Contains(re.Kind);
                            if (matchesByPath && matchesByName && matchesByValue && matchesByKind)
                            {
                                Application.Current.Dispatcher.Invoke(() => Entries.Add(re));
                            }
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
        }

        private void OpenAbout()
        {
            HelpWindow w = new();
            w.ShowDialog();
        }

        private static List<T> GetSelectedItems<T>(List<T> Items, List<bool> Selected)
        {
            return Items
                .Zip(Selected, (obj, used) => new { obj, used })
                .Where(x => x.used)
                .Select(x => x.obj)
                .ToList();
        }
    }
}