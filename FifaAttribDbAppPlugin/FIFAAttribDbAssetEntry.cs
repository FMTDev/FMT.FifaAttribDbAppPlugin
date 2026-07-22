using FMT.FileTools;
using FMT.Hash;
using FMT.Models.Assets.AssetEntry.Entries;
using FMT.PluginInterfaces;
using FMT.PluginInterfaces.Assets;
using System.ComponentModel;

namespace FifaAttribDbAppPlugin
{
    public class FIFAAttribDbAssetEntry : IAssetEntry, INotifyPropertyChanged
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
            set
            {

            }
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

        public event PropertyChangedEventHandler PropertyChanged;

        public string GetDisplayName()
        {
            return AttribDbType.Name;
        }

        public string GetFilename()
        {
            return AttribDbType.Name;
        }

        public string VltPath { get; set; }
        public string BinPath { get; set; }

        public string GetPath()
        {
            if (!string.IsNullOrEmpty(VltPath))
            {
                var parts = VltPath.Split('/');
                if (parts.Length > 2)
                {
                    return $"{parts[parts.Length - 2]}/{AttribDbType.FolderName}";
                }
            }
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
                    if (f.ModifiedValue == null) continue;

                    var vltOffset = f.VaultValueOffset - AttribDbType.DataOffsetInVault;
                    //if (vltOffset < 0 || vltOffset + 8 > vanillaDataCloned.Length) continue;

#if DEBUG
                    var vltOffsetInFile = f.VaultValueOffset;
                    if (vltOffsetInFile == 11104)
                    {

                    }
#endif

                    switch (f.FieldType)
                    {
                        case FifaAttribDbFieldType.Float:
                            if (f.Value is float fv || (f.Value is string fs && float.TryParse(fs, out fv)))
                            {
                                nw.Position = vltOffset;
                                nw.Write(fv);
                            }
                            break;
                        case FifaAttribDbFieldType.Int32:
                            if (f.Value is int iv)
                            {
                                nw.Position = vltOffset;
                                nw.Write(iv);
                            }
                            break;
                        case FifaAttribDbFieldType.Int64:
                            if (f.Value is long lv)
                            {
                                nw.Position = vltOffset;
                                nw.Write(lv);
                            }
                            break;
                        case FifaAttribDbFieldType.Bool:
                            if (f.Value is bool bv)
                            {
                                nw.Position = vltOffset;
                                nw.Write(bv ? (byte)1 : (byte)0);
                            }
                            break;
                        case FifaAttribDbFieldType.String:
                        case FifaAttribDbFieldType.RawBytes:
                            if (f.Value is int sv)
                            {
                                nw.Position = vltOffset;
                                nw.Write(sv);
                            }
                            break;
                    }
                }
                return ((MemoryStream)nw.BaseStream).ToArray();
            }
        }
    }
}
