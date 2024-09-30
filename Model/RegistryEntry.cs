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
            switch (Key.GetValueKind(ValueName))
            {
                case RegistryValueKind.Binary:
                    return System.Text.Encoding.UTF8.GetString((byte[]) value);
                case RegistryValueKind.MultiString:
                    return string.Join("", (string[]) value);
                case RegistryValueKind.None:
                case RegistryValueKind.Unknown:
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                case RegistryValueKind.DWord:
                case RegistryValueKind.QWord:
                default:
                    return value;
            }
        }
    }
}
