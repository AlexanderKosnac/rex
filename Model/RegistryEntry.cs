using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace rex.Model
{
    internal class RegistryEntry
    {
        public RegistryKey Key { get; set; }
        public string KeyPath { get; set; }
        public string ValueName { get; set; }
        public object Value { get; set; }
        public RegistryValueKind Kind { get; set; }

        public RegistryEntry(RegistryKey Key, string ValueName)
        {
            this.Key = Key;
            this.KeyPath = Key.ToString();
            this.ValueName = ValueName;
            this.Value = Key.GetValue(ValueName);
            this.Kind = Key.GetValueKind(ValueName);
        }
    }
}
