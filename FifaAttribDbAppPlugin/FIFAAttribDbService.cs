using FifaAttribDbAppPlugin.AttribDb;
using FifaAttribDbAppPlugin.AttribDbGameplay;
using FMT.FileTools;
using FMT.Hash;
using FMT.Logging;
using FMT.Models.Assets.AssetEntry.Entries;
using FMT.PluginInterfaces.Assets;
using FMT.ProfileSystem;
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

        private readonly Dictionary<string, byte[]> _originalBinaryData = new();

        public FIFAAttribDbAssetEntry GetAssetEntry(string key)
        {
            if (Assets.Count == 0)
                return null;

            return Assets.FirstOrDefault(x => 
                $"{x.GetPath()}/{x.GetDisplayName()}".Equals(key, StringComparison.OrdinalIgnoreCase) ||
                $"{x.AttribDbType.FolderName}/{x.AttribDbType.Name}".Equals(key, StringComparison.OrdinalIgnoreCase)
            );
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
            foreach (var type in types)
            {
                var updatedFields = new List<FIFAAttribDbField>();
                foreach (var field in type.Fields)
                {
                    if (field.BinaryFileOffset.HasValue && field.BinaryFileOffset.Value + 4 <= data.Length)
                    {
                        var binOff = (int)field.BinaryFileOffset.Value;
                        switch (field.FieldType)
                        {
                            case FifaAttribDbFieldType.FloatCurve:
                            case FifaAttribDbFieldType.FloatCurve2:
                                int fcFloatCount;
                                if (field.BinaryFileSize.HasValue && field.BinaryFileSize.Value >= 4)
                                {
                                    fcFloatCount = field.BinaryFileSize.Value / 4;
                                }
                                else
                                {
                                    fcFloatCount = 0;
                                }
                                if (fcFloatCount > 0 && binOff + fcFloatCount * 4 <= data.Length)
                                {
                                    var fcValues = new float[fcFloatCount];
                                    for (int p = 0; p < fcFloatCount; p++)
                                        fcValues[p] = BitConverter.ToSingle(data, binOff + p * 4);
                                    field.Value = fcValues;
                                }
                                break;
                            case FifaAttribDbFieldType.Array:
                                var arrCount = BitConverter.ToUInt16(data, binOff);
                                var arrCapacity = BitConverter.ToUInt16(data, binOff + 2);
                                var arrFlags = BitConverter.ToUInt32(data, binOff + 4);
                                if (arrCount > 0 && arrCount == arrCapacity)
                                {
                                    var arrValues = new float[arrCount];
                                    for (int p = 0; p < arrCount; p++)
                                        arrValues[p] = BitConverter.ToSingle(data, binOff + 8 + p * 4);
                                    field.Value = arrValues;
                                }
                                else
                                {
                                    field.Value = Array.Empty<float>();
                                }
                                break;
                        }
                    }
                }
            }
            return types;
        }

        public List<FIFAAttribDbType> ReadAttribDbGameplayVlt(byte[] data)
        {
            using var r = new FIFAAttribDbGameplayVLTReader(data);
            return r.ListOfDbTypes;
        }

        public void Load(byte[] vaultData, byte[] binaryData)
        {
            Load("data/attribdbgameplay/attribdb.vlt", "data/attribdbgameplay/attribdb.bin", vaultData, binaryData);
        }

        public void Load(string vltPath, string binPath, byte[] vaultData, byte[] binaryData)
        {
            _originalBinaryData[binPath] = (byte[])binaryData.Clone();

            var types = ReadAttribDbGameplayVlt(vaultData);

            // Compute BinaryFileSizes robustly
            var allNonScalarFields = types.SelectMany(t => t.Fields)
                .Where(f => f.BinaryFileOffset.HasValue && (
                    f.FieldType == FifaAttribDbFieldType.FloatCurve ||
                    f.FieldType == FifaAttribDbFieldType.FloatCurve2 ||
                    f.FieldType == FifaAttribDbFieldType.Array
                ))
                .ToList();

            var uniqueOffsets = allNonScalarFields
                .Select(f => f.BinaryFileOffset.Value)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var offsetToSize = new Dictionary<long, int>();
            for (int i = 0; i < uniqueOffsets.Count; i++)
            {
                long currentOffset = uniqueOffsets[i];
                int size;
                if (i < uniqueOffsets.Count - 1)
                {
                    size = (int)(uniqueOffsets[i + 1] - currentOffset);
                }
                else
                {
                    size = binaryData.Length - (int)currentOffset;
                }
                offsetToSize[currentOffset] = size;
            }

            foreach (var f in allNonScalarFields)
            {
                if (offsetToSize.TryGetValue(f.BinaryFileOffset.Value, out int size))
                {
                    f.BinaryFileSize = size;
                }
            }

            foreach (var type in types)
            {
                var entry = new FIFAAttribDbAssetEntry(type)
                {
                    Name = type.Name,
                    Type = "FIFAAttribDbAssetEntry",
                    Size = 0,
                    OriginalSize = 0,
                    Sha1 = Sha1.Create(Encoding.UTF8.GetBytes(type.Name)),
                    VltPath = vltPath,
                    BinPath = binPath
                };
                Assets.Add(entry);
            }

            types = ReadAttribDbGameplayBinary(binaryData, types);
        }

        private int _saveVersion;

        public async void Save()
        {
            int myVersion = ++_saveVersion;

            await Task.Delay(400);

            if (myVersion != _saveVersion)
                return;

            var assetManagementService = SingletonService.GetInstance<IAssetManagementService>();

            var groups = Assets.GroupBy(x => x.VltPath).ToList();

            // Check for assets that are marked "modified" but actually aren't and revert them
            foreach (var group in groups)
            {
                foreach (var asset in group)
                {
                    var isModifiedByField = false;
                    foreach(var f in asset.AttribDbType.Fields)
                    {
                        if (f.ModifiedValue != null)
                        {
                            isModifiedByField = true;
                            break;
                        }
                    }
                    if (!isModifiedByField)
                    {
                        RevertAsset(asset);
                    }
                }
            }

            foreach (var group in groups)
            {
                var vltPath = group.Key;
                if (string.IsNullOrEmpty(vltPath)) continue;

                var firstEntry = group.First();
                var binPath = firstEntry.BinPath;

                var vltEntry = assetManagementService.CustomAssetManagers["legacy"].GetAssetEntry(vltPath);
                if (vltEntry == null) continue;
                assetManagementService.RevertAsset(vltEntry);

                var vltVanillaData = assetManagementService.GetAssetData(vltEntry, false);
                var writtenVltData = await new FIFAAttribDbVLTWriter().WriteToBytes(group.ToList(), vltVanillaData);

#if DEBUG
                DebugBytesToFileLogger.Instance.WriteAllBytes("AttribDbVlt.dat", writtenVltData, $"AttribDb/{ProfileManager.ProfileName}", false);
#endif

                assetManagementService.ModifyCustomAsset("legacy", vltPath, writtenVltData);
                // Not important at this time.
                // TODO: Add edits to the other file too
                //assetManagementService.ModifyCustomAsset("legacy", vltPath.Replace("attribdbgameplay", "attribdb"), writtenVltData);

                byte[] modifiedBinData = null;
                if (!string.IsNullOrEmpty(binPath))
                {
                    modifiedBinData = GetModifiedBinaryData(binPath, group.ToList());
                    if (modifiedBinData != null)
                    {
                        var binEntry = assetManagementService.CustomAssetManagers["legacy"].GetAssetEntry(binPath);
                        assetManagementService.RevertAsset(binEntry);
                        assetManagementService.ModifyCustomAsset("legacy", binPath, modifiedBinData);
                    }
                }

                // Not important at this time.
                //try
                //{
                //    var bigFile = assetManagementService.EnumerateCustomAssets("legacy").FirstOrDefault(x => x.Name.Contains("gameplayattribdb.big"));
                //    if (bigFile != null)
                //    {
                //        var bigFileService = new BIGFileService();
                //        var big = bigFileService.LoadBig((AssetEntry)bigFile);
                //        var bigAttribDbVlt = big.FirstOrDefault(x => x.Name.Contains("attribdb.vlt", StringComparison.OrdinalIgnoreCase));
                //        _ = bigAttribDbVlt;

                //        var bigAttribDbBin = big.FirstOrDefault(x => x.Name.Contains("attribdb.bin", StringComparison.OrdinalIgnoreCase));
                //        _ = bigAttribDbBin;
                //        if (bigAttribDbVlt != null)
                //        {
                //            bigFileService.Import(writtenVltData, bigAttribDbVlt, (AssetEntry)bigFile, big);
                //            if (modifiedBinData != null)
                //                bigFileService.Import(modifiedBinData, bigAttribDbBin, (AssetEntry)bigFile, big);
                //        }


                //    }
                //}
                //catch
                //{

                //}
            }

            

            assetManagementService?.Logger?.Log($"{nameof(FIFAAttribDbService)}.Saved");
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

        public byte[] GetModifiedBinaryData()
        {
            var firstBinPath = _originalBinaryData.Keys.FirstOrDefault();
            if (firstBinPath == null) return null;
            return GetModifiedBinaryData(firstBinPath, Assets.Where(x => x.BinPath == firstBinPath).ToList());
        }

        public byte[] GetModifiedBinaryData(string binPath, List<FIFAAttribDbAssetEntry> groupAssets)
        {
            if (!_originalBinaryData.TryGetValue(binPath, out var originalBinData) || originalBinData == null) 
                return null;

            var binData = (byte[])originalBinData.Clone();

            foreach (var entry in groupAssets)
            {
                foreach (var field in entry.AttribDbType.Fields)
                {
                    if (!field.BinaryFileOffset.HasValue) continue;
                    if (field.ModifiedValue == null) continue;

                    var binOff = (int)field.BinaryFileOffset.Value;
                    if (binOff < 0 || binOff >= binData.Length) continue;

                    switch (field.FieldType)
                    {
                        case FifaAttribDbFieldType.FloatCurve:
                        case FifaAttribDbFieldType.FloatCurve2:
                            if (field.ModifiedValue is float[] fcVals && field.BinaryFileSize.HasValue)
                            {
                                var floatCount = Math.Min(fcVals.Length, field.BinaryFileSize.Value / 4);
                                if (binOff + floatCount * 4 <= binData.Length)
                                {
                                    for (int p = 0; p < floatCount; p++)
                                        BitConverter.GetBytes(fcVals[p]).CopyTo(binData, binOff + p * 4);
                                }
                            }
                            break;
                        case FifaAttribDbFieldType.Array:
                            if (field.ModifiedValue is float[] arrVals)
                            {
                                if (binOff + 8 <= binData.Length)
                                {
                                    BitConverter.GetBytes((ushort)arrVals.Length).CopyTo(binData, binOff);
                                    BitConverter.GetBytes((ushort)arrVals.Length).CopyTo(binData, binOff + 2);
                                    // Preserve flags at binOff + 4, do NOT overwrite with (uint)4!
                                    for (int p = 0; p < arrVals.Length; p++)
                                    {
                                        if (binOff + 8 + p * 4 + 4 <= binData.Length)
                                            BitConverter.GetBytes(arrVals[p]).CopyTo(binData, binOff + 8 + p * 4);
                                    }
                                }
                            }
                            break;
                    }
                }
            }

            return binData;
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
                nativeWriter.WriteLengthPrefixedString($"{attribDbEntry.GetPath()}/{attribDbEntry.GetDisplayName()}");

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
                                    nativeWriter.Write(Convert.ToInt32(field.ModifiedValue));
                                    break;
                                case FifaAttribDbFieldType.Int64:
                                    nativeWriter.Write(Convert.ToInt64(field.ModifiedValue));
                                    break;
                                case FifaAttribDbFieldType.Float:
                                    if (field.ModifiedValue is string sVal)
                                        nativeWriter.Write(float.Parse(sVal));
                                    else
                                        nativeWriter.Write(Convert.ToSingle(field.ModifiedValue));
                                    break;
                                case FifaAttribDbFieldType.Bool:
                                    nativeWriter.Write(Convert.ToBoolean(field.ModifiedValue));
                                    break;
                                case FifaAttribDbFieldType.FloatCurve:
                                case FifaAttribDbFieldType.FloatCurve2:
                                case FifaAttribDbFieldType.Array:
                                    if (field.ModifiedValue is float[] fArr)
                                    {
                                        nativeWriter.Write(fArr.Length);
                                        foreach (var v in fArr)
                                            nativeWriter.Write(v);
                                    }
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
                        var existingField = existingAsset.AttribDbType.Fields.FirstOrDefault(f => f.Name == fieldName);
                        if (existingField == null) continue;

                        var fieldType = (FifaAttribDbFieldType)reader.ReadULong();
                        switch (fieldType)
                        {
                            case FifaAttribDbFieldType.Int32:
                                existingField.ModifiedValue = reader.ReadInt();
                                break;
                            case FifaAttribDbFieldType.Int64:
                                existingField.ModifiedValue = reader.ReadLong();
                                break;
                            case FifaAttribDbFieldType.Float:
                                existingField.ModifiedValue = reader.ReadSingle();
                                break;
                            case FifaAttribDbFieldType.Bool:
                                existingField.ModifiedValue = reader.ReadByte() != 0;
                                break;
                            case FifaAttribDbFieldType.FloatCurve:
                            case FifaAttribDbFieldType.FloatCurve2:
                            case FifaAttribDbFieldType.Array:
                                var arrLen = reader.ReadInt();
                                var arrVals = new float[arrLen];
                                for (int a = 0; a < arrLen; a++)
                                    arrVals[a] = reader.ReadSingle();
                                existingField.ModifiedValue = arrVals;
                                break;
                            default:
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
