using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
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
        public object? Value { get; set; } = Key.GetValue(ValueName);
        public RegistryValueKind Kind { get; set; } = Key.GetValueKind(ValueName);
    }
}
