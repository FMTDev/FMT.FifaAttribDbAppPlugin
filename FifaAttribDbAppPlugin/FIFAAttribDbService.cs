using FifaAttribDbAppPlugin.AttribDb;
using FifaAttribDbAppPlugin.AttribDbGameplay;
using FMT.FileTools;
using FMT.Hash;
using FMT.PluginInterfaces.Assets;
using FMT.ServicesManagers;
using FMT.ServicesManagers.AssetEntryServicing;
using FMT.ServicesManagers.Interfaces;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaAttribDbAppPlugin
{
    public class FIFAAttribDbService : IFIFAAttribDbService, IAssetEntryService
    {
        public FIFAAttribDbService()
        {
        }

        public List<FIFAAttribDbAssetEntry> Assets { get; } = new List<FIFAAttribDbAssetEntry>();

        public Type AssetEntryType { get; } = typeof(FIFAAttribDbAssetEntry);

        public FIFAAttribDbAssetEntry GetAssetEntry(string key)
        {
            if (Assets.Count == 0)
                return null;

            return Assets.First(x => $"{x.AttribDbType.FolderName}/{x.AttribDbType.Name}".Equals(key, StringComparison.OrdinalIgnoreCase));
        }

        T IAssetEntryService.GetAssetEntry<T>(string key)
        {
            return GetAssetEntry(key) as T;
        }

        public void ReadAttribDbBinary(byte[] data)
        {
            var reader = new FIFAAttribDbBinaryReader(data);
        }

        public void ReadAttribDbVlt(byte[] data)
        {

        }

        public List<FIFAAttribDbType> ReadAttribDbGameplayBinary(byte[] data, List<FIFAAttribDbType> types)
        {
            using var r = new FIFAAttribDbGameplayBinaryReader(data);

            return types;
        }

        public List<FIFAAttribDbType> ReadAttribDbGameplayVlt(byte[] data)
        {
            using var r = new FIFAAttribDbGameplayVLTReader(data);
            return r.ListOfDbTypes;
        }

        public void Load(byte[] vaultData, byte[] binaryData)
        {
            var types = ReadAttribDbGameplayVlt(vaultData);

            foreach (var type in types)
            {
                var entry = new FIFAAttribDbAssetEntry(type)
                {
                    Name = type.Name,
                    Type = "FIFAAttribDbAssetEntry",
                    Size = 0,
                    OriginalSize = 0,
                    Sha1 = Sha1.Create(Encoding.UTF8.GetBytes(type.Name)),
                };
                Assets.Add(entry);
            }

            types = ReadAttribDbGameplayBinary(binaryData, types);

            _ = types;
        }

        public async void Save()
        {
            var vltEntry = SingletonService.GetInstance<IAssetManagementService>().CustomAssetManagers["legacy"].GetAssetEntry("data/attribdbgameplay/attribdb.vlt");
            var vltVanillaData = SingletonService.GetInstance<IAssetManagementService>().GetAssetData(vltEntry, false);
            var writtenData = await new FIFAAttribDbVLTWriter().WriteToBytes(Assets, vltVanillaData);

            // Write the modified loose VLT 
            SingletonService.GetInstance<IAssetManagementService>().ModifyCustomAsset("legacy", "data/attribdbgameplay/attribdb.vlt", writtenData);

            // Write the modified VLT into the BIG
            //var bigEntry = SingletonService.GetInstance<IAssetManagementService>().CustomAssetManagers["legacy"].GetAssetEntry("data/attribdbgameplay/gameplayattribdb.big");
            //SingletonService.GetInstance<IAssetManagementService>().ModifyCustomAsset("legacy", "data/attribdbgameplay/attribdb.vlt", writtenData);


        }

        public IEnumerable<IAssetEntry> EnumerateAssets(bool modifiedOnly = false)
        {
            if (modifiedOnly)
                return Assets.Where(x => x.HasModifiedData).OrderBy(x => x.Name);
            else
                return Assets.OrderBy(x => x.Name);
        }

        public byte[] GetAssetEntryData(IAssetEntry entry)
        {
           return entry is FIFAAttribDbAssetEntry dbEntry ? dbEntry.GetData() : null;
        }

        public IAssetEntry GenerateAssetEntry(string name, string type, Sha1 sha)
        {
            return new FIFAAttribDbAssetEntry(new FIFAAttribDbType() { })
            {
                Name = name,
                Type = type,
                Sha1 = sha
            };
        }

        public void RevertAsset(IAssetEntry entry)
        {
            if (entry is FIFAAttribDbAssetEntry dbEntry)
            {
                foreach(var field in dbEntry.AttribDbType.Fields)
                {
                    field.ModifiedValue = null;
                }
            }
        }

        public bool ModifyAssetEntry(IAssetEntry entry, byte[] data, bool isDataCompressed, bool raiseNotification = true)
        {
            if (entry is FIFAAttribDbAssetEntry dbEntry)
            {
                Save();
                // Modification has been done via AttribDbType.
                return true;
            }
            return false;
        }

        public byte[] WriteAssetEntryInfo(IAssetEntry entry)
        {
            // If we are writing the asset entries we are liking saving the project or loading from cache
            // Revert the VLT and BIN of gameplay, we want this system to modify it back when loading
            //var vlt = SingletonService.GetInstance<IAssetManagementService>().get("legacy", "data/attribdbgameplay/attribdb.vlt");
            //SingletonService.GetInstance<IAssetManagementService>().RevertAsset("legacy", "data/attribdbgameplay/attribdb.vlt", await new FIFAAttribDbVLTWriter().WriteToBytes(Assets));



            if (entry is FIFAAttribDbAssetEntry attribDbEntry)
            {
                NativeWriter nativeWriter = new NativeWriter(new MemoryStream());
                nativeWriter.WriteLengthPrefixedString($"{attribDbEntry.AttribDbType.FolderName}/{attribDbEntry.AttribDbType.Name}");

                bool modified = attribDbEntry.IsModified;
                nativeWriter.Write(modified);
                if (modified)
                {
                    var countOfFields = attribDbEntry.AttribDbType.Fields.Count;
                    nativeWriter.Write(countOfFields);
                    for (var i = 0; i < countOfFields; i++) 
                    {
                        FIFAAttribDbField field = attribDbEntry.AttribDbType.Fields[i];

                        nativeWriter.Write(field.Name);
                        bool fieldIsModified = field.Name != "unknown" && field.ModifiedValue != null;
                        nativeWriter.Write(fieldIsModified);
                        if (fieldIsModified)
                        {
                            nativeWriter.Write((ulong)field.FieldType);
                            switch (field.FieldType)
                            {
                                case FifaAttribDbFieldType.Int32:
                                    nativeWriter.Write((int)field.ModifiedValue);
                                    break;
                                case FifaAttribDbFieldType.Float:
                                    if (field.ModifiedValue is System.String)
                                        nativeWriter.Write(float.Parse((string)field.ModifiedValue));
                                    else if (field.ModifiedValue is float)
                                        nativeWriter.Write((float)field.ModifiedValue);
                                    break;
                            }
                        }
                    }
                }


                return ((MemoryStream)nativeWriter.BaseStream).ToArray();
            }

            return null;
        }

        public IAssetEntry ReadAssetEntryInfo(byte[] data)
        {
            using NativeReader reader = new NativeReader(new MemoryStream(data));
            var name = reader.ReadLengthPrefixedString();

            var existingAsset = GetAssetEntry(name);
            if (existingAsset == null)
                return null;

            // Revert the existing asset entry
            SingletonService.GetInstance<IAssetManagementService>().RevertAsset(existingAsset);

            // Overwrite the existing asset entry
            var isModified = reader.ReadBoolean();
            if (isModified)
            {
                var countOfFields = reader.ReadInt();
                for (var i = 0; i < countOfFields; i++)
                {
                    var fieldName = reader.ReadLengthPrefixedString();
                    var fieldIsModified = reader.ReadBoolean();

                    if (fieldIsModified)
                    {
                        var fieldType = (FifaAttribDbFieldType)reader.ReadULong();
                        switch (fieldType)
                        {
                            case FifaAttribDbFieldType.Int32:
                                reader.ReadInt();
                                break;
                            case FifaAttribDbFieldType.Float:
                                reader.ReadSingle();
                                break;
                        }
                    }
                }
            }

            return existingAsset;
        }
    }

    public static class FieldNameHashLoader
    {
        private static Dictionary<ulong, string>? _fieldNameHashes;

        public static Dictionary<ulong, string> Load()
        {
            if (_fieldNameHashes != null) return _fieldNameHashes;

            var ms = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("FifaAttribDbAppPlugin.FieldNameHashes.csv").CopyTo(ms);
            ms.Position = 0;
            return Load(ms);
        }

        public static Dictionary<ulong, string> Load(MemoryStream ms)
        {
            if (_fieldNameHashes != null) return _fieldNameHashes;

            var dict = new Dictionary<ulong, string>();

            File.WriteAllBytes("tempf.bin", ms.ToArray());
            foreach (var line in File.ReadLines("tempf.bin"))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("string,"))
                    continue;

                var parts = line.Split(',');
                if (parts.Length != 3)
                    continue;

                string name = parts[0].Trim();
                string hexBytes = parts[1].Trim();
                string binOffset = parts[2].Trim();

                // Parse space-separated hex bytes into a byte array
                byte[] bytes = hexBytes
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(b => byte.Parse(b, NumberStyles.HexNumber))
                    .ToArray();

                if (bytes.Length != 8)
                    throw new InvalidDataException($"Hash for '{name}' does not contain 8 bytes.");

                // Convert little-endian bytes to ulong
                ulong key = BitConverter.ToUInt64(bytes, 0);

                dict[key] = name;
            }

            _fieldNameHashes = dict;
            return _fieldNameHashes;
        }
    }

}
