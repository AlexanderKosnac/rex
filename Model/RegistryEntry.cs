using Microsoft.Win32;

namespace rex.Model
{
    internal class RegistryEntry(RegistryKey Key, string ValueName)
    {
        public RegistryKey Key { get; set; } = Key;
        public string KeyPath { get; set; } = Key.ToString();
        public string ValueName { get; set; } = ValueName == "" ? "(Default)" : ValueName;
        public string Value { get; set; } = RegistryEntry.GetValue(Key, ValueName);
        public RegistryValueKind Kind { get; set; } = Key.GetValueKind(ValueName);

        public string ToCSV()
        {
            return string.Join(",", [KeyPath, ValueName, Value, Kind.ToString()]);
        }

        static string GetValue(RegistryKey Key, string ValueName)
        {
            object? value = Key.GetValue(ValueName);
            if (value is null)
                return "null";
            return Key.GetValueKind(ValueName) switch
            {
                RegistryValueKind.Binary => (value is byte[] bytes) ? System.Text.Encoding.UTF8.GetString(bytes) : "(Failed to extract Binary data)",
                RegistryValueKind.MultiString => (value is string[] text) ? string.Join("", text) : "(Failed to extract MultiString data)",
                _ => value.ToString() ?? "(Failed to extract)",
            };
        }
    }
}
