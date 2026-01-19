using FMT.Hash;
using System;
using System.Collections.Generic;
using System.Text;

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
}
