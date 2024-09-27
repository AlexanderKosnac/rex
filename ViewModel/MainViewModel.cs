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

namespace rex.ViewModel
{
    internal class MainViewModel : ViewModelBase
    {
        public ObservableCollection<RegistryEntry> Entries { get; set; }

        public RelayCommand OpenAboutCommand => new RelayCommand(execute => OpenAbout());

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
        public RelayCommand LoadDataCommand => new(execute => FetchRegistryEntries());

        public MainViewModel()
        {
            Entries = [];
            UsedRootKeys = [false, false, false, false, false];
        }

        private void FetchRegistryEntries()
        {
            Entries.Clear();
            LoadingProgress = 0;
            List<RegistryKey> rootKeys = RootKeys
                .Zip(UsedRootKeys, (obj, used) => new { obj, used })
                .Where(x => x.used)
                .Select(x => x.obj)
                .ToList();

            try
            {
                using (RegistryKey currentKey = string.IsNullOrEmpty(subKeyPath) ? rootKey : rootKey.OpenSubKey(subKeyPath))
                {
                    if (currentKey != null)
                    {
                        foreach (string subKeyName in currentKey.GetSubKeyNames())
                        {
                            string fullSubKeyPath = subKeyPath == "" ? subKeyName : $"{subKeyPath}\\{subKeyName}";
                            TraverseRegistryKeys(rootKey, fullSubKeyPath);
                        });

                        foreach (string valueName in currentKey.GetValueNames())
                        {
                            object value = currentKey.GetValue(valueName);
                            ReList.Add(new RegistryEntry(currentKey, valueName));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing subkey: {subKeyPath}, Exception: {ex.Message}");
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
