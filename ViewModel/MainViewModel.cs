using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
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

        [ObservableProperty]
        public bool searchActive = false;

        List<RegistryValueKind> kindsSearch = [];

        [ObservableProperty]
        private int loadingProgress = 0;

        [ObservableProperty]
        private int maxValues = 0;

        public ObservableCollection<bool> UsedValueKinds { get; set; }

        readonly List<RegistryValueKind> ValueKinds = Enum.GetValues(typeof(RegistryValueKind)).Cast<RegistryValueKind>().ToList();

        public ObservableCollection<bool> UsedRootKeys { get; set; }

        readonly List<RegistryKey> RootKeys = [
            Registry.ClassesRoot,
            Registry.CurrentUser,
            Registry.LocalMachine,
            Registry.Users,
            Registry.CurrentConfig
        ];

        public RelayCommand OpenAboutCommand => new(execute => OpenAbout());
        public RelayCommand ExportDataCommand => new(execute => ExportData());
        public RelayCommand LoadDataCommand => new(async (execute) => await FetchRegistryEntries(), canExecute => !SearchActive);
        public RelayCommand CancelSearchCommand => new(execute => CancelSearch(), canExecute => SearchActive);

        public MainViewModel()
        {
            Entries = [];
            UsedValueKinds = new(Enumerable.Repeat(true, ValueKinds.Count).ToList());
            UsedRootKeys = new(Enumerable.Repeat(false, RootKeys.Count).ToList());
        }

        private async Task FetchRegistryEntries()
        {
            tokenSource = new();
            SearchActive = true;

            Entries.Clear();
            LoadingProgress = 0;
            MaxValues = 0;
            kindsSearch = GetSelectedItems(ValueKinds, [.. UsedValueKinds]);
            List<RegistryKey> rootKeys = GetSelectedItems(RootKeys, [.. UsedRootKeys]);

            List<Task> tasks = [];
            foreach (RegistryKey rootKey in rootKeys)
            {
                Task task = Task.Run(() => RecursiveRegistryValueCollector(rootKey, "", tokenSource.Token));
                tasks.Add(task);
            }
            while (tasks.Count > 0)
            {
                Task done = await Task.WhenAny(tasks);
                tasks.Remove(done);
                LoadingProgress += 100 / rootKeys.Count;
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

            if (saveFileDialog.ShowDialog() == true)
            {
                ExportRegistryEntriesToCsv([.. Entries], saveFileDialog.FileName);
            }
        }

        private void OpenAbout()
        {
            new HelpWindow().ShowDialog();
        }

        private static List<T> GetSelectedItems<T>(List<T> Items, List<bool> Selected)
        {
            return Items
                .Zip(Selected, (obj, used) => new { obj, used })
                .Where(x => x.used)
                .Select(x => x.obj)
                .ToList();
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