using rex.Model;
using rex.MVVM;
using rex.Views;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.Win32;
using System.Windows;
using System.Diagnostics;
using System.IO;

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

        public bool searchActive = false;
        public bool SearchActive {
            get { return searchActive; }
            set {
                searchActive = value;
                OnPropertyChanged();
            }
        }

        List<RegistryValueKind> kindsSearch = [];

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
        public RelayCommand ExportDataCommand => new(execute => ExportData());
        public RelayCommand LoadDataCommand => new(async (execute) => await FetchRegistryEntries());
        public RelayCommand CancelSearchCommand => new(execute => CancelSearch());

        CancellationTokenSource tokenSource = new();

        public MainViewModel()
        {
            Entries = [];
            UsedValueKinds = new(Enumerable.Repeat(true, ValueKinds.Count).ToList());
            UsedRootKeys = new(Enumerable.Repeat(false, RootKeys.Count).ToList());
        }

        private async Task FetchRegistryEntries()
        {
            tokenSource.Dispose();
            tokenSource = new();

            SearchActive = true;
            Entries.Clear();
            LoadingProgress = 0;
            MaxValues = 0;
            kindsSearch = MainViewModel.GetSelectedItems(ValueKinds, UsedValueKinds.ToList());
            List<RegistryKey> rootKeys = MainViewModel.GetSelectedItems(RootKeys, UsedRootKeys.ToList());

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

            SearchActive = false;
        }

        private void CancelSearch()
        {
            tokenSource.Cancel();
        }

        private void RecursiveRegistryValueCollector(RegistryKey baseKey, string subKey, CancellationToken token)
        {
            try
            {
                using RegistryKey? key = baseKey.OpenSubKey(subKey);
                if (key != null)
                {
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
                            Application.Current.Dispatcher.Invoke(() => Entries.Add(re));
                        }
                    }

                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        RecursiveRegistryValueCollector(key, subKeyName, token);
                    }
                }
            }
            catch (System.Security.SecurityException)
            {
                Debug.WriteLine($"Not allowed to open key {subKey}");
            }
        }

        private void ExportData()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
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

        private static void ExportRegistryEntriesToCsv(List<RegistryEntry> Entries, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Path,Name,Value,Kind");
            foreach (RegistryEntry re in Entries)
            {
                sb.AppendLine(re.ToCSV());
            }
            File.WriteAllText(filePath, sb.ToString());
        }
    }
}