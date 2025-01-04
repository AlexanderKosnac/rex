using Microsoft.Win32;

namespace rex.Core.DataStructure
{
    internal class RegistryKeyItem(string displayString, RegistryKey obj, bool isSelected)
    {
        public string DisplayString { get; } = displayString;
        public RegistryKey Object { get; } = obj;
        public bool IsSelected { get; set; } = isSelected;

        public override string ToString()
        {
            return (DisplayString, Object, IsSelected).ToString();
        }
    }
}
