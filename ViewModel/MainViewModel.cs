using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using rex.Core.DataStructure;
using rex.Model;
using rex.MVVM;
using rex.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Security;
using System.Text;
using System.Windows;

namespace rex.ViewModel
{
    internal partial class MainViewModel : ObservableObject
    {
        public ObservableCollection<RegistryEntry> Entries { get; set; }

        private CancellationTokenSource? tokenSource;

        [ObservableProperty]
        public string pathSearch = "";

        [ObservableProperty]
        public string nameSearch = "";

        [ObservableProperty]
        public string valueSearch = "";

        public List<RegistryValueKind> kindsSearch = [];

        [ObservableProperty]
        public bool searchActive = false;

        [ObservableProperty]
        private int loadingProgress = 0;

        [ObservableProperty]
        private int maxValues = 0;

        public ObservableCollection<RegistryValueKindItem> ValueKinds { get; set; } = [
            new(RegistryValueKind.None, true),
            new(RegistryValueKind.Unknown, true),
            new(RegistryValueKind.String, true),
            new(RegistryValueKind.ExpandString, true),
            new(RegistryValueKind.Binary, true),
            new(RegistryValueKind.DWord, true),
            new(RegistryValueKind.MultiString, true),
            new(RegistryValueKind.QWord, true),
        ];

        public ObservableCollection<RegistryKeyItem> RootKeys { get; set; } = [
            new("HKEY_CLASSES_ROOT", Registry.ClassesRoot, false),
            new("HKEY_CURRENT_USER", Registry.CurrentUser, false),
            new("HKEY_LOCAL_MACHINE", Registry.LocalMachine, false),
            new("HKEY_USERS", Registry.Users, false),
            new("HKEY_CURRENT_CONFIG", Registry.CurrentConfig, false),
        ];

        public RelayCommand OpenAboutCommand => new(execute => OpenAbout());
        public RelayCommand ExportDataCommand => new(execute => ExportData());
        public RelayCommand LoadDataCommand => new(async (execute) => await FetchRegistryEntries(), canExecute => !SearchActive);
        public RelayCommand CancelSearchCommand => new(execute => CancelSearch(), canExecute => SearchActive);

        public MainViewModel()
        {
            Entries = [];
        }

        private async Task FetchRegistryEntries()
        {
            tokenSource = new();
            SearchActive = true;

            Entries.Clear();
            LoadingProgress = 0;
            MaxValues = 0;
            kindsSearch = ValueKinds.Where(k => k.IsSelected).Select(k => k.Object).ToList();

            List<Task> tasks = [];
            foreach (RegistryKeyItem key in RootKeys)
            {
                if (!key.IsSelected)
                    continue;
                Task task = Task.Run(() => RecursiveRegistryValueCollector(key.Object, "", tokenSource.Token));
                tasks.Add(task);
            }
            while (tasks.Count > 0)
            {
                Task done = await Task.WhenAny(tasks);
                tasks.Remove(done);
                LoadingProgress += 100 / RootKeys.Count;
            }

            tokenSource.Dispose();
            tokenSource = null;
            SearchActive = false;
        }

        private void CancelSearch()
        {
            tokenSource?.Cancel();
        }

        private void RecursiveRegistryValueCollector(RegistryKey baseKey, string subKey, CancellationToken token)
        {
            if (OpenSubKeyOrNull(baseKey, subKey) is not RegistryKey key)
                return;

            foreach (string valueName in key.GetValueNames())
            {
                if (token.IsCancellationRequested)
                    return;

                RegistryEntry re = new(key, valueName);
                MaxValues++;

                bool matchesByPath = PathSearch == "" || re.KeyPath.Contains(PathSearch);
                bool matchesByName = NameSearch == "" || re.ValueName.Contains(NameSearch);
                bool matchesByValue = ValueSearch == "" || re.Value.Contains(ValueSearch);
                bool matchesByKind = kindsSearch.Contains(re.Kind);
                if (matchesByPath && matchesByName && matchesByValue && matchesByKind)
                {
                    Application.Current.Dispatcher.InvokeAsync(() => Entries.Add(re));
                }
            }

            foreach (string subKeyName in key.GetSubKeyNames())
            {
                RecursiveRegistryValueCollector(key, subKeyName, token);
            }
        }

        private RegistryKey? OpenSubKeyOrNull(RegistryKey baseKey, string subKey)
        {
            try
            {
                return baseKey.OpenSubKey(subKey);
            }
            catch (SecurityException)
            {
                return null;
            }
        }

        private void ExportData()
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "CSV file (*.csv)|*.csv",
                FileName = "export.csv"
            };

            if (saveFileDialog.ShowDialog() is bool r && r)
            {
                ExportRegistryEntriesToCsv([.. Entries], saveFileDialog.FileName);
            }
        }

        private void OpenAbout()
        {
            new HelpWindow().ShowDialog();
        }

        private static void ExportRegistryEntriesToCsv(List<RegistryEntry> Entries, string filePath)
        {
            StringBuilder sb = new();
            sb.AppendLine("Path,Name,Value,Kind");
            foreach (RegistryEntry re in Entries)
                sb.AppendLine(re.ToCSV());
            File.WriteAllText(filePath, sb.ToString());
        }
    }
}