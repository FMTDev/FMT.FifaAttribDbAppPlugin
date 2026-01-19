using FifaAttribDbAppPlugin;
using FMT.Hash;
using FMT.PluginInterfaces;
using FMT.PluginInterfaces.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMT.Core.Readers.FIFA
{
    public class FIFAAttribDbAssetEntry : IAssetEntry
    {
        public string Name { get; set; }

        public bool IsModified => throw new NotImplementedException();

        public bool IsIndirectlyModified => throw new NotImplementedException();

        public Sha1 Sha1 { get; set; }
        public IModifiedAssetEntry ModifiedEntry { get; set; }

        public bool HasModifiedData => throw new NotImplementedException();

        public long OriginalSize { get; set; }

        public List<int> Bundles => throw new NotImplementedException();

        public string ExtraInformation { get; set; }
        public string Type { get; set; }
        public bool IsLegacy { get; set; }
        public bool IsDirty { get; set; }
        public long Size { get; set; }
        public IAssetExtraData ExtraData { get; set; }
        public int SB_CAS_Offset_Position { get; set; }
        public int SB_CAS_Size_Position { get; set; }
        public string TOCFileLocation { get; set; }
        public AssetDataLocation Location { get; set; }
        public string SBFileLocation { get; set; }
        public bool IsAdded { get; set  ; }

        public FIFAAttribDbType AttribDbType { get; set; }

        public string GetDisplayName()
        {
            return AttribDbType.Name;
        }

        public string GetFilename()
        {
            return AttribDbType.Name;
        }

        public string GetPath()
        {
            return $"{AttribDbType.FolderName}/{AttribDbType.Name}";
        }

        public void LinkAsset(IAssetEntry entry)
        {
            throw new NotImplementedException();
        }
    }
}
