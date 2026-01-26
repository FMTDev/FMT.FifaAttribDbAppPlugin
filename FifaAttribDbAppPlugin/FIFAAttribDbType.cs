using System.Collections.ObjectModel;

namespace FifaAttribDbAppPlugin
{
    public struct FIFAAttribDbType
    {
        public string Name { get; set; }

        public ulong HashLong { get; set; }

        public string FolderName { get; set; }

        public ulong FolderHash { get; set; }

        public List<FIFAAttribDbField> Fields { get; set; } = new List<FIFAAttribDbField>();

        public FIFAAttribDbType(string name, ulong hash, string folderName, ulong folderHash, List<FIFAAttribDbField> fields)
        {
            Name = name;
            HashLong = hash;
            FolderName = folderName;
            FolderHash = folderHash;
            Fields = fields;
        }

        public override string ToString()
        {
            return $"{FolderName}/{Name}:{HashLong}";
        }
    }

    public class EditableTypeViewModel
    {
        public string Name { get; }
        public string FolderName { get; }
        public ulong HashLong { get; }
        public ulong FolderHash { get; }

        public ObservableCollection<EditableFieldViewModel> Fields { get; }

        public EditableTypeViewModel(FIFAAttribDbType type)
        {
            Name = type.Name;
            FolderName = type.FolderName;
            HashLong = type.HashLong;
            FolderHash = type.FolderHash;

            Fields = new ObservableCollection<EditableFieldViewModel>(
                type.Fields.Select(f => new EditableFieldViewModel(f))
            );
        }

        public FIFAAttribDbType ToModel()
        {
            return new FIFAAttribDbType(
                Name,
                HashLong,
                FolderName,
                FolderHash,
                Fields.Select(f => new FIFAAttribDbField(f.Name, f.ToBytes(), f.Hash, (long)f.FieldType)).ToList()
            );
        }
    }

}
