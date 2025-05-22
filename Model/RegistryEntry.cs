using Microsoft.Win32;

namespace rex.Model
{
    internal class RegistryEntry(RegistryKey key, string valueName)
    {
        public RegistryKey Key { get; } = key;
        public string KeyPath { get; } = $"{key}";
        public string ValueName { get; } = valueName == "" ? "(Default)" : valueName;
        public string Value { get; } = GetValue(key, valueName);
        public RegistryValueKind Kind { get; } = key.GetValueKind(valueName);

        public string ToCsv() => string.Join(",", [KeyPath, ValueName, Value, Kind.ToString()]);

        static string GetValue(RegistryKey key, string valueName)
        {
            object? value = key.GetValue(valueName);
            if (value is null)
                return "null";
            return key.GetValueKind(valueName) switch
            {
                RegistryValueKind.Binary => (value is byte[] bytes) ? System.Text.Encoding.UTF8.GetString(bytes) : "(Failed to extract Binary data)",
                RegistryValueKind.MultiString => (value is string[] text) ? string.Join("", text) : "(Failed to extract MultiString data)",
                _ => value.ToString() ?? "(Failed to extract)",
            };
        }
    }
}
