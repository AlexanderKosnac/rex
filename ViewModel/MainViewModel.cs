using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using rex.Core.DataStructure;
using rex.Model;
using rex.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Security;
using System.Text;
using System.Threading.Tasks.Dataflow;
using System.Windows;

namespace rex.ViewModel
{
    internal partial class MainViewModel : ObservableObject
    {
        public ObservableCollection<RegistryEntry> Entries { get; } = [];

        [ObservableProperty]
        private string _pathSearch = "";

        [ObservableProperty]
        private string _nameSearch = "";

        [ObservableProperty]
        private string _valueSearch = "";

        public List<RegistryValueKind> kindsSearch = [];

        [ObservableProperty]
        private bool _searchActive = false;

        [ObservableProperty]
        private int _maxValues = 0;

        public ObservableCollection<RegistryValueKindItem> ValueKinds { get; } = [
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

        [RelayCommand(AllowConcurrentExecutions = false, FlowExceptionsToTaskScheduler = true, IncludeCancelCommand = true)]
        public async Task SearchData(CancellationToken token)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Entries.Clear();
                MaxValues = 0;
                kindsSearch = [.. ValueKinds.Where(k => k.IsSelected).Select(k => k.Object)];
            });

            const int bufferThreshold = 1000;
            List<RegistryEntry> buffer = [];
            object bufferLock = new();

            async Task FlushBufferAsync()
            {
                List<RegistryEntry> toAdd;

                lock (bufferLock)
                {
                    toAdd = [.. buffer];
                    buffer.Clear();
                }

                if (toAdd.Count > 0)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var item in toAdd)
                        {
                            Entries.Add(item);
                        }
                    });
                }
            }

            void AddToEntries(RegistryEntry entry)
            {
                bool doFlush;

                lock (bufferLock)
                {
                    buffer.Add(entry);
                    doFlush = buffer.Count >= bufferThreshold;
                }

                if (doFlush)
                    _ = FlushBufferAsync();
            }

            await Task.Run(() =>
            {
                foreach (var key in RootKeys.Where(k => k.IsSelected))
                {
                    RecursiveRegistryValueVisitor(key.Object, "", AddToEntries, token);
                }
            }, token);

            await FlushBufferAsync();
        }

        private void RecursiveRegistryValueVisitor(RegistryKey baseKey, string subKey, Action<RegistryEntry> onEntryFound, CancellationToken token)
        {
            if (OpenSubKeyOrNull(baseKey, subKey) is not RegistryKey key)
                return;

            foreach (string valueName in key.GetValueNames())
            {
                token.ThrowIfCancellationRequested();

                RegistryEntry re = new(key, valueName);
                MaxValues++;

                bool matchesByPath = string.IsNullOrEmpty(PathSearch) || re.KeyPath.Contains(PathSearch);
                bool matchesByName = string.IsNullOrEmpty(NameSearch) || re.ValueName.Contains(NameSearch);
                bool matchesByValue = string.IsNullOrEmpty(ValueSearch) || re.Value.Contains(ValueSearch);
                bool matchesByKind = kindsSearch.Contains(re.Kind);

                if (matchesByPath && matchesByName && matchesByValue && matchesByKind)
                {
                    onEntryFound(re);
                }
            }

            foreach (string subKeyName in key.GetSubKeyNames())
            {
                RecursiveRegistryValueVisitor(key, subKeyName, onEntryFound, token);
            }
        }

        private static RegistryKey? OpenSubKeyOrNull(RegistryKey baseKey, string subKey)
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

        [RelayCommand]
        public void ExportData()
        {
            SaveFileDialog dialog = new()
            {
                Filter = "CSV file (*.csv)|*.csv",
                FileName = "export.csv"
            };

            if (dialog.ShowDialog() is bool r && r)
                ExportRegistryEntriesToCsv([.. Entries], dialog.FileName);
        }

        [RelayCommand]
        public void OpenAbout()
        {
            new HelpWindow().ShowDialog();
        }

        private static void ExportRegistryEntriesToCsv(List<RegistryEntry> Entries, string filePath)
        {
            StringBuilder sb = new();
            sb.AppendLine("Path,Name,Value,Kind");
            foreach (RegistryEntry re in Entries)
                sb.AppendLine(re.ToCsv());
            File.WriteAllText(filePath, sb.ToString());
        }
    }
}