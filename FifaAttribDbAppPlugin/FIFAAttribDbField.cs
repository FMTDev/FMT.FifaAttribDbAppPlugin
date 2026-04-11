using FMT.PluginInterfaces.Assets;
using FMT.ServicesManagers;
using FMT.ServicesManagers.Interfaces;
using System.ComponentModel;

namespace FifaAttribDbAppPlugin
{
    public enum FifaAttribDbFieldType : ulong
    {
        Int32 = 0,
        FloatCurve = 1,
        FloatCurve2 = 2,
        Int64,
        Float = 4194304,
        Bool,
        String,
        RawBytes,
        Array = 131072
    }

    public class FIFAAttribDbField : INotifyPropertyChanged
    {
        public IAssetEntry ParentEntry { get; set; }
        public string Name { get; set; }

        public ulong Hash { get; set; }

        public object Value
        {
            get
            {
                // Return ModifiedValue if it exists, otherwise return OriginalValue
                if (ModifiedValue != null)
                    return ModifiedValue;

                return OriginalValue;
            }

            set
            {

                // Set OriginalValue if it's null, otherwise set ModifiedValue
                if (OriginalValue == null)
                {
                    OriginalValue = value;
                }
                else
                {
                    ModifiedValue = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));

                    if (SingletonService.Instantiated<IAssetManagementService>())
                    {
                        SingletonService.GetInstance<IAssetManagementService>().Logger?.Log($"Field Modified: {Name} | Original: {OriginalValue} | Modified: {ModifiedValue}");
                        SingletonService.GetInstance<IAssetManagementService>().ModifyEntry(ParentEntry, null);
                    }
                }
            }

        }

        public object OriginalValue { get; set; }

        public object ModifiedValue { get; set; }

        public FifaAttribDbFieldType FieldType { get; set; }

        public long? BinaryFileOffset { get; set; }

        public long VaultValueOffset { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public FIFAAttribDbField(string name, object value, ulong hash, long fieldType, long vaultValueOffset, long? binaryFileOffset = null)
        {
            Name = name;
            Hash = hash;
            FieldType = (FifaAttribDbFieldType)fieldType;
            VaultValueOffset = vaultValueOffset;
            BinaryFileOffset = binaryFileOffset;

            Value = value;

        }

        public override string ToString()
        {
            return $"{Name}:{FieldType}:{Hash}";
        }
    }

}
