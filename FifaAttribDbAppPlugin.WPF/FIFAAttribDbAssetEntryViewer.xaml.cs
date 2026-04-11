using System.Windows.Controls;

namespace FifaAttribDbAppPlugin.WPF
{
    /// <summary>
    /// Interaction logic for FIFAAttribDbAssetEntryViewer.xaml
    /// </summary>
    public partial class FIFAAttribDbAssetEntryViewer : UserControl
    {
        public FIFAAttribDbAssetEntryViewer()
        {
            InitializeComponent();
        }

        public FIFAAttribDbAssetEntryViewer(FIFAAttribDbAssetEntry entry)
        {
            InitializeComponent();
            DataContext = new EditableTypeViewModel(entry.AttribDbType);
        }
    }
}
