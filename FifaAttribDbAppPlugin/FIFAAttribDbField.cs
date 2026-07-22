using FMT.PluginInterfaces.Assets;
using FMT.ServicesManagers;
using FMT.ServicesManagers.Interfaces;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Windows.Input;

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
                if (ModifiedValue != null)
                {
                    if (string.IsNullOrEmpty(ModifiedValue.ToString()))
                    {
                        ModifiedValue = null;
                        return OriginalValue;
                    }

                    if (ModifiedValue.ToString() == "System.Object")
                    {
                        ModifiedValue = null;
                        return OriginalValue;
                    }

                    return ModifiedValue;
                }

                switch (FieldType)
                {
                    case FifaAttribDbFieldType.Float:
                        if (OriginalValue is float fv)
                        {
                            return fv;
                        }
                        if (OriginalValue is string fs && float.TryParse(fs, out fv))
                        {
                            return fv;
                        }
                        break;
                }

                return OriginalValue;
            }

            set
            {
                if (FieldType == FifaAttribDbFieldType.Float && value is string strVal)
                {
                    if (float.TryParse(strVal, out float parsed))
                        value = parsed;
                }

                if (OriginalValue == null)
                {
                    OriginalValue = value;
                }
                else
                {
                    ModifiedValue = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueString)));

                    if (ParentEntry as INotifyPropertyChanged != null)
                        PropertyChanged?.Invoke((ParentEntry as INotifyPropertyChanged), new PropertyChangedEventArgs("IsModified"));

                    if (SingletonService.Instantiated<IAssetManagementService>())
                    {
                        if (OriginalValue.GetType().IsArray)
                        {
                            SingletonService.GetInstance<IAssetManagementService>().Logger?.Log($"Field Modified: {Name} | Original: {JsonConvert.SerializeObject(OriginalValue)} | Modified: {JsonConvert.SerializeObject(ModifiedValue)}");
                        }
                        else
                        {
                            SingletonService.GetInstance<IAssetManagementService>().Logger?.Log($"Field Modified: {Name} | Original: {OriginalValue} | Modified: {ModifiedValue}");
                        }
                        SingletonService.GetInstance<IAssetManagementService>().ModifyEntry(ParentEntry, null);
                    }
                }
            }

        }

        public object OriginalValue { get; set; }

        public object ModifiedValue { get; set; }

        public FifaAttribDbFieldType FieldType { get; set; }

        private EditableFloatArrayViewModel _arrayViewModel;
        public EditableFloatArrayViewModel ArrayViewModel
        {
            get
            {
                if (_arrayViewModel != null) return _arrayViewModel;
                if (Value is float[] arr)
                {
                    _arrayViewModel = new EditableFloatArrayViewModel(arr, this);
                    return _arrayViewModel;
                }
                return null;
            }
        }

        public long? BinaryFileOffset { get; set; }

        public int? BinaryFileSize { get; set; }

        public long VaultValueOffset { get; set; }

        public string ValueString
        {
            get
            {
                var val = Value;
                var result = val?.ToString() ?? string.Empty;
                System.Diagnostics.Debug.WriteLine($"[ValueString.get] Name={Name} Value={val} ({val?.GetType()}) Result=\"{result}\"");
                return result;
            }
            set
            {
                if (value == Value?.ToString()) return;
                if (FieldType == FifaAttribDbFieldType.Float && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f))
                    Value = f;
                else
                    Value = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand RevertValueCommand { get; set; } = new RelayCommand((x) =>
        {
        });

        public FIFAAttribDbField()
        {
            RevertValueCommand = new RelayCommand((x) =>
            {
                Value = null;
            });
        }

        public FIFAAttribDbField(string name, object value, ulong hash, long fieldType, long vaultValueOffset, long? binaryFileOffset = null) : this()
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
