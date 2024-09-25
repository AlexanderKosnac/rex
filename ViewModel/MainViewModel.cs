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

        private void FetchRegistryEntriesAsync()
        {
            Entries.Clear();
            RegistryKey[] rootKeys = [
                Registry.ClassesRoot,
                Registry.CurrentUser,
                Registry.LocalMachine,
                Registry.Users,
                Registry.CurrentConfig
            ];

            foreach (RegistryKey rootKey in rootKeys)
            {
                Console.WriteLine($"Root Hive: {rootKey.Name}");
                TraverseRegistryKeys(rootKey, "");
            }
        }

        private void TraverseRegistryKeys(RegistryKey rootKey, string subKeyPath)
        {
            List<RegistryEntry> ReList = [];

            try
            {
                using (RegistryKey currentKey = subKeyPath == "" ? rootKey : rootKey.OpenSubKey(subKeyPath))
                {
                    if (currentKey != null)
                    {
                        foreach (string subKeyName in currentKey.GetSubKeyNames())
                        {
                            string fullSubKeyPath = subKeyPath == "" ? subKeyName : $"{subKeyPath}\\{subKeyName}";
                            TraverseRegistryKeys(rootKey, fullSubKeyPath); // Recursion call
                        }

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
            HelpWindow w = new HelpWindow();
            w.ShowDialog();
        }
    }
}
