using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FifaAttribDbAppPlugin
{
    public struct FIFAAttribDbType
    {
        public string Name { get; set; }

        public ulong HashLong { get; set; }

        public string FolderName { get; set; }

        public ulong FolderHash { get; set; }

        public long DataOffsetInVault { get; set; }

        public byte[] DataInVault { get; set; }


        public List<FIFAAttribDbField> Fields { get; set; } = new List<FIFAAttribDbField>();

        public List<ulong> Hashes { get; set; } = new List<ulong>();

        public FIFAAttribDbType(string name, ulong hash, string folderName, ulong folderHash, long dataOffsetInVault, byte[] dataInVault, List<FIFAAttribDbField> fields)
        {
            Name = name;
            HashLong = hash;
            FolderName = folderName;
            FolderHash = folderHash;
            DataOffsetInVault = dataOffsetInVault;
            DataInVault = dataInVault;
            Fields = fields;
        }

        public override string ToString()
        {
            return $"{FolderName}/{Name}:{HashLong}";
        }
    }

    public class EditableTypeViewModel : INotifyPropertyChanged
    {
        public string Name { get; }
        public string FolderName { get; }
        public ulong HashLong { get; }
        public ulong FolderHash { get; }

        public long DataOffsetInVault { get; set; }
        public long DataSizeInVault { get; set; }

        public ObservableCollection<FIFAAttribDbField> Fields { get; }

        public EditableTypeViewModel(FIFAAttribDbType type)
        {
            Name = type.Name;
            FolderName = type.FolderName;
            HashLong = type.HashLong;
            FolderHash = type.FolderHash;

            Fields = new ObservableCollection<FIFAAttribDbField>(type.Fields.OrderBy(x => x.Name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

}
