using Microsoft.Win32;

namespace rex.Core.DataStructure
{
    internal class RegistryValueKindItem(RegistryValueKind obj, bool isSelected)
    {
        public RegistryValueKind Object { get; } = obj;
        public bool IsSelected { get; set; } = isSelected;

        public override string ToString()
        {
            return (Object, IsSelected).ToString();
        }
    }
}
