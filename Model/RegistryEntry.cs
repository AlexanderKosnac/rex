using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing.IndexedProperties;
using System.Security.AccessControl;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace rex.Model
{
    internal class RegistryEntry(RegistryKey Key, string ValueName)
    {
        public RegistryKey Key { get; set; } = Key;
        public string KeyPath { get; set; } = Key.ToString();
        public string ValueName { get; set; } = ValueName == "" ? "(Default)" : ValueName;
        public object? Value { get; set; } = RegistryEntry.getValue(Key, ValueName);
        public RegistryValueKind Kind { get; set; } = Key.GetValueKind(ValueName);

        static object? getValue(RegistryKey Key, string ValueName)
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
