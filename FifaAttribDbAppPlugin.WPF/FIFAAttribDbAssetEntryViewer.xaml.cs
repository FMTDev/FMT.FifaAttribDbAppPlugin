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

        private void btnRevert_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button.Command != null && button.Command is RelayCommand relayCommand)
                relayCommand.RaiseCanExecuteChanged();
        }
    }
}
