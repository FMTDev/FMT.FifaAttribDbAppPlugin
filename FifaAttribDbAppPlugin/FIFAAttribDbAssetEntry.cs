using FMT.FileTools;
using FMT.Hash;
using FMT.Models.Assets.AssetEntry.Entries;
using FMT.PluginInterfaces;
using FMT.PluginInterfaces.Assets;

namespace FifaAttribDbAppPlugin
{
    public class FIFAAttribDbAssetEntry : IAssetEntry
    {
        public string Name { get; set; }

        public bool IsModified
        {
            get
            {
                return HasModifiedData;
            }
        }

        public bool IsIndirectlyModified { get; set; }

        public Sha1 Sha1 { get; set; }


        public long OriginalSize { get; set; }

        public List<int> Bundles { get; set; }
        public List<ulong> Bundles64 { get; set; }

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
        public bool IsAdded { get; set; }

        public FIFAAttribDbType AttribDbType { get; set; }

        public bool HasModifiedData
        {
            get
            {
                return AttribDbType.Fields.Any(x => x.ModifiedValue != null);
            }
        }

        public IModifiedAssetEntry ModifiedEntry
        {
            get
            {
                return HasModifiedData ? new ModifiedAssetEntry() : null;
            }
            set => throw new NotImplementedException();
        }


        public FIFAAttribDbAssetEntry(FIFAAttribDbType attribDbType)
        {
            AttribDbType = attribDbType;
            Name = attribDbType.Name;
            Type = "FIFAAttribDbType";
            foreach (var f in attribDbType.Fields)
            {
                f.ParentEntry = this;
            }
        }

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
            return $"{AttribDbType.FolderName}";
        }

        public void LinkAsset(IAssetEntry entry)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return AttribDbType.ToString();
        }

        public byte[] GetData()
        {
            var vanillaData = AttribDbType.DataInVault;

            byte[] vanillaDataCloned;
            using (var nw = new NativeWriter(new MemoryStream(vanillaData)))
            {
                nw.Write(vanillaData);
                vanillaDataCloned = ((MemoryStream)nw.BaseStream).ToArray();
            }

            using (var nw = new NativeWriter(new MemoryStream(vanillaDataCloned)))
            {
                foreach (var f in AttribDbType.Fields)
                {
#if DEBUG
                    if (f.Name == "ATTR_DribbleJogSpeed")
                    {

                    }
#endif
                    switch (f.FieldType)
                    {
                        case FifaAttribDbFieldType.Bool:
                            break;
                        case FifaAttribDbFieldType.Float:
                            if (float.TryParse(f.Value.ToString(), out var fl))
                            {
                                nw.Position = f.VaultValueOffset - AttribDbType.DataOffsetInVault;
                                nw.Write(fl);
                            }
                            break;
                    }
                }
                return ((MemoryStream)nw.BaseStream).ToArray();
            }

        }
    }
}
