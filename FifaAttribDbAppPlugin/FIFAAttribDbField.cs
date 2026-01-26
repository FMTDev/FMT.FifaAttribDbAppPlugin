using System.ComponentModel;

namespace FifaAttribDbAppPlugin
{
    public enum FifaAttribDbFieldType : ulong
    {
        Int32,
        FloatCurve = 1,
        Int64,
        Float = 4194304,
        Bool,
        String,
        RawBytes,
        Array = 131072
    }

    public struct FIFAAttribDbField
    {
        public string Name { get; set; }

        public ulong Hash { get; set; }

        public byte[] Value { get; set; }

        public FifaAttribDbFieldType FieldType { get; set; }

        public FIFAAttribDbField(string name, byte[] value, ulong hash, long fieldType)
        {
            Name = name;
            Value = value;
            Hash = hash;
            FieldType = (FifaAttribDbFieldType)fieldType;
        }

        public override string ToString()
        {
            return $"{Name}:{FieldType}:{Hash}";
        }
    }

    public class EditableFieldViewModel : INotifyPropertyChanged
    {
        public string Name { get; }
        public ulong Hash { get; }

        private object _value;

        public event PropertyChangedEventHandler? PropertyChanged;

        public object Value
        {
            get => _value;
            set
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
            }
        }

        public FifaAttribDbFieldType FieldType { get; }


        public EditableFieldViewModel(FIFAAttribDbField field)
        {
            Name = field.Name;
            Hash = field.Hash;
            //Value = ConvertFromBytes(field.Value);
            FieldType = field.FieldType;
            switch (FieldType)
            {
                case FifaAttribDbFieldType.Float:
                    Value = BitConverter.ToSingle(new ReadOnlySpan<byte>(field.Value));
                    break;
                case FifaAttribDbFieldType.Array:
                    break;
            }
        }

        public byte[] ToBytes()
        {
            //return ConvertToBytes(Value);
            return null;
        }

        // Your conversion logic here...


    }

}
