using Microsoft.Win32;

namespace rex.Model
{
    internal class RegistryEntry(RegistryKey Key, string ValueName)
    {
        public RegistryKey Key { get; set; } = Key;
        public string KeyPath { get; set; } = Key.ToString();
        public string ValueName { get; set; } = ValueName == "" ? "(Default)" : ValueName;
        public object Value { get; set; } = RegistryEntry.getValue(Key, ValueName);
        public RegistryValueKind Kind { get; set; } = Key.GetValueKind(ValueName);

        public string ToCSV()
        {
            return string.Join(",", [KeyPath, ValueName, Value, Kind.ToString()]);
        }

        static object getValue(RegistryKey Key, string ValueName)
        {
            object value = Key.GetValue(ValueName) ?? "";
            return Key.GetValueKind(ValueName) switch
            {
                RegistryValueKind.Binary => System.Text.Encoding.UTF8.GetString((byte[])value),
                RegistryValueKind.MultiString => string.Join("", (string[])value),
                _ => value,
            };
        }
    }
}
