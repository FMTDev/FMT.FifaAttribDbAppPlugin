using System.ComponentModel;

namespace FifaAttribDbAppPlugin
{
    public class EditableFloatItem : INotifyPropertyChanged
    {
        private readonly EditableFloatArrayViewModel _parent;
        private float _value;
        private bool _isUpdating;

        public float Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                if (!_isUpdating)
                    _parent.OnItemChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public EditableFloatItem(float value, EditableFloatArrayViewModel parent)
        {
            _value = value;
            _parent = parent;
        }

        internal void SetValue(float value)
        {
            if (_value == value) return;
            _isUpdating = true;
            _value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            _isUpdating = false;
        }
    }
}
