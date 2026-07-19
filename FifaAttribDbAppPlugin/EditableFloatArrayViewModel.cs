using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace FifaAttribDbAppPlugin
{
    public class EditableFloatArrayViewModel : INotifyPropertyChanged
    {
        private readonly FIFAAttribDbField _field;

        public float[] FloatArray { get; }

        public ObservableCollection<EditableFloatItem> Items { get; }

        public int Count => Items.Count;

        public EditableFloatArrayViewModel(float[] values, FIFAAttribDbField field)
        {
            FloatArray = values;
            _field = field;
            Items = new ObservableCollection<EditableFloatItem>(
                values.Select(v => new EditableFloatItem(v, this)));
        }

        public void OnItemChanged()
        {
            _field.Value = Items.Select(i => i.Value).ToArray();
        }

        public void SyncFrom(float[] values)
        {
            if (values == null) return;
            for (int i = 0; i < Math.Min(values.Length, Items.Count); i++)
                Items[i].SetValue(values[i]);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
